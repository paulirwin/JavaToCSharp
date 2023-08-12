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
}

