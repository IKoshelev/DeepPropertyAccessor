using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace DeepPropertyAccessor
{
    public static class Util
    {
        public static object GetExprValue(this Expression expr, out Exception exceptionInChain, object source = null)
        {
            object GetValueHandlingEx(Func<object> func, out Exception exceptionInChainInner)
            {
                exceptionInChainInner = null;
                try
                {
                    return func();
                }
                catch (Exception ex) when (!(ex is ExpressionParseException))
                {
                    exceptionInChainInner = ex;
                    return null;
                }
            }

            exceptionInChain = null;
            switch (expr)
            {
                case ConstantExpression constExpr:
                    return constExpr.Value;
                    break;

                case MemberExpression memberExpr:
                    return memberExpr.GetValue(source);
                    break;

                case BinaryExpression binExpr:
                    return GetValueHandlingEx(() => binExpr.GetValue(source), out exceptionInChain);
                    break;

                case MethodCallExpression methodCallEx:
                    return GetValueHandlingEx(() => methodCallEx.GetValue(source), out exceptionInChain);
                    break;

                default:
                    throw new ExpressionParseException("Can't recognize type of member expression.", expr);
                    break;
            };
        }

        public static string GetStrValue(this ConstantExpression constExpr)
        {
            return constExpr.Value?.ToString() ?? "null";
        }

        public static object GetValue(this MemberExpression memberExpr, object source = null)
        {
            source = source
                    ?? (memberExpr.Expression as ConstantExpression)?.Value
                    ?? throw new ExpressionParseException("Can't recognize type of source expression.", memberExpr);

            var member = memberExpr.Member;
            object value;
            switch (member)
            {
                case FieldInfo fieldInfo:
                    value = fieldInfo.GetValue(source);
                    break;

                case PropertyInfo propertyInfo:
                    value = propertyInfo.GetValue(source, new object[0]);
                    break;

                default:
                    throw new ExpressionParseException("Can't recognize type of member expression.", memberExpr);
                    break;
            };

            return value;
        }

        public static object GetStrValue(this MemberExpression memberExpr, object source = null)
        {
            return Util.GetValue(memberExpr, source)?.ToString() ?? "null";
        }

        public static object GetValue(this BinaryExpression binExpr, object source = null)
        {
            if (binExpr.NodeType != ExpressionType.ArrayIndex)
            {
                throw new ExpressionParseException("For BinaryExpression only ExpressionType.ArrayIndex is supported.", binExpr);
            }

            source = source
                    ?? (binExpr.Left as ConstantExpression)?.Value
                    ?? throw new ExpressionParseException("Can't recognize type of source (Left) expression.", binExpr);

            //var index = (int)binExpr.Right.GetExprValue(out Exception exceptionInChain);
            if((binExpr.Right is ConstantExpression) == false)
            {
                throw new ExpressionParseException("Only Const expressions allowed as index (Right).", binExpr);
            }
            var index = (int)((ConstantExpression)binExpr.Right).Value; 
             var arr = (Array)source;
            var elem = arr.GetValue(index);

            return elem;
        }

        public static object GetValue(this MethodCallExpression methodCallExpr, object source = null)
        {
            var arguments = methodCallExpr.Arguments.Select(x =>
            {
                var val = x.GetExprValue(out Exception ex) ?? "null";
                if (ex != null)
                {
                    throw ex;
                }
                return val;
            })
            .ToArray();

            var method = methodCallExpr.Method;

            var result = method.Invoke(source, arguments);

            return result;
        }
    }
}
