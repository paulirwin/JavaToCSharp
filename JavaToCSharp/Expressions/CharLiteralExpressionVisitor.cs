using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;

namespace JavaToCSharp.Expressions
{
    public class CharLiteralExpressionVisitor : ExpressionVisitor<CharLiteralExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, CharLiteralExpr expr)
        {
            string value = Regex.Unescape(expr.getValue());
            return SyntaxFactory.LiteralExpression(SyntaxKind.CharacterLiteralExpression, SyntaxFactory.Literal(value[0]));
        }
    }
}
