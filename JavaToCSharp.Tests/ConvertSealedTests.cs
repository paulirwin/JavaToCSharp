namespace JavaToCSharp.Tests;

public class ConvertSealedTests
{
    [Fact]
    public void Sealed_Class_Is_Converted_To_Closed_Class_By_Default()
    {
        const string javaCode = """
                                package com.example;
                                public sealed class Shape
                                    permits Circle, Square, Rectangle {
                                }
                                """;

        var warnings = new List<string>();
        var parsed = Convert(javaCode, NewOptions(warnings));

        Assert.Contains("// Java: sealed, permits Circle, Square, Rectangle", parsed);
        Assert.Contains("public closed class Shape", parsed);
        Assert.Empty(warnings);
    }

    [Fact]
    public void Sealed_Class_Omits_Closed_Modifier_And_Warns_When_Option_Disabled()
    {
        const string javaCode = """
                                package com.example;
                                public sealed class Shape
                                    permits Circle, Square, Rectangle {
                                }
                                """;

        var warnings = new List<string>();
        var options = NewOptions(warnings);
        options.UseClosedForSealedClasses = false;

        var parsed = Convert(javaCode, options);

        Assert.Contains("// Java: sealed, permits Circle, Square, Rectangle", parsed);
        Assert.Contains("public class Shape", parsed);
        Assert.DoesNotContain("closed", parsed);
        Assert.Contains(warnings, w => w.Contains("Sealed class Shape"));
    }

    [Fact]
    public void Sealed_Interface_Is_Converted_To_Plain_Interface_With_Warning()
    {
        const string javaCode = """
                                package com.example;
                                public sealed interface Service permits Alpha, Beta {
                                    void run();
                                }
                                """;

        var warnings = new List<string>();
        var parsed = Convert(javaCode, NewOptions(warnings));

        Assert.Contains("// Java: sealed, permits Alpha, Beta", parsed);
        Assert.Contains("public interface Service", parsed);
        Assert.DoesNotContain("closed", parsed);
        Assert.Contains(warnings, w => w.Contains("Sealed interface Service"));
    }

    [Fact]
    public void Non_Sealed_Class_Is_Converted_Without_Modifier_With_Warning()
    {
        const string javaCode = """
                                package com.example;
                                public non-sealed class Square extends Shape {
                                    public double side;
                                }
                                """;

        var warnings = new List<string>();
        var parsed = Convert(javaCode, NewOptions(warnings));

        Assert.Contains("// Java: non-sealed", parsed);
        Assert.Contains("public class Square : Shape", parsed);
        Assert.DoesNotContain("closed", parsed);
        Assert.Contains(warnings, w => w.Contains("Non-sealed class Square"));
    }

    [Fact]
    public void Sealed_Class_Without_Permits_Clause_Omits_Permits_From_Comment()
    {
        const string javaCode = """
                                package com.example;
                                public sealed class Shape {
                                    public static final class Circle extends Shape {}
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("// Java: sealed", parsed);
        Assert.DoesNotContain("permits", parsed);
    }

    [Fact]
    public void Ordinary_Class_Has_No_Sealed_Comment_Or_Warning()
    {
        const string javaCode = """
                                package com.example;
                                public class Shape {
                                }
                                """;

        var warnings = new List<string>();
        var parsed = Convert(javaCode, NewOptions(warnings));

        Assert.DoesNotContain("// Java:", parsed);
        Assert.DoesNotContain("closed", parsed);
        Assert.Empty(warnings);
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
