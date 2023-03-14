using System;
using Xunit;

namespace JavaToCSharp.Tests
{
    public class IntegrationTests
    {
        [Theory]
        [InlineData("Resources/SimilarityBase.java")]
        [InlineData("Resources/TestNumericDocValuesUpdates.java")]
        public void TestCommentsCanBeConverted(string filePath)
        {
            var options = new JavaConversionOptions();
            options.WarningEncountered += (sender, eventArgs)
                                              => Console.WriteLine("Line {0}: {1}", eventArgs.JavaLineNumber, eventArgs.Message);
            var parsed = JavaToCSharpConverter.ConvertText(System.IO.File.ReadAllText(filePath), options);
            Assert.NotNull(parsed);
        }
    }
}
