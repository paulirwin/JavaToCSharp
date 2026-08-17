using com.github.javaparser.ast.stmt;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements;

public class LabeledStatementVisitor : StatementVisitor<LabeledStmt>
{
    public override StatementSyntax? Visit(ConversionContext context, LabeledStmt labeledStmt)
    {
        var label = labeledStmt.getLabel().asString();
        var statement = labeledStmt.getStatement();

        // Only this label's own jumps matter here; an enclosing labeled loop handles its own.
        bool neededBreakTarget = context.LabelsNeedingBreakTarget.Remove(label);
        bool neededContinueTarget = context.LabelsNeedingContinueTarget.Remove(label);

        var syntax = VisitStatement(context, statement);

        if (syntax is null)
        {
            return null;
        }

        if (context.Options.UseLabeledBreakAndContinue)
        {
            // C# 15 places the label directly on the loop, exactly as Java does.
            return SyntaxFactory.LabeledStatement(label, syntax);
        }

        bool usesBreak = context.LabelsNeedingBreakTarget.Remove(label);
        bool usesContinue = context.LabelsNeedingContinueTarget.Remove(label);

        if (usesContinue)
        {
            syntax = AppendContinueTarget(context, syntax, label);
        }

        if (!usesBreak)
        {
            // Restore any state belonging to an enclosing statement with the same label.
            RestoreOuter(context, label, neededBreakTarget, neededContinueTarget);
            return syntax;
        }

        // The break target must follow the loop, so the loop and the label become a block. An empty
        // statement is required because a C# label cannot be the last statement in a block.
        var result = SyntaxFactory.Block(
            syntax,
            SyntaxFactory.LabeledStatement(
                LabeledJumpHelper.BreakTargetLabel(label),
                SyntaxFactory.EmptyStatement()));

        RestoreOuter(context, label, neededBreakTarget, neededContinueTarget);

        return result;
    }

    /// <summary>
    /// Appends the <c>continue</c> target label to the end of the loop body, so that a <c>goto</c> to it
    /// finishes the current iteration and lets the loop advance normally.
    /// </summary>
    private static StatementSyntax AppendContinueTarget(ConversionContext context, StatementSyntax syntax, string label)
    {
        var targetLabel = SyntaxFactory.LabeledStatement(
            LabeledJumpHelper.ContinueTargetLabel(label),
            SyntaxFactory.EmptyStatement());

        return syntax switch
        {
            ForStatementSyntax f => f.WithStatement(WithTrailing(f.Statement, targetLabel)),
            ForEachStatementSyntax f => f.WithStatement(WithTrailing(f.Statement, targetLabel)),
            WhileStatementSyntax w => w.WithStatement(WithTrailing(w.Statement, targetLabel)),
            DoStatementSyntax d => d.WithStatement(WithTrailing(d.Statement, targetLabel)),
            _ => Unsupported(context, syntax, label),
        };
    }

    private static StatementSyntax Unsupported(ConversionContext context, StatementSyntax syntax, string label)
    {
        context.Options.Warning(
            $"Labeled continue targeting `{label}` could not be lowered because the labeled statement is not a loop. Check for correctness.",
            0);

        return syntax;
    }

    private static BlockSyntax WithTrailing(StatementSyntax body, StatementSyntax target) =>
        body is BlockSyntax block
            ? block.AddStatements(target)
            : SyntaxFactory.Block(body, target);

    private static void RestoreOuter(ConversionContext context, string label, bool neededBreak, bool neededContinue)
    {
        if (neededBreak)
        {
            context.LabelsNeedingBreakTarget.Add(label);
        }

        if (neededContinue)
        {
            context.LabelsNeedingContinueTarget.Add(label);
        }
    }
}
