using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions;

public class LongLiteralExpressionVisitor : ExpressionVisitor<LiteralStringValueExpr>
{
    protected override ExpressionSyntax Visit(ConversionContext context, LiteralStringValueExpr expr)
    {
        string value = expr is LongLiteralExpr longLiteralExpr ? longLiteralExpr.getValue() : expr.toString();
        value = value.Trim('\"')
            .Replace("L", string.Empty)
            .Replace("l", string.Empty)
            .Replace("_", string.Empty);

        long int64Value;

        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            int64Value = Convert.ToInt64(value, 16);
        }
        else if (value.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
        {
            int64Value = Convert.ToInt64(value[2..], 2);
        }
        else if (value.StartsWith('0') && value.Length > 1)
        {
            int64Value = Convert.ToInt64(value, 8);
            value = int64Value.ToString();
        }
        else
        {
            int64Value = Convert.ToInt64(value);
        }

        return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(value, int64Value));
    }
}
