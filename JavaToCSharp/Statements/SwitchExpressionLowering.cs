using com.github.javaparser.ast.expr;
using com.github.javaparser.ast.stmt;
using JavaToCSharp.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements;

/// <summary>
/// Lowers Java 14 multi-statement switch expressions (those using <c>yield</c>) into C# switch
/// statements that assign into a target variable. C# switch expressions only permit a single
/// expression per arm, so multi-statement arms cannot be represented directly.
/// </summary>
internal static class SwitchExpressionLowering
{
    /// <summary>
    /// Determines whether the given switch expression requires lowering to a switch statement,
    /// i.e. whether any arm contains more than a single expression or throw.
    /// </summary>
    public static bool RequiresLowering(SwitchExpr expr)
    {
        foreach (var entry in expr.getEntries().ToList<SwitchEntry>() ?? [])
        {
            var statements = entry.getStatements().ToList<Statement>() ?? [];

            // Zero statements is a fallthrough label in the colon form, which is representable.
            if (statements.Count > 1)
            {
                return true;
            }

            if (statements.Count == 1 && statements[0] is not (ThrowStmt or ExpressionStmt))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Builds a switch statement equivalent to <paramref name="expr"/>, assigning each arm's
    /// yielded value to <paramref name="target"/>.
    /// </summary>
    public static StatementSyntax Lower(ConversionContext context, SwitchExpr expr, string target)
    {
        var selector = ExpressionVisitor.VisitExpression(context, expr.getSelector())
                       ?? throw new InvalidOperationException("Switch expression selector cannot be null");

        var entries = expr.getEntries().ToList<SwitchEntry>() ?? [];
        var sections = new List<SwitchSectionSyntax>();
        var pendingLabels = new List<SwitchLabelSyntax>();

        var previousTarget = context.YieldTarget;
        context.YieldTarget = target;

        try
        {
            foreach (var entry in entries)
            {
                var labels = entry.getLabels().ToList<Expression>() ?? [];

                if (labels.Count == 0)
                {
                    pendingLabels.Add(SyntaxFactory.DefaultSwitchLabel());
                }
                else
                {
                    foreach (var label in labels)
                    {
                        var labelExpr = ExpressionVisitor.VisitExpression(context, label)
                                        ?? throw new InvalidOperationException("Switch expression label must contain an expression");

                        pendingLabels.Add(SyntaxFactory.CaseSwitchLabel(labelExpr));
                    }
                }

                var statements = entry.getStatements().ToList<Statement>() ?? [];

                // A label with no statements falls through to the next entry's labels.
                if (statements.Count == 0)
                {
                    continue;
                }

                var body = BuildSectionBody(context, statements, target);

                sections.Add(SyntaxFactory.SwitchSection(
                    SyntaxFactory.List(pendingLabels),
                    SyntaxFactory.List(body)));

                pendingLabels = [];
            }
        }
        finally
        {
            context.YieldTarget = previousTarget;
        }

        return SyntaxFactory.SwitchStatement(selector, SyntaxFactory.List(sections));
    }

    private static List<StatementSyntax> BuildSectionBody(
        ConversionContext context,
        List<Statement> statements,
        string target)
    {
        List<StatementSyntax> body;

        // The arrow form wraps the arm body in a block; flatten it so the assignment and break
        // sit directly in the switch section rather than inside a nested scope.
        if (statements is [BlockStmt block])
        {
            body = StatementVisitor.VisitStatements(context, block.getStatements().ToList<Statement>());
        }
        else if (statements is [ExpressionStmt exprStmt])
        {
            // Arrow form with a bare expression: `case X -> value` yields that value.
            var value = ExpressionVisitor.VisitExpression(context, exprStmt.getExpression())
                        ?? throw new InvalidOperationException("Switch expression arm must contain an expression");

            body =
            [
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(target),
                        value))
            ];
        }
        else
        {
            body = StatementVisitor.VisitStatements(context, statements);
        }

        if (!EndsControlFlow(body))
        {
            body.Add(SyntaxFactory.BreakStatement());
        }

        return body;
    }

    private static bool EndsControlFlow(List<StatementSyntax> body)
        => body.Count > 0 && body[^1] is BreakStatementSyntax or ReturnStatementSyntax or ThrowStatementSyntax;
}
