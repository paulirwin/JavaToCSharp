using System;
using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions;

public class IntegerLiteralExpressionVisitor : ExpressionVisitor<IntegerLiteralExpr>
{
    public override ExpressionSyntax Visit(ConversionContext context, IntegerLiteralExpr expr)
    {
        var value = expr.getValue().Replace("_", String.Empty);
        var int32Value = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? Convert.ToInt32(value, 16) : Int32.Parse(value);
        return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(value, int32Value));
    }
}