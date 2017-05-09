using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace DeepPropertyAccessor
{
    public class ExpressionParseException: ArgumentException
    {
        public Expression Expression { get; private set; }

        public string ExpressionString
        {
            get {
                return Expression?.ToString() ?? "null";
            }
        }
        public ExpressionParseException(string message, Expression expression) : base(message)
        {
            Expression = expression;
        }
    }
}
