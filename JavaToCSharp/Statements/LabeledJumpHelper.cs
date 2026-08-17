using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements;

/// <summary>
/// Lowers Java's labeled <c>break</c>/<c>continue</c> into C#.
/// </summary>
/// <remarks>
/// Two shapes are supported, selected by <see cref="JavaConversionOptions.UseLabeledBreakAndContinue"/>:
/// <list type="bullet">
/// <item>C# 15 labeled jumps (<c>break outer;</c>). Roslyn cannot model these — there is no label operand on
/// <see cref="BreakStatementSyntax"/> and the parser rejects the syntax even at
/// <c>LanguageVersion.Preview</c> — so a <c>goto</c> placeholder is emitted instead and rewritten to the real
/// text after whitespace normalization by <see cref="SanitizingSyntaxRewriter"/>.</item>
/// <item>A <c>goto</c> to a generated target label, which is valid in every C# version.</item>
/// </list>
/// </remarks>
internal static partial class LabeledJumpHelper
{
    /// <summary>
    /// Prefix for the placeholder <c>goto</c> target that stands in for a C# 15 labeled jump.
    /// Chosen to be a valid C# identifier so the placeholder tree parses and normalizes cleanly.
    /// </summary>
    private const string PlaceholderPrefix = "__javaToCSharp_labeled_";

    [GeneratedRegex($@"goto {PlaceholderPrefix}(break|continue)_(\w+);")]
    private static partial Regex PlaceholderRegex { get; }

    /// <summary>
    /// The label a <c>goto</c> jumps to in order to leave the loop labeled <paramref name="label"/>.
    /// Emitted immediately after the loop.
    /// </summary>
    internal static string BreakTargetLabel(string label) => $"{label}_break";

    /// <summary>
    /// The label a <c>goto</c> jumps to in order to start the next iteration of the loop labeled
    /// <paramref name="label"/>. Emitted as the last statement of the loop body.
    /// </summary>
    internal static string ContinueTargetLabel(string label) => $"{label}_continue";

    /// <summary>
    /// Creates the statement for a labeled <c>break</c> or <c>continue</c>, recording on the context which
    /// target labels the <c>goto</c> fallback must emit.
    /// </summary>
    internal static StatementSyntax CreateJump(ConversionContext context, string label, bool isBreak)
    {
        if (context.Options.UseLabeledBreakAndContinue)
        {
            // Placeholder for `break <label>;` / `continue <label>;`, rewritten post-normalization.
            return SyntaxFactory.ParseStatement(
                $"goto {PlaceholderPrefix}{(isBreak ? "break" : "continue")}_{label};");
        }

        var target = isBreak ? BreakTargetLabel(label) : ContinueTargetLabel(label);

        if (isBreak)
        {
            context.LabelsNeedingBreakTarget.Add(label);
        }
        else
        {
            context.LabelsNeedingContinueTarget.Add(label);
        }

        return SyntaxFactory.GotoStatement(
            SyntaxKind.GotoStatement,
            SyntaxFactory.IdentifierName(target));
    }

    /// <summary>
    /// Replaces the placeholder <c>goto</c> statements with real C# 15 labeled jumps. Must run on the final
    /// text, because the resulting syntax cannot be represented in a Roslyn syntax tree.
    /// </summary>
    internal static string RewritePlaceholders(string text) =>
        PlaceholderRegex.Replace(text, "$1 $2;");
}
