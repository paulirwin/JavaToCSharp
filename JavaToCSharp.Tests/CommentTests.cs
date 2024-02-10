using Microsoft.CodeAnalysis.CSharp;
using Xunit.Abstractions;

namespace JavaToCSharp.Tests;

public class CommentTests(ITestOutputHelper testOutputHelper)
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

        testOutputHelper.WriteLine(parsed);

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

    [Fact]
    public void CommentsBeforePackage_ShouldRemainAtTopOfFile_WithUsings()
    {
        const string javaCode = """
                                // 1 comment
                                // 2 comment
                                // Red comment
                                // Blue comment
                                package com.example.code;

                                // Import some package
                                import java.something.Bar;
                                
                                // Import some other package
                                import java.something.other.Baz;

                                // Define some class
                                public class Foo {
                                }
                                """;
        var options = new JavaConversionOptions
        {
            StartInterfaceNamesWithI = true,
        };

        options.Usings.Clear();

        var parsed = JavaToCSharpConverter.ConvertText(javaCode, options) ?? "";

        testOutputHelper.WriteLine(parsed);

        // TODO.PI: improve whitespace
        const string expected = """
                                // 1 comment
                                // 2 comment
                                // Red comment
                                // Blue comment
                                // Import some package
                                using Java.Something;
                                // Import some other package
                                using Java.Something.Other;

                                namespace Com.Example.Code
                                {
                                    // Define some class
                                    public class Foo
                                    {
                                    }
                                }
                                """;

        Assert.Equal(expected.ReplaceLineEndings(), parsed.ReplaceLineEndings());
    }
}
