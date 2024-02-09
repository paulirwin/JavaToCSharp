namespace JavaToCSharp.Tests;

public class CommentTests
{
    [Fact]
    public void CommentsBeforePackage_ShouldRemainAtTopOfFile()
    {
        const string javaCode = """
                                // 1 comment
                                // 2 comment
                                // Red comment
                                // Blue comment
                                package com.example.code;

                                import java.something.Bar;

                                public class Foo {
                                }
                                """;
        var options = new JavaConversionOptions
        {
            StartInterfaceNamesWithI = true,
        };

        options.Usings.Clear();

        var parsed = JavaToCSharpConverter.ConvertText(javaCode, options) ?? "";

        const string expected = """
                                // 1 comment
                                // 2 comment
                                // Red comment
                                // Blue comment
                                using Java.Something;

                                namespace Com.Example.Code
                                {
                                    public class Foo
                                    {
                                    }
                                }
                                """;

        Assert.Equal(expected.ReplaceLineEndings(), parsed.ReplaceLineEndings());
    }
}
