namespace JavaToCSharp.Tests;

public class ConvertRecordTests
{
    [Fact]
    public void Record_Is_Converted_To_Positional_Record()
    {
        const string javaCode = """
                                package com.example;
                                public record Point(int x, int y) {}
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("public record Point(int x, int y)", parsed);
    }

    [Fact]
    public void Record_Component_Names_Are_Preserved()
    {
        // Bodies reference components directly (`x + y`) and those references are not rewritten,
        // so renaming the components would produce code that does not compile.
        const string javaCode = """
                                package com.example;
                                public record Point(int x, int y) {
                                    public int sum() { return x + y; }
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("(int x, int y)", parsed);
        Assert.Contains("return x + y;", parsed);
    }

    [Fact]
    public void Record_Implements_Interface()
    {
        const string javaCode = """
                                package com.example;
                                public record Circle(int radius) implements Shape {}
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("public record Circle(int radius) : Shape", parsed);
    }

    [Fact]
    public void Generic_Record_Emits_Type_Parameters()
    {
        const string javaCode = """
                                package com.example;
                                public record Labeled<T>(String label, T value) {}
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("public record Labeled<T>(string label, T value)", parsed);
    }

    [Fact]
    public void Nested_Record_Is_Converted()
    {
        const string javaCode = """
                                package com.example;
                                public class Holder {
                                    public record Inner(int a) {}
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("public record Inner(int a)", parsed);
    }

    [Fact]
    public void Record_Static_Member_Is_Converted()
    {
        const string javaCode = """
                                package com.example;
                                public record Point(int x, int y) {
                                    public static final Point ORIGIN = new Point(0, 0);
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("public static readonly Point ORIGIN", parsed);
    }

    [Fact]
    public void Secondary_Constructor_Delegates_To_Primary_Constructor()
    {
        const string javaCode = """
                                package com.example;
                                public record Point(int x, int y) {
                                    public Point(int v) { this(v, v); }
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("public Point(int v) : this(v, v)", parsed);
    }

    [Fact]
    public void Compact_Constructor_Warns_And_Is_Not_Ported()
    {
        const string javaCode = """
                                package com.example;
                                public record Point(int x, int y) {
                                    public Point {
                                        if (x < 0) throw new IllegalArgumentException("neg");
                                    }
                                }
                                """;

        var warnings = new List<string>();
        Convert(javaCode, NewOptions(warnings));

        Assert.Contains(warnings, w => w.Contains("Compact constructor"));
    }

    [Fact]
    public void Canonical_Constructor_Warns_Because_It_Conflicts_With_Primary_Constructor()
    {
        const string javaCode = """
                                package com.example;
                                public record Point(int x, int y) {
                                    public Point(int x, int y) { this.x = x; this.y = y; }
                                }
                                """;

        var warnings = new List<string>();
        var parsed = Convert(javaCode, NewOptions(warnings));

        Assert.Contains(warnings, w => w.Contains("Canonical constructor"));
        // Emitting it would duplicate the generated primary constructor (CS0111), so the record
        // is left with no body at all.
        Assert.Contains("public record Point(int x, int y);", parsed);
    }

    [Fact]
    public void Explicit_Accessor_Warns_Because_It_Conflicts_With_Generated_Property()
    {
        const string javaCode = """
                                package com.example;
                                public record Point(int x, int y) {
                                    public int x() { return Math.abs(x); }
                                }
                                """;

        var warnings = new List<string>();
        var parsed = Convert(javaCode, NewOptions(warnings));

        Assert.Contains(warnings, w => w.Contains("Accessor `x()`"));
        Assert.DoesNotContain("int X()", parsed);
    }

    [Fact]
    public void Record_Without_Members_Ends_With_Semicolon()
    {
        const string javaCode = """
                                package com.example;
                                public record Point(int x, int y) {}
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("public record Point(int x, int y);", parsed);
    }

    private static JavaConversionOptions NewOptions(List<string>? warnings = null)
    {
        var options = new JavaConversionOptions { IncludeComments = false };
        options.WarningEncountered += (_, eventArgs) => warnings?.Add(eventArgs.Message);
        return options;
    }

    private static string Convert(string javaCode, JavaConversionOptions? options = null)
        => JavaToCSharpConverter.ConvertText(javaCode, options ?? NewOptions()) ?? "";
}
