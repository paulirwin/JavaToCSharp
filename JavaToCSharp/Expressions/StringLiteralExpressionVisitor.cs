using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;

namespace JavaToCSharp.Expressions;

public class StringLiteralExpressionVisitor : ExpressionVisitor<StringLiteralExpr>
{
    public override ExpressionSyntax Visit(ConversionContext context, StringLiteralExpr expr)
    {
        var value = Regex.Unescape(expr.getValue());
        return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(value));
    }
}
