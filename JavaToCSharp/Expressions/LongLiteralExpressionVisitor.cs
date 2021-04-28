using System;
using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions
{
    public class LongLiteralExpressionVisitor : ExpressionVisitor<StringLiteralExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, StringLiteralExpr expr)
        {
            var data = expr.toString()
                .Trim('\"')
                .Replace("L", string.Empty)
                .Replace("l", string.Empty)
                .Replace("_", string.Empty);

            var value = data.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? Convert.ToInt64(data, 16) : Convert.ToInt64(data);
            return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(value));
        }
    }
}