namespace JavaToCSharp.Tests;

/// <summary>
/// Tests for Java constructs at or below Java 21 that previously failed to convert or converted
/// into code with different runtime behavior than the Java source.
/// </summary>
public class ConvertJava21GapTests
{
    /// <summary>
    /// Local records previously threw, since <c>LocalRecordDeclarationStmt</c> was missing from the
    /// statement visitor registry, aborting conversion of the entire file. C# has no local record
    /// declaration (the compiler reads one as a local function), so it is hoisted to the enclosing
    /// type rather than left in the method body.
    /// </summary>
    [Fact]
    public void Local_Record_Declaration_Is_Hoisted_To_The_Enclosing_Type()
    {
        const string javaCode = """
                                package com.example;
                                public class Shapes {
                                    public int test() {
                                        record Point(int x, int y) {}
                                        Point p = new Point(1, 2);
                                        return p.x;
                                    }
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("record Point(int x, int y)", parsed);

        // The declaration must sit outside the method body, after the method that declared it.
        var methodStart = parsed.IndexOf("public virtual int Test()", StringComparison.Ordinal);
        var recordStart = parsed.IndexOf("record Point", StringComparison.Ordinal);
        var methodEnd = parsed.IndexOf("return p.x;", StringComparison.Ordinal);

        Assert.True(methodStart >= 0 && recordStart > methodEnd, "The local record must be hoisted out of the method body.");
    }

    /// <summary>
    /// A method reference is a method group, not a call. Emitting an invocation would call the
    /// method at the point of reference instead of passing it as a delegate.
    /// </summary>
    [Fact]
    public void Method_Reference_Is_Converted_To_A_Method_Group()
    {
        const string javaCode = """
                                package com.example;
                                import java.util.List;
                                import java.util.function.Function;
                                public class Shapes {
                                    public Function<String, Integer> test(List<String> items) {
                                        items.forEach(System.out::println);
                                        return String::length;
                                    }
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("return string.Length;", parsed);
        Assert.DoesNotContain("string.Length()", parsed);
    }

    /// <summary>
    /// C# has no constructor-reference syntax, so <c>Foo::new</c> becomes a lambda. Generic
    /// arguments on the referenced type must survive the conversion.
    /// </summary>
    [Fact]
    public void Constructor_Reference_Is_Converted_To_A_Lambda()
    {
        const string javaCode = """
                                package com.example;
                                import java.util.ArrayList;
                                import java.util.function.Supplier;
                                public class Shapes {
                                    public Supplier<ArrayList<String>> test() {
                                        return ArrayList<String>::new;
                                    }
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("() => new List<string>()", parsed);
    }

    /// <summary>
    /// A static import names the declaring type, so the type must be retained and the directive
    /// emitted as <c>using static</c>. Treating it as a namespace import dropped the type name.
    /// </summary>
    [Fact]
    public void Static_Import_Is_Converted_To_A_Using_Static()
    {
        const string javaCode = """
                                package com.example;
                                import static java.lang.Math.max;
                                public class Shapes {
                                    public int test(int a, int b) {
                                        return max(a, b);
                                    }
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("using static Java.Lang.Math;", parsed);
    }

    /// <summary>
    /// A non-static import must keep converting to a plain namespace using, with the class name
    /// stripped off.
    /// </summary>
    [Fact]
    public void Non_Static_Import_Remains_A_Namespace_Using()
    {
        const string javaCode = """
                                package com.example;
                                import java.util.List;
                                public class Shapes {
                                    public int test(List<String> items) {
                                        return items.size();
                                    }
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("using Java.Util;", parsed);
        Assert.DoesNotContain("using static", parsed);
    }

    /// <summary>
    /// Java runs an instance initializer at the start of every constructor. It was previously
    /// emitted as a static constructor, which runs once per type rather than once per instance.
    /// </summary>
    [Fact]
    public void Instance_Initializer_Is_Prepended_To_Each_Constructor()
    {
        const string javaCode = """
                                package com.example;
                                public class Shapes {
                                    private int x;
                                    { x = 42; }
                                    public Shapes() { }
                                    public Shapes(int y) { this.x = y; }
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.DoesNotContain("static Shapes()", parsed);

        // Both constructors run the initializer, and it runs before the constructor's own body so a
        // constructor parameter still wins over the initialized value.
        var body = parsed[parsed.IndexOf("public Shapes(int y)", StringComparison.Ordinal)..];
        Assert.Contains("x = 42;", body);
        Assert.True(
            body.IndexOf("x = 42;", StringComparison.Ordinal) < body.IndexOf("this.x = y;", StringComparison.Ordinal),
            "The instance initializer must run before the constructor body.");
    }

    /// <summary>
    /// A constructor chaining to <c>this(...)</c> must not re-run the initializer, since it already
    /// ran in the constructor being chained to.
    /// </summary>
    [Fact]
    public void Instance_Initializer_Is_Not_Repeated_In_A_Chained_Constructor()
    {
        const string javaCode = """
                                package com.example;
                                public class Shapes {
                                    private int x;
                                    { x = 42; }
                                    public Shapes(int y) { this.x = y; }
                                    public Shapes(String s) { this(s.length()); }
                                }
                                """;

        var parsed = Convert(javaCode);

        var chained = parsed[parsed.IndexOf("public Shapes(string s)", StringComparison.Ordinal)..];
        Assert.DoesNotContain("x = 42;", chained);
    }

    /// <summary>
    /// A class with an instance initializer but no declared constructor needs one synthesized,
    /// otherwise the initializer would be dropped.
    /// </summary>
    [Fact]
    public void Instance_Initializer_Without_A_Constructor_Synthesizes_One()
    {
        const string javaCode = """
                                package com.example;
                                public class Shapes {
                                    private int x;
                                    { x = 42; }
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("public Shapes()", parsed);
        Assert.Contains("x = 42;", parsed);
    }

    /// <summary>
    /// Static initializers must still become static constructors.
    /// </summary>
    [Fact]
    public void Static_Initializer_Remains_A_Static_Constructor()
    {
        const string javaCode = """
                                package com.example;
                                public class Shapes {
                                    private static int x;
                                    static { x = 42; }
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("static Shapes()", parsed);
    }

    private static string Convert(string javaCode, bool allowWarnings = false)
    {
        var options = new JavaConversionOptions { IncludeComments = false };

        options.WarningEncountered += (_, eventArgs) =>
        {
            if (!allowWarnings)
            {
                throw new InvalidOperationException($"Encountered a warning in conversion: {eventArgs.Message}");
            }
        };

        return JavaToCSharpConverter.ConvertText(javaCode, options) ?? "";
    }
}
