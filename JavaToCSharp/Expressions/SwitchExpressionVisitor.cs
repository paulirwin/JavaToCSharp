using com.github.javaparser;
using com.github.javaparser.ast.expr;
using com.github.javaparser.ast.stmt;
using JavaToCSharp.Statements;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace JavaToCSharp.Expressions;

public class SwitchExpressionVisitor : ExpressionVisitor<SwitchExpr>
{
    protected override ExpressionSyntax Visit(ConversionContext context, SwitchExpr expr)
    {
        var entries = expr.getEntries().ToList<SwitchEntry>() ?? [];

        var governingExpr = VisitExpression(context, expr.getSelector())
                            ?? throw new InvalidOperationException("Switch expression selector cannot be null");

        return SwitchExpression(
            governingExpr,
            SeparatedList(entries.Select(e => Visit(context, e)))
        );
    }

    private SwitchExpressionArmSyntax Visit(ConversionContext context, SwitchEntry entry)
    {
        var pattern = GetArmPatternSyntax(context, entry);
        var expr = GetArmExpressionSyntax(context, entry);

        return SwitchExpressionArm(
            pattern,
            expr
        );
    }

    private static PatternSyntax GetArmPatternSyntax(ConversionContext context, SwitchEntry entry)
    {
        var labels = entry.getLabels().ToList<Expression>() ?? [];

        if (labels.Count == 0)
        {
            return DiscardPattern();
        }

        var patterns = new List<PatternSyntax>();

        foreach (var label in labels)
        {
            if (VisitExpression(context, label) is not ExpressionSyntax labelExpr)
            {
                throw new InvalidOperationException("Switch expression label must contain an expression");
            }

            patterns.Add(ConstantPattern(labelExpr));
        }

        if (patterns.Count == 1)
        {
            return patterns[0];
        }

        var orPattern = BinaryPattern(SyntaxKind.OrPattern, patterns[0], patterns[1]);

        for (var i = 2; i < patterns.Count; i++)
        {
            orPattern = BinaryPattern(SyntaxKind.OrPattern, orPattern, patterns[i]);
        }

        return orPattern ?? throw new InvalidOperationException("Switch expression label must contain an expression");
    }

    private static ExpressionSyntax GetArmExpressionSyntax(ConversionContext context, SwitchEntry entry)
    {
        var statements = entry.getStatements().ToList<Statement>() ?? [];

        if (statements.Count == 0)
        {
            throw new InvalidOperationException("Switch expression entry must contain at least one statement");
        }

        // Reaching here with multiple statements, a block, or a yield means the enclosing statement
        // visitor could not hoist the statements out, i.e. the switch expression is nested inside a
        // larger expression. Emit an immediately-invoked lambda so the arm can still hold statements.
        // A block containing nothing but a yield is equivalent to that single expression.
        if (statements is [BlockStmt onlyBlock]
            && onlyBlock.getStatements().ToList<Statement>() is [YieldStmt blockYield])
        {
            statements = [blockYield];
        }

        if (statements.Count > 1 || statements[0] is BlockStmt)
        {
            return GetArmInvokedLambdaSyntax(context, entry, statements);
        }

        var armExpr = statements[0] switch
        {
            ThrowStmt throwStmt => throwStmt.getExpression(),
            ExpressionStmt exprStmt => exprStmt.getExpression(),
            // A lone yield is just the value it yields.
            YieldStmt yieldStmt => yieldStmt.getExpression(),
            _ => throw new InvalidOperationException("Only throw and expression statements are supported in switch expressions")
        };

        return VisitExpression(context, armExpr)
               ?? throw new InvalidOperationException("Switch expression entry must contain a single expression statement");
    }

    /// <summary>
    /// Builds <c>((Func&lt;SPECIFY_ME&gt;)(() =&gt; { ... }))()</c> for an arm whose statements cannot be
    /// hoisted into the enclosing block. The return type cannot be inferred without a symbol solver,
    /// so a placeholder is emitted for the user to replace.
    /// </summary>
    private static ExpressionSyntax GetArmInvokedLambdaSyntax(
        ConversionContext context,
        SwitchEntry entry,
        List<Statement> statements)
    {
        context.Options.Warning(
            "Switch expression arm with multiple statements is nested within another expression and was "
            + "converted to an invoked lambda. Replace SPECIFY_ME with the appropriate return type.",
            entry.getBegin().FromRequiredOptional<Position>().line);

        // Inside a lambda, Java's `yield` becomes `return`. A null YieldTarget selects that behaviour.
        var previousTarget = context.YieldTarget;
        context.YieldTarget = null;

        List<StatementSyntax> body;

        try
        {
            // The arrow form wraps the arm in a block; use its statements directly to avoid nesting.
            body = statements is [BlockStmt block]
                ? StatementVisitor.VisitStatements(context, block.getStatements().ToList<Statement>())
                : StatementVisitor.VisitStatements(context, statements);
        }
        finally
        {
            context.YieldTarget = previousTarget;
        }

        var lambda = ParenthesizedLambdaExpression().WithBlock(Block(body));

        var funcType = GenericName(Identifier("Func"))
            .WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList<TypeSyntax>(IdentifierName("SPECIFY_ME"))));

        return InvocationExpression(
            ParenthesizedExpression(
                CastExpression(funcType, ParenthesizedExpression(lambda))));
    }
}
