using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements;

/// <summary>
/// Lowers Java's labeled <c>break</c>/<c>continue</c> into C#.
/// </summary>
/// <remarks>
/// Two shapes are supported, selected by <see cref="JavaConversionOptions.UseLabeledBreakAndContinue"/>:
/// <list type="bullet">
/// <item>C# 15 labeled jumps (<c>break outer;</c>), which map one-to-one onto the Java syntax.</item>
/// <item>A <c>goto</c> to a generated target label, which is valid in every C# version.</item>
/// </list>
/// </remarks>
internal static class LabeledJumpHelper
{
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
            var name = SyntaxFactory.IdentifierName(label);

            // The labeled break/continue factory overloads are still marked experimental in Roslyn.
#pragma warning disable RSEXPERIMENTAL006
            return isBreak
                ? SyntaxFactory.BreakStatement(name)
                : SyntaxFactory.ContinueStatement(name);
#pragma warning restore RSEXPERIMENTAL006
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
}
