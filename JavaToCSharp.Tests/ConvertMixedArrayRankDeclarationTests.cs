namespace JavaToCSharp.Tests;

/// <summary>
/// Java permits C-style array brackets on individual declarators, so a single declaration can mix
/// array ranks. C# has no equivalent, so these must be split into one declaration per rank.
/// </summary>
public class ConvertMixedArrayRankDeclarationTests
{
    [Fact]
    public void Mixed_Ranks_Split_Into_Separate_Declarations()
    {
        var parsed = Convert("""
                             package com.example;
                             public class Program {
                                 public void run() {
                                     int single[] = new int[2], scalar = 7;
                                 }
                             }
                             """);

        Assert.Contains("int[] single = new int[2];", parsed);
        Assert.Contains("int scalar = 7;", parsed);
    }

    [Fact]
    public void Declarators_Of_The_Same_Rank_Stay_In_One_Declaration()
    {
        var parsed = Convert("""
                             package com.example;
                             public class Program {
                                 public void run() {
                                     int a[] = new int[1], b = 0, c[] = new int[2];
                                 }
                             }
                             """);

        // `a` and `c` share a rank, so they must remain a single declaration rather than being
        // split one-per-declarator.
        Assert.Contains("int[] a = new int[1], c = new int[2];", parsed);
        Assert.Contains("int b = 0;", parsed);
    }

    [Fact]
    public void Declaration_Groups_Are_Emitted_As_Siblings_In_Declaration_Order()
    {
        var parsed = Convert("""
                             package com.example;
                             public class Program {
                                 public void run() {
                                     int single[] = new int[2], scalar = 7;
                                 }
                             }
                             """);

        int arrayDecl = parsed.IndexOf("int[] single", StringComparison.Ordinal);
        int scalarDecl = parsed.IndexOf("int scalar", StringComparison.Ordinal);

        Assert.True(arrayDecl > 0 && scalarDecl > 0);
        Assert.True(arrayDecl < scalarDecl, "Groups must preserve the original declaration order.");

        // The split must not introduce a nested scope, which would put the variables out of reach
        // of later statements in the enclosing block.
        Assert.DoesNotContain("{\n            {", parsed.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void Uninitialized_Declarators_Are_Preserved_When_Split()
    {
        var parsed = Convert("""
                             package com.example;
                             public class Program {
                                 public void run() {
                                     int values[], count = 0;
                                 }
                             }
                             """);

        Assert.Contains("int[] values;", parsed);
        Assert.Contains("int count = 0;", parsed);
    }

    [Fact]
    public void Single_Rank_Declarations_Are_Unaffected()
    {
        var parsed = Convert("""
                             package com.example;
                             public class Program {
                                 public void run() {
                                     int x = 1, y = 2;
                                 }
                             }
                             """);

        Assert.Contains("int x = 1, y = 2;", parsed);
    }

    [Fact]
    public void Mixed_Ranks_Convert_Without_Error()
    {
        // Regression test for #100: asking JavaParser for a common type across mixed ranks
        // threw "The variables do not have a common type."
        var warnings = new List<string>();

        var parsed = Convert("""
                             package com.example;
                             public class Program {
                                 public void run() {
                                     int multi[][] = new int[2][2], single[] = new int[2];
                                 }
                             }
                             """, warnings);

        // The two ranks must land in separate declarations. Note the 2-D array is emitted as a
        // rectangular `int[,]` rather than a jagged `int[][]`; that is a pre-existing limitation
        // independent of the mixed-rank split under test here.
        Assert.Contains("int[, ] multi = new int[2, 2];", parsed);
        Assert.Contains("int[] single = new int[2];", parsed);

        // The only warning permitted here is the pre-existing multi-dimensional array caveat.
        Assert.All(warnings, w => Assert.Contains("Multi-dimensional arrays", w));
    }

    private static string Convert(string javaCode, List<string>? warnings = null)
    {
        var options = new JavaConversionOptions
        {
            IncludeComments = false,
        };

        options.WarningEncountered += (_, eventArgs) => warnings?.Add(eventArgs.Message);

        return JavaToCSharpConverter.ConvertText(javaCode, options) ?? "";
    }
}
