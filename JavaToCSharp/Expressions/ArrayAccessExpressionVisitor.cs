using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions;

public class ArrayAccessExpressionVisitor : ExpressionVisitor<ArrayAccessExpr>
{
    protected override ExpressionSyntax? Visit(ConversionContext context, ArrayAccessExpr expr)
    {
        var nameExpr = expr.getName();
        var nameSyntax = VisitExpression(context, nameExpr);
        if (nameSyntax is null)
        {
            return null;
        }

        var indexExpr = expr.getIndex();
        var indexSyntax = VisitExpression(context, indexExpr);
        if (indexSyntax is null)
        {
            return null;
        }

        return SyntaxFactory.ElementAccessExpression(nameSyntax, SyntaxFactory.BracketedArgumentList(SyntaxFactory.SeparatedList(
        [
            SyntaxFactory.Argument(indexSyntax)
        ])));
    }
}
