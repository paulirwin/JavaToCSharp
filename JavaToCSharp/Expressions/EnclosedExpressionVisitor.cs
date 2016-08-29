using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions
{
    public class EnclosedExpressionVisitor : ExpressionVisitor<EnclosedExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, EnclosedExpr enclosedExpr)
        {
            var expr = enclosedExpr.getInner();
            var exprSyntax = ExpressionVisitor.VisitExpression(context, expr);

            return SyntaxFactory.ParenthesizedExpression(exprSyntax);
        }
    }
}
