using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions
{
	public class BooleanLiteralExpressionVisitor : ExpressionVisitor<BooleanLiteralExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, BooleanLiteralExpr expr)
        {
            if (expr.getValue())
                return SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);
            else
                return SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);
        }
    }
}
