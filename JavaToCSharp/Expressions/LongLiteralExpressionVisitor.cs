using System;
using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions;

public class LongLiteralExpressionVisitor : ExpressionVisitor<StringLiteralExpr>
{
    public override ExpressionSyntax Visit(ConversionContext context, StringLiteralExpr expr)
    {
        var longText = expr is LongLiteralExpr longLiteralExpr ? longLiteralExpr.getValue() : expr.toString();
        longText = longText.Trim('\"')
                   .Replace("L", String.Empty)
                   .Replace("l", String.Empty)
                   .Replace("_", String.Empty);

        var value = longText.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? Convert.ToInt64(longText, 16) : Convert.ToInt64(longText);
        return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(value));
    }
}