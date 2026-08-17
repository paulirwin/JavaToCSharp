using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions;

public class LongLiteralExpressionVisitor : ExpressionVisitor<LiteralStringValueExpr>
{
    protected override ExpressionSyntax Visit(ConversionContext context, LiteralStringValueExpr expr)
    {
        string value = expr is LongLiteralExpr longLiteralExpr ? longLiteralExpr.getValue() : expr.toString();
        value = value.Trim('\"').Replace("_", string.Empty);

        // Java marks a long literal with a trailing L/l. Strip it as a suffix only: a blanket
        // Replace would also corrupt digits in a value we echo back into the generated source.
        if (value.EndsWith('L') || value.EndsWith('l'))
        {
            value = value[..^1];
        }

        long int64Value;

        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            // Convert.ToInt64 accepts the 0x prefix and wraps values above long.MaxValue
            // (e.g. 0xFFFFFFFFFFFFFFFF -> -1), matching Java's two's-complement semantics.
            int64Value = Convert.ToInt64(value, 16);

            // C# types a hex literal by its magnitude, so anything above long.MaxValue becomes
            // ulong and will not implicitly convert to long (CS0266) even with the L suffix.
            // Emit the wrapped decimal value instead, which is the number Java means.
            if (int64Value < 0)
            {
                value = int64Value.ToString();
            }
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

        // Re-append the L suffix. C# infers int for a bare literal, so without it a value above
        // int.MaxValue either fails to compile (0xFFFFFFFFFFFFFFFF is ulong) or changes type.
        return SyntaxFactory.LiteralExpression(
            SyntaxKind.NumericLiteralExpression,
            SyntaxFactory.Literal(value + "L", int64Value));
    }
}
