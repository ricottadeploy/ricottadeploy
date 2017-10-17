using Sprache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Common.Expressions
{
    public class EnvironmentExpression
    {
        private string _environment;

        protected internal Parser<string> Identifier =>
                from first in Parse.Letter.Once()
                from rest in Parse.LetterOrDigit.XOr(Parse.Char('-')).XOr(Parse.Char('_')).Many()
                select new string(first.Concat(rest).ToArray());

        protected internal Parser<Expression> Selector =>
            from environment in Identifier
            select CompareEnv(environment);

        protected internal virtual Expression CompareEnv(string environment)
        {
            return Expression.Equal(Expression.Constant(environment), Expression.Constant(_environment));
        }

        protected internal Parser<Expression> NotFactor =>
            from not in Not
            from factor in Factor
            select Expression.Not(factor);

        protected internal Parser<Expression> Factor =>
            Selector.Or(NotFactor).Or(ExprInParenthesis);

        protected internal Parser<Expression> Expr =>
            Parse.ChainOperator(OrElse, Factor, Expression.MakeBinary);

        protected internal Parser<Expression> ExprInParenthesis =>
            from open in Parse.Char('(')
            from expr in Expr
            from close in Parse.Char(')')
            select expr;

        protected internal Parser<ExpressionType> Operator(string op, ExpressionType opType) =>
           Parse.String(op).Token().Return(opType);

        protected internal virtual Parser<ExpressionType> OrElse =>
           Operator("|", ExpressionType.OrElse);

        protected internal virtual Parser<ExpressionType> Not =>
            Operator("!", ExpressionType.Not);

        public bool Evaluate(string environment, string expression)
        {
            _environment = environment;
            var expr = Expr.Parse(expression);
            Expression<Func<bool>> e = Expression.Lambda<Func<bool>>(expr);
            return e.Compile()();
        }
    }
}
