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

        private static IEnumerable<AccessorChainPart> ParseAccessChain<TSource, TProp>(
            TSource source, 
            Expression<Func<TSource, TProp>> getter)
        {
            yield return new AccessorChainPart(source, typeof(TSource));

            List<MemberExpression> chainOfMemberAccess = new List<MemberExpression>();

            var current = getter.Body as MemberExpression;
            while (current != null)
            {
                chainOfMemberAccess.Add(current);
                current = current.Expression as MemberExpression;
            }

            chainOfMemberAccess.Reverse();

            object lastValue = source;
            if (chainOfMemberAccess.Any())
            {
                var firstAccess = chainOfMemberAccess.ElementAt(0);
                lastValue = GetValueFromMemberExpression(source, firstAccess);

                yield return new AccessorChainPart(lastValue, firstAccess.Member);
            }

            for(int count1 = 1; count1 < chainOfMemberAccess.Count(); count1++)
            {
                var currentAccess = chainOfMemberAccess.ElementAt(count1);
                lastValue = GetValueFromMemberExpression(lastValue, currentAccess);
                yield return new AccessorChainPart(lastValue, currentAccess.Member);
            }
        }

        private static object GetValueFromMemberExpression(object source, MemberExpression expr)
        {
            var propInfo = (expr.Member as PropertyInfo);
            if (propInfo != null)
            {
                return propInfo.GetValue(source, null);

            }
            var fieldInfo = (expr.Member as FieldInfo);
            if (fieldInfo != null)
            {
                return fieldInfo.GetValue(source);
            }

            throw new ArgumentException($"{expr.ToString()} is neither Field nor Property access.");

        }
    }

    public class AccessorChainPart
    {
        public object Value { get; private set; }
        public string Name { get; private set; }
        public MemberInfo MemberInfo { get; private set; }

        public AccessorChainPart()
        {
        }

        public AccessorChainPart(object value, MemberInfo memberInfo)
        {
            Value = value;
            MemberInfo = memberInfo;
            Name = MemberInfo.Name;
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
