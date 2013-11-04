using japa.parser.ast.expr;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavaToCSharp.Expressions
{
    public class ConditionalExpressionVisitor : ExpressionVisitor<ConditionalExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, ConditionalExpr conditionalExpr)
        {
            var condition = conditionalExpr.getCondition();
            var conditionSyntax = ExpressionVisitor.VisitExpression(context, condition);

            var thenExpr = conditionalExpr.getThenExpr();
            var thenSyntax = ExpressionVisitor.VisitExpression(context, thenExpr);

            var elseExpr = conditionalExpr.getElseExpr();
            var elseSyntax = ExpressionVisitor.VisitExpression(context, elseExpr);

            return Syntax.ConditionalExpression(conditionSyntax, thenSyntax, elseSyntax);
        }
    }
}
