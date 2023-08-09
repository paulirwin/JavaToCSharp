using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions;

public class EnclosedExpressionVisitor : ExpressionVisitor<EnclosedExpr>
{
    public override ExpressionSyntax? Visit(ConversionContext context, EnclosedExpr enclosedExpr)
    {
        var expr = enclosedExpr.getInner();
        var exprSyntax = VisitExpression(context, expr);
        if (exprSyntax is null)
        {
            return null;
        }

        return SyntaxFactory.ParenthesizedExpression(exprSyntax);
    }
}
