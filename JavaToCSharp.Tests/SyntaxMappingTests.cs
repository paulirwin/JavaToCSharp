using YamlDotNet.Core;

namespace JavaToCSharp.Tests;

public class SyntaxMappingTests
{
    [Fact]
    public void Deserialize_Mappings()
    {
        var mappingString = """
            ImportMappings:
              org.junit.Test : XUnit
              java.util.List : ""
            """;

        var mappings = SyntaxMapping.Deserialize(mappingString);
        Assert.NotNull(mappings);
        Assert.Equal(2, mappings.ImportMappings.Count);
        Assert.Equal("XUnit", mappings.ImportMappings["org.junit.Test"]);
        Assert.Equal("", mappings.ImportMappings["java.util.List"]);
        Assert.False(mappings.ImportMappings.ContainsKey("other.Clazz"));
        Assert.Empty(mappings.VoidMethodMappings);
        Assert.Empty(mappings.NonVoidMethodMappings);
        Assert.Empty(mappings.AnnotationMappings);
    }

    [Fact]
    public void Conversion_Options_Defaults_To_Empty_Mappings()
    {
        var options = new JavaConversionOptions();
        Assert.Empty(options.SyntaxMappings.ImportMappings);
        Assert.Empty(options.SyntaxMappings.VoidMethodMappings);
        Assert.Empty(options.SyntaxMappings.NonVoidMethodMappings);
        Assert.Empty(options.SyntaxMappings.AnnotationMappings);
    }

    [Theory]
    [InlineData("VoidMethodMappings:\n  org.junit.Assert.assertTrue : Assert.True")]
    [InlineData("VoidMethodMappings:\n  assertTrue : \"\"")]
    [InlineData("NonVoidMethodMappings:\n  org.junit.Assert.assertTrue : Assert.True")]
    [InlineData("NonVoidMethodMappings:\n  assertTrue : \"\"")]
    public void Validation_Exceptions(string mappingString)
    {
        Assert.Throws<SemanticErrorException>(() => SyntaxMapping.Deserialize(mappingString));
    }

}
