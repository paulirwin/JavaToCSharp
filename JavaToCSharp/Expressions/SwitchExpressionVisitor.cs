using com.github.javaparser.ast.expr;
using com.github.javaparser.ast.stmt;
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

        if (statements.Count > 1)
        {
            throw new InvalidOperationException("Switch expressions with multiple statements are not supported");
        }

        var armExpr = statements[0] switch
        {
            ThrowStmt throwStmt => throwStmt.getExpression(),
            ExpressionStmt exprStmt => exprStmt.getExpression(),
            _ => throw new InvalidOperationException("Only throw and expression statements are supported in switch expressions")
        };

        return VisitExpression(context, armExpr)
               ?? throw new InvalidOperationException("Switch expression entry must contain a single expression statement");
    }
}
