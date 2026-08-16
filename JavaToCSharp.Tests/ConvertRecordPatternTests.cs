namespace JavaToCSharp.Tests;

/// <summary>
/// Tests for Java 21 record patterns (JEP 440), which convert to C# positional patterns.
/// </summary>
public class ConvertRecordPatternTests
{
    [Fact]
    public void InstanceOf_Record_Pattern_Is_Converted_To_Positional_Pattern()
    {
        const string javaCode = """
                                package com.example;
                                public class Shapes {
                                    record Point(int x, int y) {}
                                    public boolean test(Object obj) {
                                        return obj instanceof Point(int x, int y) && x > y;
                                    }
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("obj is Point (int x, int y)", parsed);
    }

    [Fact]
    public void InstanceOf_Type_Pattern_Binds_The_Variable()
    {
        const string javaCode = """
                                package com.example;
                                public class Shapes {
                                    public String test(Object obj) {
                                        if (obj instanceof String s) {
                                            return s;
                                        }
                                        return "";
                                    }
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("obj is string s", parsed);
    }

    [Fact]
    public void InstanceOf_Without_Pattern_Remains_A_Plain_Type_Test()
    {
        const string javaCode = """
                                package com.example;
                                public class Shapes {
                                    public boolean test(Object obj) {
                                        return obj instanceof String;
                                    }
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("obj is string", parsed);
        Assert.DoesNotContain("is string s", parsed);
    }

    [Fact]
    public void Nested_Record_Pattern_Is_Converted()
    {
        const string javaCode = """
                                package com.example;
                                public class Shapes {
                                    record Point(int x, int y) {}
                                    record Line(Point start, Point end) {}
                                    public boolean test(Object obj) {
                                        return obj instanceof Line(Point(var ax, var ay), Point end);
                                    }
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("obj is Line (Point (var ax, var ay), Point end)", parsed);
    }

    [Fact]
    public void Switch_Expression_Record_Pattern_Is_Converted()
    {
        const string javaCode = """
                                package com.example;
                                public class Shapes {
                                    record Point(int x, int y) {}
                                    public String test(Object obj) {
                                        return switch (obj) {
                                            case Point(int x, int y) -> "point";
                                            default -> "other";
                                        };
                                    }
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("Point (int x, int y) =>", parsed);
    }

    [Fact]
    public void Switch_Expression_Guard_Is_Converted_To_When_Clause()
    {
        const string javaCode = """
                                package com.example;
                                public class Shapes {
                                    record Point(int x, int y) {}
                                    public String test(Object obj) {
                                        return switch (obj) {
                                            case Point(int x, int y) when x > y -> "wide";
                                            default -> "other";
                                        };
                                    }
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("when x > y =>", parsed);
    }

    [Fact]
    public void Switch_Statement_Record_Pattern_Is_Converted_With_Guard()
    {
        const string javaCode = """
                                package com.example;
                                public class Shapes {
                                    record Point(int x, int y) {}
                                    public String test(Object obj) {
                                        switch (obj) {
                                            case Point(int x, int y) when x > y:
                                                return "wide";
                                            default:
                                                return "other";
                                        }
                                    }
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("case Point (int x, int y)when x > y:", parsed);
    }

    /// <summary>
    /// Java's arrow form never falls through, but C# requires each switch section to end with a
    /// jump statement, so the conversion has to supply the break that Java leaves implicit.
    /// </summary>
    [Fact]
    public void Arrow_Switch_Statement_Cases_Get_An_Implicit_Break()
    {
        const string javaCode = """
                                package com.example;
                                public class Shapes {
                                    record Point(int x, int y) {}
                                    public void test(Object obj) {
                                        switch (obj) {
                                            case Point(int x, int y) -> System.out.println("point");
                                            default -> System.out.println("other");
                                        }
                                    }
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("case Point (int x, int y):", parsed);
        // Both the pattern case and the default case need a break.
        Assert.Equal(2, parsed.Split("break;").Length - 1);
    }

    /// <summary>
    /// A break after a return would be unreachable, which C# rejects as an error.
    /// </summary>
    [Fact]
    public void Cases_Ending_In_A_Jump_Statement_Do_Not_Get_An_Extra_Break()
    {
        const string javaCode = """
                                package com.example;
                                public class Shapes {
                                    public String test(Object obj) {
                                        switch (obj) {
                                            case String s -> { return s; }
                                            default -> { return "other"; }
                                        }
                                    }
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.DoesNotContain("break;", parsed);
    }

    /// <summary>
    /// Colon-form entries keep Java's explicit fallthrough semantics, so no break is invented.
    /// </summary>
    [Fact]
    public void Colon_Switch_Statement_Fallthrough_Is_Preserved()
    {
        const string javaCode = """
                                package com.example;
                                public class Shapes {
                                    public String test(int i) {
                                        switch (i) {
                                            case 1:
                                            case 2:
                                                return "low";
                                            default:
                                                return "high";
                                        }
                                    }
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("case 1:", parsed);
        Assert.Contains("case 2:", parsed);
        Assert.DoesNotContain("break;", parsed);
    }

    private static string Convert(string javaCode)
    {
        var options = new JavaConversionOptions { IncludeComments = false };
        options.WarningEncountered += (_, eventArgs)
            => throw new InvalidOperationException($"Encountered a warning in conversion: {eventArgs.Message}");

        return JavaToCSharpConverter.ConvertText(javaCode, options) ?? "";
    }
}
