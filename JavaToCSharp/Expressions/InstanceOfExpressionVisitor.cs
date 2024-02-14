using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions;

public class InstanceOfExpressionVisitor : ExpressionVisitor<InstanceOfExpr>
{
    protected override ExpressionSyntax? Visit(ConversionContext context, InstanceOfExpr expr)
    {
        var innerExpr = expr.getExpression();
        var exprSyntax = VisitExpression(context, innerExpr);

        if (exprSyntax is null)
        {
            return null;
        }

        var type = TypeHelper.ConvertTypeOf(expr);

        return SyntaxFactory.BinaryExpression(SyntaxKind.IsExpression, exprSyntax, SyntaxFactory.IdentifierName(type));
    }
}
