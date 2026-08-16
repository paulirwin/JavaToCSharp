namespace JavaToCSharp.Tests;

/// <summary>
/// Tests for the Java 21 switch pattern matching labels (JEP 441) that record patterns did not
/// already cover, namely the null label and its combined `case null, default` form.
/// </summary>
public class ConvertSwitchPatternTests
{
    [Fact]
    public void Switch_Expression_Null_Label_Is_Converted_To_A_Null_Pattern()
    {
        const string javaCode = """
                                package com.example;
                                public class Shapes {
                                    public String test(Object obj) {
                                        return switch (obj) {
                                            case null -> "null";
                                            case String s -> s;
                                            default -> "other";
                                        };
                                    }
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("null => \"null\"", parsed);
        Assert.Contains("_ => \"other\"", parsed);
    }

    /// <summary>
    /// Java models `case null, default` as a null label carrying the default flag. Dropping the
    /// default half would leave the arm matching only null, so a non-null value that matched no
    /// other arm would throw at runtime instead of taking this arm.
    /// </summary>
    [Fact]
    public void Switch_Expression_Null_Default_Label_Is_Converted_To_A_Discard()
    {
        const string javaCode = """
                                package com.example;
                                public class Shapes {
                                    public String test(Object obj) {
                                        return switch (obj) {
                                            case Integer i -> "int";
                                            case null, default -> "fallback";
                                        };
                                    }
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("_ => \"fallback\"", parsed);
        Assert.DoesNotContain("null => \"fallback\"", parsed);
    }

    [Fact]
    public void Switch_Statement_Null_Default_Label_Is_Converted_To_A_Default_Section()
    {
        const string javaCode = """
                                package com.example;
                                public class Shapes {
                                    public String test(Object obj) {
                                        switch (obj) {
                                            case Integer i -> { return "int"; }
                                            case null, default -> { return "fallback"; }
                                        }
                                    }
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("default:", parsed);
        Assert.DoesNotContain("case null:", parsed);
    }

    [Fact]
    public void Switch_Expression_Over_Unrelated_Types_Uses_Type_Patterns()
    {
        const string javaCode = """
                                package com.example;
                                public class Shapes {
                                    public String test(Object obj) {
                                        return switch (obj) {
                                            case Integer i -> "int " + i;
                                            case String s -> "string " + s;
                                            default -> "other";
                                        };
                                    }
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("int i =>", parsed);
        Assert.Contains("string s =>", parsed);
    }

    /// <summary>
    /// C# has no exhaustiveness concept to carry over, so an exhaustive Java switch simply converts
    /// its arms and gains no default.
    /// </summary>
    [Fact]
    public void Exhaustive_Switch_Over_Sealed_Types_Does_Not_Gain_A_Default_Arm()
    {
        const string javaCode = """
                                package com.example;
                                public class Shapes {
                                    sealed interface Shape permits Circle, Square {}
                                    record Circle(int r) implements Shape {}
                                    record Square(int s) implements Shape {}
                                    public String test(Shape shape) {
                                        return switch (shape) {
                                            case Circle(int r) -> "circle";
                                            case Square(int s) -> "square";
                                        };
                                    }
                                }
                                """;

        var parsed = Convert(javaCode, allowWarnings: true);

        Assert.Contains("Circle (int r) =>", parsed);
        Assert.Contains("Square (int s) =>", parsed);
        Assert.DoesNotContain("_ =>", parsed);
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
