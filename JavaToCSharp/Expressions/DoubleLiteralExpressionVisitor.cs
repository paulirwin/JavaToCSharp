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

            if (dbl.getValue().EndsWith("f", StringComparison.OrdinalIgnoreCase))
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(float.Parse(dbl.getValue().TrimEnd('f', 'F'))));
            else
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(double.Parse(dbl.getValue().TrimEnd('d', 'D'))));
        }
    }
}
