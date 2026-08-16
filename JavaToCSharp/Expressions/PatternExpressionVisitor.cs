using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace JavaToCSharp.Expressions;

/// <summary>
/// Converts Java pattern expressions (JEP 440/441) into C# patterns.
/// </summary>
/// <remarks>
/// Java's type patterns (<c>Shape s</c>) map onto C# declaration patterns, and Java's record
/// deconstruction patterns (<c>Point(int x, int y)</c>) map onto C# positional patterns.
/// The shapes line up closely enough that the conversion is structural, but see
/// <see cref="ConvertRecordPattern"/> for the one case where they diverge.
/// </remarks>
public static class PatternExpressionVisitor
{
    /// <summary>
    /// Converts a Java pattern into the equivalent C# pattern, or returns <c>null</c> if the
    /// pattern kind is not recognized.
    /// </summary>
    public static PatternSyntax? ConvertPattern(ConversionContext context, PatternExpr pattern) =>
        pattern switch
        {
            RecordPatternExpr recordPattern => ConvertRecordPattern(context, recordPattern),
            TypePatternExpr typePattern => ConvertTypePattern(typePattern),
            _ => null
        };

    /// <summary>
    /// Converts a type pattern such as <c>String s</c> into a C# declaration pattern.
    /// </summary>
    private static PatternSyntax ConvertTypePattern(TypePatternExpr pattern) =>
        DeclarationPattern(
            IdentifierName(TypeHelper.ConvertTypeOf(pattern)),
            SingleVariableDesignation(Identifier(pattern.getNameAsString())));

    /// <summary>
    /// Converts a record deconstruction pattern such as <c>Point(int x, int y)</c> into a C#
    /// positional pattern.
    /// </summary>
    private static PatternSyntax ConvertRecordPattern(ConversionContext context, RecordPatternExpr pattern)
    {
        var subpatterns = new List<SubpatternSyntax>();

        foreach (var component in pattern.getPatternList().ToList<PatternExpr>() ?? [])
        {
            // A nested `var x` component parses as a type pattern whose type is `var`. C# spells the
            // equivalent as a plain variable designation, since `var x` is not valid as a subpattern type.
            if (component is TypePatternExpr { } typeComponent && TypeHelper.ConvertTypeOf(typeComponent) == "var")
            {
                subpatterns.Add(Subpattern(VarPattern(SingleVariableDesignation(Identifier(typeComponent.getNameAsString())))));
                continue;
            }

            if (ConvertPattern(context, component) is not { } converted)
            {
                return RecursivePattern();
            }

            subpatterns.Add(Subpattern(converted));
        }

        return RecursivePattern()
            .WithType(IdentifierName(TypeHelper.ConvertTypeOf(pattern)))
            .WithPositionalPatternClause(PositionalPatternClause(SeparatedList(subpatterns)));
    }
}
