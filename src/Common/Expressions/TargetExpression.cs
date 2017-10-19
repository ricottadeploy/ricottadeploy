using Sprache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Ricotta.Expressions
{
    public class TargetExpression
    {
        private AgentInfo _agentInfo;
        private string _environment;

        public TargetExpression(AgentInfo agentInfo)
        {
            _agentInfo = agentInfo;
        }

        protected internal Parser<string> Identifier =>
                from first in Parse.Letter.Once()
                from rest in Parse.LetterOrDigit.XOr(Parse.Char('-')).XOr(Parse.Char('_')).Many()
                select new string(first.Concat(rest).ToArray());

        protected internal Parser<Expression> Selector =>
            from idOrRole in Identifier
            select Call(idOrRole);

        protected internal virtual Expression Call(string idOrRole)
        {
            var methodInfo = typeof(AgentInfo).GetMethod("EvaluateSelector");
            var methodArgs = new Expression[] {
                Expression.Constant(_environment, typeof(string)),
                Expression.Constant(idOrRole, typeof(string))
            };
            return Expression.Call(Expression.Constant(_agentInfo), methodInfo, methodArgs);
        }

        protected internal Parser<Expression> NotFactor =>
            from not in Not
            from factor in Factor
            select Expression.Not(factor);

        protected internal Parser<Expression> Factor =>
            Selector.Or(NotFactor).Or(ExprInParenthesis);

        protected internal Parser<Expression> Term =>
            Parse.ChainOperator(AndAlso, Factor, Expression.MakeBinary);

        protected internal Parser<Expression> Expr =>
            Parse.ChainOperator(OrElse, Term, Expression.MakeBinary);

        protected internal Parser<Expression> ExprInParenthesis =>
            from open in Parse.Char('(')
            from expr in Expr
            from close in Parse.Char(')')
            select expr;

        protected internal Parser<ExpressionType> Operator(string op, ExpressionType opType) =>
            Parse.String(op).Token().Return(opType);

        protected internal virtual Parser<ExpressionType> AndAlso =>
            Operator("&", ExpressionType.AndAlso);

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
