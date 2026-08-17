using com.github.javaparser.ast.expr;
using com.github.javaparser.ast.stmt;
using JavaToCSharp.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements;

public class SwitchStatementVisitor : StatementVisitor<SwitchStmt>
{
    public override StatementSyntax? Visit(ConversionContext context, SwitchStmt switchStmt)
    {
        var selector = switchStmt.getSelector();
        var selectorSyntax = ExpressionVisitor.VisitExpression(context, selector);

        if (selectorSyntax is null)
        {
            return null;
        }

        var cases = switchStmt.getEntries().ToList<SwitchEntry>();

        if (cases is null)
        {
            return SyntaxFactory.SwitchStatement(selectorSyntax, SyntaxFactory.List<SwitchSectionSyntax>());
        }

        var caseSyntaxes = new List<SwitchSectionSyntax>();

        foreach (var cs in cases)
        {
            var labels = cs.getLabels().ToList<Expression>();

            var statements = cs.getStatements().ToList<Statement>();
            var syntaxes = VisitStatements(context, statements);

            // Java's arrow form (`case X ->`) never falls through, but C# still requires each
            // section to end with a jump statement, so an implicit break is added. Colon-form
            // entries keep Java's explicit fallthrough and are left alone.
            if (!cs.getType().Equals(SwitchEntry.Type.STATEMENT_GROUP))
            {
                AddImplicitBreak(syntaxes);
            }

            // `case null, default` is modelled as a null label plus the default flag, so an entry
            // can be the default while still having labels. C#'s `default` section already handles
            // null, so the combined form collapses onto it.
            if (labels is not { Count: > 0 } || cs.isDefault())
            {
                // default case
                if (cs.getType().Equals(SwitchEntry.Type.STATEMENT_GROUP))
                {
                    AddImplicitBreak(syntaxes);
                }

                var defaultSyntax = SyntaxFactory.SwitchSection(
                    SyntaxFactory.List(new List<SwitchLabelSyntax> { SyntaxFactory.DefaultSwitchLabel() }),
                    SyntaxFactory.List(syntaxes.AsEnumerable()));
                caseSyntaxes.Add(defaultSyntax);
            }
            else
            {
                // A guard applies to the entry as a whole, so it is attached to each of its labels.
                var guardSyntax = cs.getGuard().FromOptional<Expression>() is { } guard
                    ? ExpressionVisitor.VisitExpression(context, guard)
                    : null;

                var labelSyntaxes = labels
                    .Select(SwitchLabelSyntax? (label) => ConvertLabel(context, label, guardSyntax))
                    .OfType<SwitchLabelSyntax>();

                var caseSyntax = SyntaxFactory.SwitchSection(
                    SyntaxFactory.List(labelSyntaxes.ToList()),
                    SyntaxFactory.List(syntaxes.AsEnumerable()));
                caseSyntaxes.Add(caseSyntax);
            }
        }

        return SyntaxFactory.SwitchStatement(selectorSyntax, SyntaxFactory.List(caseSyntaxes));
    }

    /// <summary>
    /// Appends a <c>break</c> to a switch section unless it already ends with a statement that
    /// transfers control, which would make the added break unreachable.
    /// </summary>
    private static void AddImplicitBreak(List<StatementSyntax> syntaxes)
    {
        if (syntaxes.Count > 0 && TransfersControl(syntaxes[^1]))
        {
            return;
        }

        syntaxes.Add(SyntaxFactory.BreakStatement());
    }

    /// <summary>
    /// Determines whether a statement unconditionally transfers control out of a switch section.
    /// </summary>
    private static bool TransfersControl(StatementSyntax statement) =>
        statement switch
        {
            // The arrow form's braces become a block, so the jump is one level down.
            BlockSyntax block => block.Statements.Count > 0 && TransfersControl(block.Statements[^1]),
            _ => statement.Kind() is SyntaxKind.BreakStatement
                or SyntaxKind.ReturnStatement
                or SyntaxKind.ThrowStatement
                or SyntaxKind.ContinueStatement
                or SyntaxKind.GotoStatement
        };

    /// <summary>
    /// Converts a single Java case label, which may be a Java 21 pattern, into a C# switch label.
    /// </summary>
    private static SwitchLabelSyntax? ConvertLabel(
        ConversionContext context,
        Expression label,
        ExpressionSyntax? guardSyntax)
    {
        if (label is PatternExpr patternLabel)
        {
            if (PatternExpressionVisitor.ConvertPattern(context, patternLabel) is not { } patternSyntax)
            {
                return null;
            }

            var caseLabel = SyntaxFactory.CasePatternSwitchLabel(patternSyntax, SyntaxFactory.Token(SyntaxKind.ColonToken));

            return guardSyntax is null
                ? caseLabel
                : caseLabel.WithWhenClause(SyntaxFactory.WhenClause(guardSyntax));
        }

        return ExpressionVisitor.VisitExpression(context, label) is { } labelExpr
            ? SyntaxFactory.CaseSwitchLabel(labelExpr)
            : null;
    }
}
