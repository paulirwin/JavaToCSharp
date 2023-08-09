using System;
using Xunit;

namespace JavaToCSharp.Tests;

public class IntegrationTests
{
    [Theory]
    [InlineData("Resources/ArrayField.java")]
    [InlineData("Resources/SimilarityBase.java")]
    [InlineData("Resources/TestNumericDocValuesUpdates.java")]
    public void GeneralSuccessfulConversionTest(string filePath)
    {
        var options = new JavaConversionOptions();
        options.WarningEncountered += (_, eventArgs)
            => Console.WriteLine("Line {0}: {1}", eventArgs.JavaLineNumber, eventArgs.Message);
        var parsed = JavaToCSharpConverter.ConvertText(System.IO.File.ReadAllText(filePath), options);
        Assert.NotNull(parsed);
    }
}