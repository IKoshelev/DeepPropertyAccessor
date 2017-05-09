using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace DeepPropertyAccessor
{
    public class AccessChainValidityChecker : ExpressionVisitor
    {
        protected override Expression VisitBinary(BinaryExpression expr)
        {
            if(expr.Right.NodeType != ExpressionType.Constant)
            {
                throw new ExpressionParseException("Only consant value expressions can be used with Index.",expr);
            }

            return base.VisitBinary(expr);
        }

        protected override Expression VisitMethodCall(MethodCallExpression expr)
        {
            if (expr.Arguments.Any(x => x.NodeType != ExpressionType.Constant))
            {
                throw new ExpressionParseException("MethodCall can only have constant arguments.", expr);
            }

            return base.VisitMethodCall(expr);
        }

    }
}
