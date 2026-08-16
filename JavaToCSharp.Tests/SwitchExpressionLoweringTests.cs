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
    [Theory]
    [InlineData("switch (x) { case 1 -> { int a = 1; yield a; } default -> 20; }")]
    public void MultiStatementArms_AreNotConvertibleAsExpression(string javaExpr)
    {
        Assert.ThrowsAny<Exception>(() => Convert(javaExpr));
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

    private static string? Convert(string javaExpr)
    {
        var parseResult = new JavaParser().parseExpression(javaExpr);
        var parsedExpr = parseResult.getResult().FromRequiredOptional<Expression>();
        var context = new ConversionContext(new JavaConversionOptions());

        return ExpressionVisitor.VisitExpression(context, parsedExpr)?.ToString();
    }
}
