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

    // The use of mappings is tested using a typical JUnit4 test converted to Xunit:
    // - Multiple java imports: rewritten and removed (empty value)
    // - Multiple java methods: rewritten (void) and not rewritten (non void)
    // - Multiple Java method annotations: rewritten and removed (no mapping)
    // Not tested:
    // - Qualified java method/annotation names (this would require a more elaborated handling of the scope)
    // - No qualified CSharp methods (this would require CSharp static imports, that are not implemented)
    // - Annotations with parameters
    [Fact]
    public void Conversion_With_Import_Method_And_Annotation_Mappings()
    {
        const string javaCode = """
                                import static org.junit.Assert.assertEquals;
                                import static org.junit.Assert.assertTrue;
                                import org.junit.Test;
                                public class MappingsTest {
                                	@Test @CustomJava @NotMapped
                                    public void testAsserts() {
                                		assertEquals("a", "a");
                                		assertTrue(true);
                                        va.assertTrue(true); // non void is not mapped
                                	}
                                }
                                
                                """;
        var mappingsYaml = """
                                ImportMappings:
                                  org.junit.Test : Xunit
                                  #to remove static imports
                                  org.junit.Assert.assertEquals : ""
                                  org.junit.Assert.assertTrue : ""
                                VoidMethodMappings:
                                  assertEquals : Assert.Equal
                                  assertTrue : Assert.True
                                AnnotationMappings:
                                  Test : Fact
                                  CustomJava : CustomCs
                                
                                """;
        const string expectedCSharpCode = """
                                          using Xunit;

                                          public class MappingsTest
                                          {
                                              [Fact]
                                              [CustomCs]
                                              public virtual void TestAsserts()
                                              {
                                                  Assert.Equal("a", "a");
                                                  Assert.True(true);
                                                  va.AssertTrue(true); // non void is not mapped
                                              }
                                          }
                                          
                                          """;

        var parsed = GetParsed(javaCode, mappingsYaml);
        Assert.Equal(expectedCSharpCode.ReplaceLineEndings(), parsed.ReplaceLineEndings());
    }

    private static string GetParsed(string javaCode, string mappingsYaml)
    {
        var mappings = SyntaxMapping.Deserialize(mappingsYaml);
        var options = new JavaConversionOptions { IncludeNamespace = false, IncludeUsings = false, SyntaxMappings = mappings };
        options.WarningEncountered += (_, eventArgs)
            => Console.WriteLine("Line {0}: {1}", eventArgs.JavaLineNumber, eventArgs.Message);
        var parsed = JavaToCSharpConverter.ConvertText(javaCode, options) ?? "";
        return parsed;
    }
}
