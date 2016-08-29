using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions
{
    public class ArrayAccessExpressionVisitor : ExpressionVisitor<ArrayAccessExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, ArrayAccessExpr expr)
        {
            var nameExpr = expr.getName();
            var nameSyntax = ExpressionVisitor.VisitExpression(context, nameExpr);

            var indexExpr = expr.getIndex();
            var indexSyntax = ExpressionVisitor.VisitExpression(context, indexExpr);

            return SyntaxFactory.ElementAccessExpression(nameSyntax, SyntaxFactory.BracketedArgumentList(SyntaxFactory.SeparatedList(new []
            {
                SyntaxFactory.Argument(indexSyntax)
            })));
        }
    }
}
