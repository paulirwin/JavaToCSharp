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

    [Theory]
    [InlineData("Child", "Child")]
    [InlineData("Child extends Parent", "Child : Parent")]
    [InlineData("Child implements Parent", "Child : Parent")]
    [InlineData("Child extends Parent implements IParent", "Child : Parent, IParent")]

    [InlineData("Parent<T>", "Parent<T>")]
    [InlineData("Child<T extends BoundType<T>>", "Child<T>")] // issue #125, should add: where T : BoundType<T>
    [InlineData("Child extends Parent<BoundType>", "Child : Parent<BoundType>")]
    public void CommentsInsideClass_ShouldNotBeDuplicated_Fix_88(string javaClass, string csharpClass)
    {
        string javaCode = $$"""
                                //class comment
                                public class {{javaClass}} {
                                    //before comment 1
                                    public void method1() {
                                        doSomething(); //after comment1
                                    }
                                    //before comment 2
                                    public void method1() {
                                        doSomething(); //after comment2
                                    }
                                    //before comment 3
                                    public void method1() {
                                    }
                                }
                                """;
        var options = new JavaConversionOptions
        {
            IncludeUsings = false,
            IncludeNamespace = false,
        };

        var parsed = JavaToCSharpConverter.ConvertText(javaCode, options) ?? "";

        testOutputHelper.WriteLine(parsed);

        string expected = $$"""
                                //class comment
                                public class {{csharpClass}}
                                {
                                    //before comment 1
                                    public virtual void Method1()
                                    {
                                        DoSomething(); //after comment1
                                    }

                                    //before comment 2
                                    public virtual void Method1()
                                    {
                                        DoSomething(); //after comment2
                                    }

                                    //before comment 3
                                    public virtual void Method1()
                                    {
                                    }
                                }

                                """;

        Assert.Equal(expected.ReplaceLineEndings(), parsed.ReplaceLineEndings());
    }
}
