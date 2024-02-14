using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions;

public class BooleanLiteralExpressionVisitor : ExpressionVisitor<BooleanLiteralExpr>
{
    protected override ExpressionSyntax Visit(ConversionContext context, BooleanLiteralExpr expr) =>
        SyntaxFactory.LiteralExpression(expr.getValue()
            ? SyntaxKind.TrueLiteralExpression
            : SyntaxKind.FalseLiteralExpression);
}
