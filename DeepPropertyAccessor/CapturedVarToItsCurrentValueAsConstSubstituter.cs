using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace DeepPropertyAccessor
{
    public class CapturedVarToItsCurrentValueAsConstSubstituter: ExpressionVisitor
    {
        protected override Expression VisitMember(MemberExpression node)
        {
            var isCapturedVarAccess = node.Expression.NodeType == ExpressionType.Constant;

            if (isCapturedVarAccess == false)
            {
                return base.VisitMember(node);
            }

            var value = node.GetValue();

            return Expression.Constant(value);
        }
    }
}
