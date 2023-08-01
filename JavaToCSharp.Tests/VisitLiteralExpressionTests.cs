using com.github.javaparser.ast.expr;
using JavaToCSharp.Expressions;
using Xunit;

namespace JavaToCSharp.Tests;

public class VisitLiteralExpressionTests
{
    [Fact]
    public void VisitLiteralExpression_Char()
    {
        Assert.Equal("\n", ExpressionVisitor.VisitExpression(null, new CharLiteralExpr("\\n")).GetFirstToken().ValueText);
    }

    [Fact]
    public void VisitLiteralExpression_String()
    {
        Assert.Equal("\\r", ExpressionVisitor.VisitExpression(null, new StringLiteralExpr("\\\\r")).GetFirstToken().ValueText);
    }
}

