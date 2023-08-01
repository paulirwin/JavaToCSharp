using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions;

public class ConditionalExpressionVisitor : ExpressionVisitor<ConditionalExpr>
{
    public override ExpressionSyntax? Visit(ConversionContext context, ConditionalExpr conditionalExpr)
    {
        var condition = conditionalExpr.getCondition();
        var conditionSyntax = VisitExpression(context, condition);
        if (conditionSyntax is null)
        {
            return null;
        }

        var thenExpr = conditionalExpr.getThenExpr();
        var thenSyntax = VisitExpression(context, thenExpr);
        if (thenSyntax is null)
        {
            return null;
        }

        var elseExpr = conditionalExpr.getElseExpr();
        var elseSyntax = VisitExpression(context, elseExpr);
        if (elseSyntax is null)
        {
            return null;
        }

        return SyntaxFactory.ConditionalExpression(conditionSyntax, thenSyntax, elseSyntax);
    }
}
