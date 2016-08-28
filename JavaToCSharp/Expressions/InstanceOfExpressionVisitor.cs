using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions
{
    public class InstanceOfExpressionVisitor : ExpressionVisitor<InstanceOfExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, InstanceOfExpr expr)
        {
            var innerExpr = expr.getExpr();
            var exprSyntax = ExpressionVisitor.VisitExpression(context, innerExpr);

            var type = TypeHelper.ConvertType(expr.getType().toString());

            return SyntaxFactory.BinaryExpression(SyntaxKind.IsExpression, exprSyntax, SyntaxFactory.IdentifierName(type));
        }
    }
}
