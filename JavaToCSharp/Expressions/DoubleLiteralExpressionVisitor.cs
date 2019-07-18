using System;
using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions
{
    public class DoubleLiteralExpressionVisitor : ExpressionVisitor<DoubleLiteralExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, DoubleLiteralExpr expr)
        {
            // note: this must come before the check for StringLiteralExpr because DoubleLiteralExpr : StringLiteralExpr
            var dbl = (DoubleLiteralExpr)expr;

            var value = dbl.getValue().Replace("_", string.Empty);
            if (value.EndsWith("f", StringComparison.OrdinalIgnoreCase))
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(float.Parse(value.TrimEnd('f', 'F'))));
            else
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(double.Parse(value.TrimEnd('d', 'D'))));
        }
    }
}
