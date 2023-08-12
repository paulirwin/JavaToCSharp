using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Tests;

/// <summary>
/// Integration tests for converting Java files to C#.
/// </summary>
/// <remarks>
/// Uses some BSD-2-Clause licensed code from Jaktnat. License: https://github.com/paulirwin/jaktnat/blob/main/LICENSE
/// </remarks>
public class IntegrationTests
{
    [Theory]
    [InlineData("Resources/ArrayField.java")]
    [InlineData("Resources/SimilarityBase.java")]
    [InlineData("Resources/TestNumericDocValuesUpdates.java")]
    [InlineData("Resources/Java9DiamondOperatorInnerClass.java")]
    public void GeneralSuccessfulConversionTest(string filePath)
    {
        var options = new JavaConversionOptions
        {
            IncludeComments = false,
        };
        
        options.WarningEncountered += (_, eventArgs)
            => throw new InvalidOperationException($"Encountered a warning in conversion: {eventArgs.Message}");
        
        var parsed = JavaToCSharpConverter.ConvertText(File.ReadAllText(filePath), options);
        Assert.NotNull(parsed);
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
            => throw new InvalidOperationException($"Encountered a warning in conversion when we expected a failure: {eventArgs.Message}");
        
        Assert.ThrowsAny<Exception>(() => JavaToCSharpConverter.ConvertText(File.ReadAllText(filePath), options));
    }

    [Theory]
    [InlineData("Resources/HelloWorld.java")]
    [InlineData("Resources/Java7BasicTryWithResources.java")]
    [InlineData("Resources/Java7TryWithResources.java")]
    [InlineData("Resources/Java9TryWithResources.java")]
    [InlineData("Resources/Java9PrivateInterfaceMethods.java")]
    [InlineData("Resources/Java10TypeInference.java")]
    public void FullIntegrationTests(string filePath)
    {
        var options = new JavaConversionOptions
        {
            ConvertSystemOutToConsole = true,
            IncludeComments = false,
        };
        
        options.WarningEncountered += (_, eventArgs)
            => throw new InvalidOperationException($"Encountered a warning in conversion: {eventArgs.Message}");

        var javaText = File.ReadAllText(filePath);
        
        var parsed = JavaToCSharpConverter.ConvertText(javaText, options);
        Assert.NotNull(parsed);

        var fileName = Path.GetFileNameWithoutExtension(filePath);
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
        var syntaxTree = CSharpSyntaxTree.ParseText(cSharpLanguageText);
        
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
                throw new InvalidOperationException($"Failed to emit Roslyn assembly: {string.Join(", ", emitResult.Diagnostics)}");
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