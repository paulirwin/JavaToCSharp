using JavaToCSharp;

namespace JavaToCSharp.Tests;

/// <summary>
/// Regression tests for shared mutable state that previously corrupted conversions
/// when they ran concurrently. See the <c>voidvoid</c> return-type corruption caused by
/// <see cref="TypeNameParser"/> holding its parse state in static fields.
/// </summary>
public class ConcurrencyTests
{
    /// <summary>
    /// Hammers the type-name parser from many threads at once. Before the fix, the parser's
    /// shared static StringBuilder caused threads to splice each other's output, producing
    /// results like "voidvoid" or another thread's type name entirely.
    /// </summary>
    [Fact]
    public void ConvertType_IsThreadSafe()
    {
        (string Java, string Expected)[] cases =
        [
            ("void", "void"),
            ("String", "string"),
            ("Integer", "int"),
            ("List<String>", "IList<string>"),
            ("Map<String, Integer>", "Dictionary<string, int>"),
            ("int[]", "int[]"),
            ("List<Map<String, Object>>", "IList<Dictionary<string, object>>"),
        ];

        var failures = new System.Collections.Concurrent.ConcurrentBag<string>();

        Parallel.For(0, 2000, new ParallelOptions { MaxDegreeOfParallelism = 16 }, i =>
        {
            var (java, expected) = cases[i % cases.Length];
            var actual = TypeHelper.ConvertType(java);

            if (actual != expected)
            {
                failures.Add($"ConvertType(\"{java}\") returned \"{actual}\", expected \"{expected}\"");
            }
        });

        Assert.Empty(failures);
    }

    /// <summary>
    /// Runs full conversions in parallel. Before the fix these corrupted each other's output,
    /// which surfaced as intermittent failures in unrelated test classes.
    /// </summary>
    [Fact]
    public void Convert_IsThreadSafe()
    {
        const string java = """
            package com.example;

            public class Program {
                public static void main(String[] args) {
                    System.out.println("Hello world!");
                }

                public List<String> getNames(Map<String, Integer> input) {
                    return null;
                }
            }
            """;

        var expected = JavaToCSharpConverter.ConvertText(java, new JavaConversionOptions());
        Assert.NotNull(expected);
        Assert.Contains("void Main", expected);

        var results = new System.Collections.Concurrent.ConcurrentBag<string?>();

        Parallel.For(0, 200, new ParallelOptions { MaxDegreeOfParallelism = 16 }, _ =>
        {
            results.Add(JavaToCSharpConverter.ConvertText(java, new JavaConversionOptions()));
        });

        // Every concurrent conversion must match the single-threaded result exactly.
        Assert.All(results, r => Assert.Equal(expected, r));
    }
}
