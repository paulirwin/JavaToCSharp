using System;
using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions;

public class LongLiteralExpressionVisitor : ExpressionVisitor<LiteralStringValueExpr>
{
    public override ExpressionSyntax Visit(ConversionContext context, LiteralStringValueExpr expr)
    {
        var longText = expr is LongLiteralExpr longLiteralExpr ? longLiteralExpr.getValue() : expr.toString();
        longText = longText.Trim('\"')
            .Replace("L", string.Empty)
            .Replace("l", string.Empty)
            .Replace("_", string.Empty);

        var value = longText.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? Convert.ToInt64(longText, 16)
            : Convert.ToInt64(longText);
        return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(value));
    }
}