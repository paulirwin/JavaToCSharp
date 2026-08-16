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
    public void VisitLiteralExpression_Long(string javaLiteral, long expected)
    {
        var expr = ExpressionVisitor.VisitExpression(new ConversionContext(new JavaConversionOptions()), new LongLiteralExpr(javaLiteral));
        Assert.Equal(expected, expr?.GetFirstToken().Value);
    }
}

