using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DeepPropertyAccessor
{
    public static class DeepPropertyAccessor
    {
        public static TProp DeepGet<TSource,TProp>(
            this TSource source, 
            Expression<Func<TSource, TProp>> getter, 
            Action<List<AccessorChainPart>, Expression<Func<TSource, TProp>>> onNullInChain = null)
            where TProp : class
        {         
            getter = SubstituteCapturedVarsToTheirCurrentValueAsConst(getter);

            Validate(getter);

            onNullInChain = onNullInChain ?? ((a,b)=>{ });

            var chain = new List<AccessorChainPart>();

            foreach(var chainPart in ParseAccessChain(source, getter))
            {
                chain.Add(chainPart);
                if(chainPart.Value == null)
                {
                    onNullInChain(chain, getter);
                    return null;
                }
            }

            return (TProp)chain.Last().Value;
        }

        public static TProp? DeepGetStruct<TSource, TProp>(
            this TSource source,
            Expression<Func<TSource, TProp>> getter,
            Action<List<AccessorChainPart>, Expression<Func<TSource, TProp>>> onNullInChain = null)
            where TProp : struct
        {
            getter = SubstituteCapturedVarsToTheirCurrentValueAsConst(getter);

            Validate(getter);

            onNullInChain = onNullInChain ?? ((a, b) => { });

            var chain = new List<AccessorChainPart>();

            foreach (var chainPart in ParseAccessChain(source, getter))
            {
                chain.Add(chainPart);
                if (chainPart.Value == null)
                {
                    onNullInChain(chain, getter);
                    return null;
                }
            }

            return (TProp?)chain.Last().Value;
        }

        public static string ToChainDescription(this Expression expr)
        {
            var str = expr.ToString();
            var firstDotIndex = str.IndexOf('.');
            if(firstDotIndex == -1)
            {
                throw new ArgumentException($"{str} is not a chain");
            }

            return "(root)" + str.Substring(firstDotIndex);
        }

        private static Dictionary<string, bool>  ValidationCache = new Dictionary<string, bool>();
        private static object CahceLock = new object();
        private static void Validate<TSource, TProp>(Expression<Func<TSource, TProp>> getter)
        {
            var key = getter.ToString();
            if(ValidationCache.TryGetValue(key, out bool value))
            {
                if (value == false)
                {
                    throw new ExpressionParseException("Expression has been previously deemed invalid.",getter);
                };
                return;
            }

            try
            {
                new AccessChainValidityChecker().Visit(getter);
            }
            catch (Exception ex)
            {
                lock (CahceLock)
                {
                    ValidationCache.Add(key, false);
                }
                throw;
            }

            lock (CahceLock)
            {
                ValidationCache.Add(key, true);
            }
        }

        private static Expression<Func<TSource, TProp>> 
            SubstituteCapturedVarsToTheirCurrentValueAsConst<TSource, TProp>
                                                                (Expression<Func<TSource, TProp>> getter)
        {
            var visitor = new CapturedVarToItsCurrentValueAsConstSubstituter();
            var modified = (Expression<Func<TSource, TProp>>)visitor.Visit(getter);
            return modified;
        }

        private static IEnumerable<AccessorChainPart> ParseAccessChain<TSource, TProp>(
            TSource source, 
            Expression<Func<TSource, TProp>> getter)
        {
            yield return new AccessorChainPart(source, typeof(TSource));

            List<Expression> chainOfMemberAccess = new List<Expression>();

            var current = getter.Body;
            while (current != null)
            {
                switch (current)
                {
                    case ParameterExpression _:
                        current = null; //start of chain, handled separately
                        break;
                    case MemberExpression memberExpr:
                        chainOfMemberAccess.Add(memberExpr);
                        current = memberExpr.Expression;
                        break;
                    case BinaryExpression binExpr:
                        chainOfMemberAccess.Add(binExpr);
                        current = binExpr.Left;
                        break;
                    case MethodCallExpression methodCallExpr:
                        chainOfMemberAccess.Add(methodCallExpr);
                        current = methodCallExpr.Object;
                        break;
                    default:
                        throw new ExpressionParseException("Unrecognized expression in chain", current);
                }
                
            }

            chainOfMemberAccess.Reverse();

            object lastValue = source;
            if (chainOfMemberAccess.Any())
            {
                var firstAccess = chainOfMemberAccess.ElementAt(0);
                lastValue = firstAccess.GetExprValue(out Exception exceptionInChain, source);

                yield return new AccessorChainPart(lastValue, firstAccess, exceptionInChain);
            }

            for(int count1 = 1; count1 < chainOfMemberAccess.Count(); count1++)
            {
                var currentAccess = chainOfMemberAccess.ElementAt(count1);
                lastValue = currentAccess.GetExprValue(out Exception exceptionInChain, lastValue);
                yield return new AccessorChainPart(lastValue, currentAccess, exceptionInChain);
            }
        }
    }

    public class AccessorChainPart
    {
        //not guaranteed, but worth a try
        public const string ExpectedIndexerMethodName = "get_Item";
        public object Value { get; private set; }
        public Exception ExceptionThrown { get; private set; }
        public string Name { get; private set; }
        public MemberInfo MemberInfo { get; private set; }
        public BinaryExpression BinarryExpression { get; private set; }
        public MethodCallExpression MethodCallExpression { get; private set; }

        public AccessorChainPart()
        {
        }

        public AccessorChainPart(object value, Expression expr, Exception exceptionThrown = null)
        {
            void ProcessBinaryExpr(BinaryExpression binExpr)
            {
                BinarryExpression = binExpr;
                var argument = binExpr.Right;
                var argValue = argument.GetExprValue(out Exception exceptionInChain) ?? "null";
                if (exceptionInChain != null)
                {
                    throw exceptionInChain;
                }
                Name = $"[{argValue}]";
            }

            void ProcessMethodCallExpr(MethodCallExpression methodCallExpr)
            {
                MethodCallExpression = methodCallExpr;
                var arguments = methodCallExpr.Arguments.Select(x =>
                {
                    var val = x.GetExprValue(out Exception ex);
                    if (ex != null)
                    {
                        throw ex;
                    }
                    return (val is string) 
                                ? $"\"{val}\"" 
                                : (val ?? "null");
                });
                var argValue = string.Join(",", arguments);
                var methodName = methodCallExpr.Method.Name;
                var isIndexer = methodName == ExpectedIndexerMethodName;
                if (isIndexer)
                {
                    Name = $"[{argValue}]";
                }
                else
                {
                    Name = $"{methodName}({argValue})";
                }           
            }

            Value = value;
            ExceptionThrown = exceptionThrown;
            switch (expr)
            {
                case MemberExpression memberExpr:
                    MemberInfo = memberExpr.Member;
                    Name = MemberInfo.Name;
                    break;
                case BinaryExpression binExpr:
                    ProcessBinaryExpr(binExpr);
                    break;
                case MethodCallExpression methodCallExpr:
                    ProcessMethodCallExpr(methodCallExpr);
                    break;
                default:
                    throw new ExpressionParseException("Expression type not supported.", expr);
            }        
        }

        public AccessorChainPart(object value, Type root)
        {
            Value = value;
            Name = "(root)" + GetClassNameAccountingForGeneric(root);
        }

        private string GetClassNameAccountingForGeneric(Type targetType)
        {
            if (targetType.IsGenericType == false)
            {         
                return targetType.Name;
            }

            if (targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return targetType.GetGenericArguments().Select(GetClassNameAccountingForGeneric).Single() + "?";
            }

            var baseType = targetType.GetGenericTypeDefinition();
            var name = baseType.Name.Split('`')[0];
            var paramNames = targetType.GetGenericArguments().Select(GetClassNameAccountingForGeneric);
            var constructorName = name + "<" + string.Join(",", paramNames) + ">";
            return constructorName;

        }

    }
}
