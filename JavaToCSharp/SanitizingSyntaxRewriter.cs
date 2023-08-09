using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace JavaToCSharp;

/// <summary>
/// Improves the C# code after the conversion from Java and after the whitespace normalization was done.
/// </summary>
/// <remarks>Works on the C# Roslyn syntax tree, not on the Java ast.</remarks>
internal class SanitizingSyntaxRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? Visit(SyntaxNode? node)
    {
        if (node != null)
        {
            // We must do this after whitespace normalization!
            node = CommentsHelper.FixCommentsWhitespaces(node);
        }
        return base.Visit(node);
    }
}
