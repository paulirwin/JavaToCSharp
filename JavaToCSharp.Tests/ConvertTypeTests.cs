using com.github.javaparser.ast.type;

namespace JavaToCSharp.Tests;

public class ConvertTypeTests
{
    [Fact]
    public void ConvertType_Int()
    {
        Assert.Equal("int", TypeHelper.ConvertType("int"));
    }

    [Fact]
    public void ConvertType_String()
    {
        Assert.Equal("string", TypeHelper.ConvertType("String"));
    }

    [Fact]
    public void ConvertType_Object()
    {
        Assert.Equal("object", TypeHelper.ConvertType("Object"));
    }

    [Fact]
    public void ConvertType_IntArray_BracketsAfterType()
    {
        Assert.Equal("int[]", TypeHelper.ConvertType("int[]"));
    }

    [Fact]
    public void ConvertType_GenericSingleParameter()
    {
        Assert.Equal("MyType<string>", TypeHelper.ConvertType("MyType<String>"));
    }

    [Fact]
    public void ConvertType_GenericMultipleParameters()
    {
        Assert.Equal("MyType<string, object>", TypeHelper.ConvertType("MyType<String, Object>"));
    }

    [Fact]
    public void ConvertType_WithGenericWildcard_ShouldReplaceToken()
    {
        Assert.Equal("MyType<TWildcardTodo>", TypeHelper.ConvertType("MyType<?>"));
    }

    [Fact]
    public void ConvertTypeSyntax_GivenNonArrayType_ShouldReturnCorrectSyntax()
    {
        var type = new PrimitiveType(PrimitiveType.Primitive.INT);

        var syntax = TypeHelper.ConvertTypeSyntax(type, 0);

        Assert.Equal("int", syntax.ToString());
    }

    [Fact]
    public void ConvertTypeSyntax_GivenNonArrayTypeWithRank_ShouldReturnCorrectSyntax()
    {
        var type = new PrimitiveType(PrimitiveType.Primitive.INT);

        var syntax = TypeHelper.ConvertTypeSyntax(type, 1);

        Assert.Equal("int[]", syntax.ToString());
    }

    [Fact]
    public void ConvertTypeSyntax_GivenNonArrayTypeWithMultidimensionalRank_ShouldReturnCorrectSyntax()
    {
        var type = new PrimitiveType(PrimitiveType.Primitive.INT);

        var syntax = TypeHelper.ConvertTypeSyntax(type, 2);

        Assert.Equal("int[,]", syntax.ToString());
    }

    [Fact]
    public void ConvertTypeSyntax_GivenArrayTypeWithoutRank_ShouldReturnCorrectSyntax()
    {
        var type = new ArrayType(new PrimitiveType(PrimitiveType.Primitive.INT));

        var syntax = TypeHelper.ConvertTypeSyntax(type, 0);

        Assert.Equal("int[]", syntax.ToString());
    }

    [Fact]
    public void ConvertTypeSyntax_GivenArrayTypeWithRank_ShouldReturnCorrectSyntax()
    {
        var type = new ArrayType(new PrimitiveType(PrimitiveType.Primitive.INT));

        var syntax = TypeHelper.ConvertTypeSyntax(type, 1);

        Assert.Equal("int[]", syntax.ToString());
    }

    [Fact]
    public void ConvertTypeSyntax_GivenArrayTypeWithMultidimensionalRank_ShouldReturnCorrectSyntax()
    {
        var type = new ArrayType(new ArrayType(new PrimitiveType(PrimitiveType.Primitive.INT)));

        var syntax = TypeHelper.ConvertTypeSyntax(type, 2);

        Assert.Equal("int[,]", syntax.ToString());
    }

    [Fact]
    public void ConvertTypeSyntax_GivenMismatchedRank_ShouldThrowException()
    {
        var type = new ArrayType(new ArrayType(new PrimitiveType(PrimitiveType.Primitive.INT)));

        Assert.Throws<ArgumentException>(() => TypeHelper.ConvertTypeSyntax(type, 1));
    }

    [Theory]
    [InlineData("GenericClass<T>", "GenericClass<T>")]
    [InlineData("GenericClass<T extends BoundType<T>>", "GenericClass<T>\n    where T : BoundType<T>")]
    [InlineData("GenericClass<T extends BoundType>", "GenericClass<T>\n    where T : BoundType")]

    [InlineData("GenericClass<T, U>", "GenericClass<T, U>")]
    [InlineData("GenericClass<T, U extends BoundType<U>>", "GenericClass<T, U>\n    where U : BoundType<U>")]
    [InlineData("GenericClass<T extends BoundType1<T>, U extends BoundType2<U>>", "GenericClass<T, U>\n    where T : BoundType1<T> where U : BoundType2<U>")]

    [InlineData("GenericClass<T extends BoundType1<T> & BoundType2<T>>", "GenericClass<T>\n    where T : BoundType1<T>, BoundType2<T>")]
    public void ConvertClassTypeBoundedParameters(string javaClass, string csharpClass)
    {
        string javaCode = $$"""
                                public class {{javaClass}} { }
                                """;
        var options = new JavaConversionOptions
        {
            IncludeUsings = false,
            IncludeNamespace = false,
        };
        var parsed = JavaToCSharpConverter.ConvertText(javaCode, options) ?? "";
        string expected = $$"""
                                public class {{csharpClass}}
                                {
                                }

                                """;

        Assert.Equal(expected.ReplaceLineEndings(), parsed.ReplaceLineEndings());
    }
}
