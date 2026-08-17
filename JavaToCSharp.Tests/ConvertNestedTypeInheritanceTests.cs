namespace JavaToCSharp.Tests;

public class ConvertNestedTypeInheritanceTests
{
    [Fact]
    public void Extending_Nested_Type_Keeps_Declaring_Type_Qualifier()
    {
        const string javaCode = """
                                package com.example;
                                public class GeneratorFactory {
                                    public abstract static class AbstractXmlFeatureGeneratorFactory {
                                    }
                                    public interface XmlFeatureGeneratorFactory {
                                    }
                                    public class CachedFeatureGeneratorFactory
                                        extends GeneratorFactory.AbstractXmlFeatureGeneratorFactory
                                        implements GeneratorFactory.XmlFeatureGeneratorFactory {
                                    }
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains(
            "public class CachedFeatureGeneratorFactory : GeneratorFactory.AbstractXmlFeatureGeneratorFactory, GeneratorFactory.XmlFeatureGeneratorFactory",
            parsed);
    }

    [Fact]
    public void Extending_Simple_Type_Is_Unaffected()
    {
        const string javaCode = """
                                package com.example;
                                public class Square extends Shape {
                                }
                                """;

        var parsed = Convert(javaCode);

        Assert.Contains("public class Square : Shape", parsed);
    }

    private static string Convert(string javaCode)
        => JavaToCSharpConverter.ConvertText(javaCode, new JavaConversionOptions { IncludeComments = false }) ?? "";
}
