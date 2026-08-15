namespace JavaToCSharp.Tests;

public class ConvertEnumTests
{
    [Fact]
    public void Enum_In_Same_File_Emits_Static_Using()
    {
        const string javaCode = """
                                package com.example;
                                public enum Color { RED, GREEN }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("using static Com.Example.Color;", parsed);
    }

    [Fact]
    public void Static_Usings_Do_Not_Leak_Into_Later_Files_Sharing_Options()
    {
        const string enumCode = """
                                package com.example;
                                public enum Color { RED, GREEN }
                                """;
        const string classCode = """
                                 package com.example;
                                 public class Foo {
                                 }
                                 """;

        // The same options instance is reused across files, as the CLI and GUI do.
        var options = NewOptions();

        var enumResult = Convert(enumCode, options);
        var classResult = Convert(classCode, options);

        Assert.Contains("using static Com.Example.Color;", enumResult);
        Assert.DoesNotContain("using static", classResult);
    }

    [Fact]
    public void Static_Usings_Do_Not_Leak_Across_Namespaces()
    {
        const string enumCode = """
                                package com.foo;
                                public enum Color { RED, GREEN }
                                """;
        const string classCode = """
                                 package com.bar;
                                 public class Foo {
                                 }
                                 """;

        var options = NewOptions();

        Convert(enumCode, options);
        var classResult = Convert(classCode, options);

        // Leaking here previously produced "using static Com.Bar.Color;", a
        // reference to a type that does not exist in that namespace.
        Assert.DoesNotContain("Color", classResult);
    }

    [Fact]
    public void Nested_Enum_Static_Using_Includes_Declaring_Type()
    {
        const string javaCode = """
                                package com.example;
                                public class Holder {
                                    public enum Inner { A, B }
                                }
                                """;

        var parsed = Convert(javaCode);

        // The C# type is Com.Example.Holder.Inner; a using naming only the enum
        // would not resolve.
        Assert.Contains("using static Com.Example.Holder.Inner;", parsed);
    }

    [Fact]
    public void Deeply_Nested_Enum_Static_Using_Includes_Full_Path()
    {
        const string javaCode = """
                                package com.example;
                                public class Outer {
                                    public static class Middle {
                                        public enum Deep { A }
                                    }
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("using static Com.Example.Outer.Middle.Deep;", parsed);
    }

    [Fact]
    public void Enum_Nested_In_Interface_Static_Using_Includes_Declaring_Type()
    {
        const string javaCode = """
                                package com.example;
                                public interface Contract {
                                    enum Kind { A, B }
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("using static Com.Example.Contract.Kind;", parsed);
    }

    [Fact]
    public void Same_Enum_Name_At_Different_Scopes_Emits_Distinct_Static_Usings()
    {
        const string javaCode = """
                                package com.example;
                                public enum Color { RED }
                                public class Holder {
                                    public enum Color { GREEN }
                                }
                                """;

        var parsed = Convert(javaCode);

        // These are two different C# types and each needs its own static using.
        Assert.Contains("using static Com.Example.Color;", parsed);
        Assert.Contains("using static Com.Example.Holder.Color;", parsed);
    }

    [Fact]
    public void Static_Using_Is_Not_Duplicated_For_A_Single_Enum()
    {
        const string javaCode = """
                                package com.example;
                                public enum Color { RED, GREEN }
                                """;

        var parsed = Convert(javaCode);

        var occurrences = parsed.Split("using static Com.Example.Color;").Length - 1;
        Assert.Equal(1, occurrences);
    }

    private static JavaConversionOptions NewOptions()
    {
        var options = new JavaConversionOptions { IncludeComments = false };
        options.WarningEncountered += (_, eventArgs)
            => Console.WriteLine("Line {0}: {1}", eventArgs.JavaLineNumber, eventArgs.Message);
        return options;
    }

    private static string Convert(string javaCode, JavaConversionOptions? options = null)
        => JavaToCSharpConverter.ConvertText(javaCode, options ?? NewOptions()) ?? "";
}
