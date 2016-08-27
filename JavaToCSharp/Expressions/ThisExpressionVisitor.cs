using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions
{
	public class ThisExpressionVisitor : ExpressionVisitor<ThisExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, ThisExpr expr)
        {
            return SyntaxFactory.ThisExpression();
        }
    }
}
