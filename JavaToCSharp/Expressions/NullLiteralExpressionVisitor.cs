using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions
{
	public class NullLiteralExpressionVisitor : ExpressionVisitor<NullLiteralExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, NullLiteralExpr expr)
        {
            return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
        }
    }
}
