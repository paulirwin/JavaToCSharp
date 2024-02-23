using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace JavaToCSharp;

public static class Whitespace
{
    public static SyntaxTrivia NewLine => Environment.NewLine == "\r\n"
        ? SyntaxFactory.CarriageReturnLineFeed
        : SyntaxFactory.LineFeed;
}
