using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions;

public class TextBlockLiteralExpressionVisitor : ExpressionVisitor<TextBlockLiteralExpr>
{
    protected override ExpressionSyntax Visit(ConversionContext context, TextBlockLiteralExpr expr)
    {
        // asString() applies the Java text block rules for us: incidental whitespace is
        // stripped and escape sequences (including \s and line continuations) are resolved,
        // so the result is the final string value and must not be unescaped again.
        var value = expr.asString();

        return SyntaxFactory.LiteralExpression(
            SyntaxKind.StringLiteralExpression,
            CreateRawStringLiteral(value));
    }

    private static SyntaxToken CreateRawStringLiteral(string value)
    {
        // C# requires the delimiter to be longer than the longest run of quotes in the
        // content, so text containing "" needs at least four quotes to fence it.
        var fence = new string('"', Math.Max(3, LongestQuoteRun(value) + 1));

        // The opening fence is followed by a newline, and the closing fence sits on its own
        // line. That final newline belongs to the delimiter rather than the value, so a Java
        // text block ending in a newline needs the value written out verbatim before it.
        var text = $"{fence}\n{value}\n{fence}";

        return SyntaxFactory.Token(
            SyntaxTriviaList.Empty,
            SyntaxKind.MultiLineRawStringLiteralToken,
            text,
            value,
            SyntaxTriviaList.Empty);
    }

    private static int LongestQuoteRun(string value)
    {
        int longest = 0, run = 0;

        foreach (var c in value)
        {
            run = c == '"' ? run + 1 : 0;

            if (run > longest)
            {
                longest = run;
            }
        }

        return longest;
    }
}
