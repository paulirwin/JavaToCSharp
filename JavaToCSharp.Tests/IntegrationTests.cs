using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit.Abstractions;

namespace JavaToCSharp.Tests;

/// <summary>
/// Integration tests for converting Java files to C#.
/// </summary>
/// <remarks>
/// Uses some BSD-2-Clause licensed code from Jaktnat. License: https://github.com/paulirwin/jaktnat/blob/main/LICENSE
/// </remarks>
public class IntegrationTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("Resources/ArrayField.java")]
    [InlineData("Resources/SimilarityBase.java")]
    [InlineData("Resources/TestNumericDocValuesUpdates.java")]
    [InlineData("Resources/Java9DiamondOperatorInnerClass.java")]
    [InlineData("Resources/Java11LambdaInference.java")]
    [InlineData("Resources/MultidimensionalArrays.java", true)]
    [InlineData("Resources/Java17SealedClasses.java", true)]
    // Conversion-only: java.util.function has no BCL delegate mapping, so the output cannot be run.
    [InlineData("Resources/Java8MethodReferences.java")]
    public void GeneralSuccessfulConversionTest(string filePath, bool allowWarnings = false)
    {
        var options = new JavaConversionOptions
        {
            IncludeComments = false,
        };

        options.WarningEncountered += (_, eventArgs) =>
        {
            if (!allowWarnings)
            {
                throw new InvalidOperationException($"Encountered a warning in conversion: {eventArgs.Message}");
            }
        };

        var parsed = JavaToCSharpConverter.ConvertText(File.ReadAllText(filePath), options);

        Assert.NotNull(parsed);

        testOutputHelper.WriteLine(parsed);
    }

    [Theory]
    [InlineData("Resources/Java9Underscore.java")]
    public void GeneralUnsuccessfulConversionTest(string filePath)
    {
        var options = new JavaConversionOptions
        {
            IncludeComments = false,
        };

        options.WarningEncountered += (_, eventArgs)
            => throw new InvalidOperationException(
                $"Encountered a warning in conversion when we expected a failure: {eventArgs.Message}");

        Assert.ThrowsAny<Exception>(() => JavaToCSharpConverter.ConvertText(File.ReadAllText(filePath), options));
    }

    [Theory]
    [InlineData("Resources/HelloWorld.java")]
    [InlineData("Resources/Java7BasicTryWithResources.java")]
    [InlineData("Resources/Java7TryWithResources.java")]
    [InlineData("Resources/Java9TryWithResources.java")]
    [InlineData("Resources/Java9PrivateInterfaceMethods.java")]
    [InlineData("Resources/Java10TypeInference.java")]
    [InlineData("Resources/Java14SwitchExpressions.java")]
    [InlineData("Resources/Java14SwitchExpressionsYield.java", true)]
    [InlineData("Resources/Java14SwitchExpressionsYieldReturn.java", true)]
    [InlineData("Resources/Java14SwitchExpressionsYieldAssign.java", true)]
    [InlineData("Resources/Java15TextBlocks.java")]
    [InlineData("Resources/Java16Records.java")]
    [InlineData("Resources/Java21RecordPatterns.java")]
    // Warnings are expected: the sealed interface has no C# equivalent.
    [InlineData("Resources/Java21SwitchPatternMatching.java", true)]
    [InlineData("Resources/NewArrayLiteralBug.java")]
    [InlineData("Resources/OctalLiteralBug.java")]
    [InlineData("Resources/DeprecatedAnnotation.java")]
    [InlineData("Resources/BooleanArrays.java")]
    [InlineData("Resources/BinaryLiterals.java")]
    [InlineData("Resources/NestedEnumStaticUsing.java")]
    [InlineData("Resources/Java16LocalRecords.java")]
    [InlineData("Resources/InstanceInitializers.java")]
    [InlineData("Resources/StaticImports.java")]
    [InlineData("Resources/LabeledBreakContinue.java")]
    [InlineData("Resources/ExceptionGetMessage.java")]
    [InlineData("Resources/LongLiterals.java")]
    public void FullIntegrationTests(string filePath, bool allowWarnings = false)
        => RunFullIntegrationTest(filePath, allowWarnings);

    /// <summary>
    /// Runs the labeled break/continue sample through the <c>goto</c> fallback, to confirm it behaves
    /// identically to the C# 15 labeled jumps covered by <see cref="FullIntegrationTests"/>.
    /// </summary>
    [Theory]
    [InlineData("Resources/LabeledBreakContinue.java")]
    public void FullIntegrationTestsWithGotoFallback(string filePath)
        => RunFullIntegrationTest(filePath, allowWarnings: false, useLabeledBreakAndContinue: false);

    private void RunFullIntegrationTest(string filePath, bool allowWarnings, bool useLabeledBreakAndContinue = true)
    {
        var options = new JavaConversionOptions
        {
            ConvertSystemOutToConsole = true,
            IncludeComments = false,
            UseLabeledBreakAndContinue = useLabeledBreakAndContinue,
        };

        options.AddUsing("System");

        options.WarningEncountered += (_, eventArgs) =>
        {
            if (!allowWarnings)
            {
                throw new InvalidOperationException($"Encountered a warning in conversion: {eventArgs.Message}");
            }
        };

        var javaText = File.ReadAllText(filePath);

        var parsed = JavaToCSharpConverter.ConvertText(javaText, options);

        Assert.NotNull(parsed);

        testOutputHelper.WriteLine(parsed);

        // The suffix keeps the two option variants in separate assemblies, because Assembly.LoadFile
        // caches by path and would otherwise reuse the first variant's compiled output.
        var fileName = Path.GetFileNameWithoutExtension(filePath)
                       + (useLabeledBreakAndContinue ? "" : "_Goto");
        var assembly = CompileAssembly(fileName, parsed);

        var expectation = ParseExpectation(javaText);

        // NOTE: examples must have a class name of Program in the example package
        var programType = assembly.GetType("Example.Program");

        if (programType is null)
        {
            throw new InvalidOperationException("Cannot find expected Program type in assembly");
        }

        var mainMethod = programType.GetMethod("Main", BindingFlags.Static | BindingFlags.Public);

        if (mainMethod is null)
        {
            throw new InvalidOperationException("Cannot find expected Main method in assembly");
        }

        using var sw = new StringWriter();
        Console.SetOut(sw);

        try
        {
            mainMethod.Invoke(null, new object[] { Array.Empty<string>() });
        }
        catch
        {
            if (expectation.Error == null)
            {
                throw;
            }

            return;
        }

        var output = sw.ToString().ReplaceLineEndings("\n");

        if (expectation.Output != null)
        {
            Assert.Equal(expectation.Output, output);
        }
        else if (expectation.Error != null)
        {
            throw new InvalidOperationException("Expected an error, but app ran successfully");
        }
        else
        {
            throw new InvalidOperationException("Test must have either an output or error expectation");
        }
    }

    private static Assembly CompileAssembly(string assemblyName, string cSharpLanguageText)
    {
        // Preview is required for the C# 15 features the converter emits, such as labeled break/continue.
        var syntaxTree = CSharpSyntaxTree.ParseText(
            cSharpLanguageText,
            new CSharpParseOptions(LanguageVersion.Preview));

        var options = new CSharpCompilationOptions(OutputKind.ConsoleApplication)
            .WithOverflowChecks(true)
            .WithOptimizationLevel(OptimizationLevel.Debug);

        var compilation = CSharpCompilation.Create(assemblyName,
            new List<SyntaxTree> { syntaxTree },
            GetMetadataReferencesForBcl(),
            options);

        var outputDir = Path.Join(Environment.CurrentDirectory, "bin");
        Directory.CreateDirectory(outputDir);

        var outputFile = Path.Join(outputDir, $"{assemblyName}.exe");

        using (var ms = new MemoryStream())
        {
            var emitResult = compilation.Emit(ms);

            if (!emitResult.Success)
            {
                throw new InvalidOperationException(
                    $"Failed to emit Roslyn assembly: {string.Join(", ", emitResult.Diagnostics)}");
            }

            ms.Position = 0;

            using (var writer = File.OpenWrite(outputFile))
            {
                ms.CopyTo(writer);
                writer.Flush(true);
            }
        }

        const string runtimeConfigJson = @"{
  ""runtimeOptions"": {
    ""tfm"": ""net6.0"",
    ""framework"": {
      ""name"": ""Microsoft.NETCore.App"",
      ""version"": ""6.0.0""
    }
  }
}";
        File.WriteAllText(Path.Join(outputDir, $"{assemblyName}.runtimeconfig.json"), runtimeConfigJson);

        return Assembly.LoadFile(outputFile);
    }

    private static IEnumerable<MetadataReference> GetMetadataReferencesForBcl()
    {
        var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);

        if (assemblyPath != null)
        {
            yield return MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Private.CoreLib.dll"));
            yield return MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Console.dll"));
            yield return MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Linq.dll"));
            yield return MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll"));
        }
    }

    private static Expectation ParseExpectation(string contents)
    {
        using var sr = new StringReader(contents);
        var expectation = new Expectation();

        while (sr.ReadLine() is { } line && line.StartsWith("///"))
        {
            line = line.TrimStart('/', ' ');

            if (!line.StartsWith("-"))
            {
                continue;
            }

            line = line.TrimStart('-', ' ');

            if (line.StartsWith("output: "))
            {
                line = line["output: ".Length..];

                // HACK.PI: use C# string escape format for output string
                var syntax = CSharpSyntaxTree.ParseText(line);

                var root = syntax.GetRoot();

                var literal = FindLiteralExpressionSyntax(root);

                if (literal != null)
                {
                    expectation.Output = literal.Token.ValueText;
                }
                else
                {
                    throw new InvalidOperationException("Unable to parse output expectation as a string");
                }
            }
            else if (line.StartsWith("error: "))
            {
                line = line["error: ".Length..];

                expectation.Error = line; // TODO: validate error message
            }
        }

        return expectation;
    }

    private static LiteralExpressionSyntax? FindLiteralExpressionSyntax(SyntaxNode node)
    {
        if (node is LiteralExpressionSyntax literal)
        {
            return literal;
        }

        return node.ChildNodes()
            .Select(FindLiteralExpressionSyntax)
            .FirstOrDefault(childLiteral => childLiteral != null);
    }

    private class Expectation
    {
        public string? Output { get; set; }

        public string? Error { get; set; }
    }
}
