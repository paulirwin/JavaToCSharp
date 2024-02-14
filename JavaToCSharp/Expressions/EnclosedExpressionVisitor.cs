using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions;

public class EnclosedExpressionVisitor : ExpressionVisitor<EnclosedExpr>
{
    protected override ExpressionSyntax? Visit(ConversionContext context, EnclosedExpr enclosedExpr)
    {
        var expr = enclosedExpr.getInner();
        var exprSyntax = VisitExpression(context, expr);

        return exprSyntax is null ? null : SyntaxFactory.ParenthesizedExpression(exprSyntax);
    }
}
