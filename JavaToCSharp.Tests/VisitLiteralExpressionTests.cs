using com.github.javaparser.ast.expr;
using JavaToCSharp.Expressions;

namespace JavaToCSharp.Tests;

public class VisitLiteralExpressionTests
{
    [Fact]
    public void VisitLiteralExpression_Char()
    {
        var expr = ExpressionVisitor.VisitExpression(new ConversionContext(new JavaConversionOptions()), new CharLiteralExpr("\\n"));
        Assert.Equal("\n", expr?.GetFirstToken().ValueText);
    }

    [Fact]
    public void VisitLiteralExpression_String()
    {
        var expr = ExpressionVisitor.VisitExpression(new ConversionContext(new JavaConversionOptions()), new StringLiteralExpr(@"\\r"));
        Assert.Equal("\\r", expr?.GetFirstToken().ValueText);
    }

    [Theory]
    // The value is written as it appears in Java source, indented under the opening """.
    [InlineData("    a\n    b\n    ", "a\nb\n")]
    [InlineData("    <html>\n        <body>hi</body>\n    </html>\n    ", "<html>\n    <body>hi</body>\n</html>\n")]
    // Escapes are resolved once: \t stays a tab and \\ collapses to a single backslash.
    [InlineData("    tab\there \\\\ backslash\n    ", "tab\there \\ backslash\n")]
    // Two adjacent quotes need a four-quote delimiter in the generated C#.
    [InlineData("    he said \"\"quoted\"\" ok\n    ", "he said \"\"quoted\"\" ok\n")]
    public void VisitLiteralExpression_TextBlock(string javaValue, string expected)
    {
        var expr = ExpressionVisitor.VisitExpression(
            new ConversionContext(new JavaConversionOptions()),
            new TextBlockLiteralExpr(javaValue));

        Assert.Equal(expected, expr?.GetFirstToken().ValueText);
    }

    [Theory]
    [InlineData("0b10", 2)]
    [InlineData("0b100", 4)]
    [InlineData("0B1010", 10)]
    [InlineData("0b1_0000", 16)]
    [InlineData("0x1F", 31)]
    [InlineData("010", 8)]
    [InlineData("42", 42)]
    public void VisitLiteralExpression_Integer(string javaLiteral, int expected)
    {
        var expr = ExpressionVisitor.VisitExpression(new ConversionContext(new JavaConversionOptions()), new IntegerLiteralExpr(javaLiteral));
        Assert.Equal(expected, expr?.GetFirstToken().Value);
    }

    [Theory]
    [InlineData("0b10L", 2L)]
    [InlineData("0B1010L", 10L)]
    [InlineData("0x1FL", 31L)]
    [InlineData("010L", 8L)]
    [InlineData("10L", 10L)]
    [InlineData("2147483648L", 2147483648L)]
    // Java long literals are two's-complement, so an all-ones hex literal is -1.
    [InlineData("0xFFFFFFFFFFFFFFFFL", -1L)]
    [InlineData("0x7FFFFFFFFFFFFFFFL", long.MaxValue)]
    // A lowercase l suffix is equally valid Java.
    [InlineData("42l", 42L)]
    public void VisitLiteralExpression_Long(string javaLiteral, long expected)
    {
        var expr = ExpressionVisitor.VisitExpression(new ConversionContext(new JavaConversionOptions()), new LongLiteralExpr(javaLiteral));
        Assert.Equal(expected, expr?.GetFirstToken().Value);
    }

    /// <summary>
    /// The emitted text must keep the L suffix. C# types a bare numeric literal as int, so
    /// dropping it makes 0xFFFFFFFFFFFFFFFF a ulong that will not implicitly convert to long
    /// (CS0266), and pushes any value above int.MaxValue to a different inferred type.
    /// </summary>
    [Theory]
    // Above long.MaxValue C# would type the hex literal as ulong (CS0266), so the wrapped
    // decimal value is emitted instead.
    [InlineData("0xFFFFFFFFFFFFFFFFL", "-1L")]
    [InlineData("0x8000000000000000L", "-9223372036854775808L")]
    [InlineData("2147483648L", "2147483648L")]
    [InlineData("0x1FL", "0x1FL")]
    [InlineData("0b10L", "0b10L")]
    [InlineData("10L", "10L")]
    [InlineData("42l", "42L")]
    // Underscores are separators in Java and are dropped from the emitted literal.
    [InlineData("1_000_000L", "1000000L")]
    // Java octal has no C# equivalent, so it is rewritten in decimal - still suffixed.
    [InlineData("010L", "8L")]
    public void VisitLiteralExpression_Long_PreservesSuffixInText(string javaLiteral, string expectedText)
    {
        var expr = ExpressionVisitor.VisitExpression(new ConversionContext(new JavaConversionOptions()), new LongLiteralExpr(javaLiteral));
        Assert.Equal(expectedText, expr?.GetFirstToken().Text);
    }
}

