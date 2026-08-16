using com.github.javaparser;
using com.github.javaparser.ast.expr;
using JavaToCSharp.Expressions;

namespace JavaToCSharp.Tests;

public class SwitchExpressionLoweringTests
{
    /// <summary>
    /// Single-expression arms must keep using C# switch expressions rather than being lowered,
    /// so that existing conversions are unaffected.
    /// </summary>
    [Theory]
    [InlineData("switch (x) { case 1 -> 10; default -> 20; }")]
    [InlineData("switch (x) { case 1 -> 10; default -> throw new RuntimeException(); }")]
    // NOTE: the colon/yield form cannot be parsed by parseExpression, only in statement context,
    // so it is covered by the integration tests rather than here.
    public void SingleExpressionArms_ConvertToSwitchExpression(string javaExpr)
    {
        var csharp = Convert(javaExpr);

        Assert.Contains("switch", csharp);
        Assert.Contains("=>", csharp);
    }

    /// <summary>
    /// Arms with more than one statement cannot be represented as a C# switch expression arm.
    /// These are lowered by the statement visitors, so converting the expression alone throws.
    /// </summary>
    /// <summary>
    /// In positions where the statements cannot be hoisted out — nested inside a larger expression —
    /// the arm is emitted as an immediately-invoked lambda instead. The return type cannot be
    /// inferred without a symbol solver, so a placeholder is emitted for the user to replace.
    /// </summary>
    /// <remarks>
    /// Parsed as a whole file rather than via parseExpression, which does not recognise `yield`
    /// as a yield statement outside of statement context.
    /// </remarks>
    [Fact]
    public void MultiStatementArms_NestedInExpression_ConvertToInvokedLambda()
    {
        var warnings = new List<string>();
        var options = new JavaConversionOptions { IncludeComments = false };
        options.WarningEncountered += (_, e) => warnings.Add(e.Message);

        var csharp = JavaToCSharpConverter.ConvertText(
            """
            package example;
            public class Program {
                static int foo(int v) { return v; }
                public static void main(String[] args) {
                    int x = 1;
                    foo(switch (x) { case 1 -> { int a = 1; yield a; } default -> 20; });
                }
            }
            """, options);

        Assert.NotNull(csharp);
        Assert.Contains("Func<SPECIFY_ME>", csharp);
        // yield becomes return inside the lambda
        Assert.Contains("return a;", csharp);
        // the lambda must actually be invoked, not merely constructed
        Assert.Contains("))()", csharp);
        // arms that are already a single expression are left alone
        Assert.Contains("_ => 20", csharp);

        Assert.Contains(warnings, w => w.Contains("SPECIFY_ME"));
    }

    /// <summary>
    /// A label with no statements falls through to the next label. This previously crashed with an
    /// index-out-of-range because the arm body was read before checking that one existed.
    /// </summary>
    [Fact]
    public void FallthroughLabels_DoNotCrash()
    {
        var csharp = Convert("switch (x) { case 1, 2 -> 10; default -> 20; }");

        Assert.Contains("=>", csharp);
    }

    private static string? Convert(string javaExpr, JavaConversionOptions? options = null)
    {
        var parseResult = new JavaParser().parseExpression(javaExpr);
        var parsedExpr = parseResult.getResult().FromRequiredOptional<Expression>();
        var context = new ConversionContext(options ?? new JavaConversionOptions());

        return ExpressionVisitor.VisitExpression(context, parsedExpr)?.ToString();
    }
}
