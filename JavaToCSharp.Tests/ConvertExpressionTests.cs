using com.github.javaparser;
using com.github.javaparser.ast.expr;
using JavaToCSharp.Expressions;

namespace JavaToCSharp.Tests;

public class ConvertExpressionTests
{
    [Theory]
    [InlineData("lst.size()", "lst.Count")]
    [InlineData("lst.get(i)", "lst[i]")]
    [InlineData("lst.set(i, value)", "lst[i] = value")]
    [InlineData("str.length()", "str.Length")]
    [InlineData("arr.length", "arr.Length")]

    //Conversion not done if param number does not match the required
    [InlineData("obj.size(i)", "obj.Size(i)")]
    [InlineData("obj.get()", "obj.Get()")]
    [InlineData("obj.get(i, j)", "obj.Get(i,j)")]
    [InlineData("obj.set(i)", "obj.Set(i)")]
    [InlineData("obj.set(i, j ,k)", "obj.Set(i,j,k)")]
    [InlineData("obj.length(i)", "obj.Length(i)")]
    public void Convert_DesignatedMethods_Into_PropertyAccessors(string javaExpr, string expectedCSharpExpr)
    {
        ParseResult parseResult = new JavaParser().parseExpression(javaExpr);
        Expression parsedExpr = parseResult.getResult().FromRequiredOptional<Expression>();
        var expr = ExpressionVisitor.VisitExpression(new ConversionContext(new JavaConversionOptions()), parsedExpr);
        Assert.Equal(expectedCSharpExpr, expr?.ToString());
    }

}
