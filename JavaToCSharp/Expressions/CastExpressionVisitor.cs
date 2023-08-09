using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions
{
    public class CastExpressionVisitor : ExpressionVisitor<CastExpr>
    {
        public override ExpressionSyntax? Visit(ConversionContext context, CastExpr castExpr)
        {
            var expr = castExpr.getExpression();
            var exprSyntax = VisitExpression(context, expr);
            if (exprSyntax is null)
            {
                return null;
            }

            var type = TypeHelper.ConvertTypeOf(castExpr);
            return SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName(type), exprSyntax);
        }
    }
}
