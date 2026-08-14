using Microsoft.CodeAnalysis.CSharp;

namespace JavaToCSharp.Tests;

public class EscapeIdentifierTests
{
    /// <summary>
    /// Every reserved keyword in the language, per Roslyn. Contextual keywords (e.g. <c>var</c>,
    /// <c>record</c>, <c>value</c>) are excluded: they are legal identifiers and must not be escaped.
    /// The undocumented typed-reference keywords (<c>__arglist</c> and friends) are excluded too:
    /// they are not valid Java identifiers, so they cannot reach us from a parsed source file.
    /// </summary>
    public static TheoryData<string> ReservedKeywords =>
        [..SyntaxFacts.GetReservedKeywordKinds()
            .Select(SyntaxFacts.GetText)
            .Where(keyword => !keyword.StartsWith("__", StringComparison.Ordinal))];

    [Theory]
    [MemberData(nameof(ReservedKeywords))]
    public void EscapeIdentifier_GivenReservedKeyword_ShouldPrefixWithAtSign(string keyword)
    {
        Assert.Equal("@" + keyword, TypeHelper.EscapeIdentifier(keyword));
    }

    [Theory]
    [MemberData(nameof(ReservedKeywords))]
    public void EscapeIdentifier_GivenReservedKeyword_ShouldParseAsIdentifierToken(string keyword)
    {
        var token = SyntaxFactory.ParseToken(TypeHelper.EscapeIdentifier(keyword));

        Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
        Assert.Equal(keyword, token.ValueText);
    }

    [Theory]
    [InlineData("struct")]
    [InlineData("string")]
    [InlineData("ref")]
    [InlineData("out")]
    [InlineData("in")]
    [InlineData("class")]
    [InlineData("void")]
    public void EscapeIdentifier_GivenKeyword_ShouldEscape(string keyword)
    {
        Assert.Equal("@" + keyword, TypeHelper.EscapeIdentifier(keyword));
    }

    [Theory]
    [InlineData("foo")]
    [InlineData("Struct")]      // casing matters: keywords are lowercase
    [InlineData("structure")]
    [InlineData("myClass")]
    [InlineData("var")]         // contextual keyword, legal as an identifier
    [InlineData("record")]
    [InlineData("value")]
    [InlineData("nameof")]
    [InlineData("")]
    public void EscapeIdentifier_GivenNonKeyword_ShouldReturnUnchanged(string name)
    {
        Assert.Equal(name, TypeHelper.EscapeIdentifier(name));
    }

    /// <summary>
    /// Regression test for #147: a Java parameter named after a C# keyword crashed the converter.
    /// </summary>
    [Fact]
    public void ConvertText_GivenParameterNamedAfterKeyword_ShouldEscapeParameter()
    {
        const string javaCode = """
                                public class Foo {
                                    public void bar(Structure struct) {
                                    }
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("public virtual void Bar(Structure @struct)", parsed);
    }

    [Fact]
    public void ConvertText_GivenParameterNamedAfterKeyword_ShouldEscapeUsagesOfParameter()
    {
        const string javaCode = """
                                public class Foo {
                                    public void bar(Structure struct) {
                                        struct.baz();
                                    }
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("@struct.Baz();", parsed);
    }

    [Fact]
    public void ConvertText_GivenFieldNamedAfterKeyword_ShouldEscapeFieldAccess()
    {
        const string javaCode = """
                                public class Foo {
                                    public void bar(Structure s) {
                                        s.event = 1;
                                    }
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("s.@event = 1;", parsed);
    }

    [Fact]
    public void ConvertText_GivenConstructorParameterNamedAfterKeyword_ShouldEscapeParameter()
    {
        const string javaCode = """
                                public class Foo {
                                    public Foo(int base) {
                                    }
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("public Foo(int @base)", parsed);
    }

    [Fact]
    public void ConvertText_GivenLambdaParameterNamedAfterKeyword_ShouldEscapeParameter()
    {
        const string javaCode = """
                                public class Foo {
                                    public void bar(List<String> items) {
                                        items.forEach(object -> print(object));
                                    }
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("(@object) => Print(@object)", parsed);
    }

    /// <summary>
    /// Escaped identifiers must survive a round trip through the C# parser without producing
    /// diagnostics, which is what the original crash was really about.
    /// </summary>
    [Fact]
    public void ConvertText_GivenParameterNamedAfterKeyword_ShouldProduceParseableCSharp()
    {
        const string javaCode = """
                                public class Foo {
                                    public void bar(Structure struct) {
                                        struct.baz();
                                    }
                                }
                                """;

        var parsed = Convert(javaCode);

        var tree = CSharpSyntaxTree.ParseText(parsed);

        Assert.Empty(tree.GetDiagnostics());
    }

    private static string Convert(string javaCode)
    {
        var options = new JavaConversionOptions
        {
            IncludeUsings = false,
            IncludeNamespace = false,
        };

        return JavaToCSharpConverter.ConvertText(javaCode, options) ?? "";
    }
}
