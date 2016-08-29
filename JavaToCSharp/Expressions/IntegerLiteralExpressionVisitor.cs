using System;
using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions
{
    public class IntegerLiteralExpressionVisitor : ExpressionVisitor<IntegerLiteralExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, IntegerLiteralExpr expr)
        {
            string value = expr.getValue();

            if (value.StartsWith("0x"))
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(value, Convert.ToInt32(value.Substring(2), 16)));
            else
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(int.Parse(value)));
        }
    }
}
