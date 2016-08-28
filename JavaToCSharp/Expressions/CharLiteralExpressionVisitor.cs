using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions
{
    public class CharLiteralExpressionVisitor : ExpressionVisitor<CharLiteralExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, CharLiteralExpr expr)
        {
            return SyntaxFactory.LiteralExpression(SyntaxKind.CharacterLiteralExpression, SyntaxFactory.Literal(expr.toString().Trim('\'')[0]));
        }
    }
}
