using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions;

public class IntegerLiteralExpressionVisitor : ExpressionVisitor<IntegerLiteralExpr>
{
    public override ExpressionSyntax Visit(ConversionContext context, IntegerLiteralExpr expr)
    {
        string value = expr.getValue().Replace("_", string.Empty);
        int int32Value;

        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            int32Value = Convert.ToInt32(value, 16);
        }
        else if (value.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
        {
            int32Value = Convert.ToInt32(value[2..], 2);
        }
        else if (value.StartsWith('0') && value.Length > 1)
        {
            int32Value = Convert.ToInt32(value, 8);
            value = int32Value.ToString();
        }
        else
        {
            int32Value = Convert.ToInt32(value);
        }

        return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(value, int32Value));
    }
}
