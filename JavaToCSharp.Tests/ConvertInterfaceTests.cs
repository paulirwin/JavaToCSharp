namespace JavaToCSharp.Tests;

public class ConvertInterfaceTests
{
    [Fact]
    public void Verify_Method_Param_Types_Use_New_Interface_Name_Types()
    {
        const string javaCode = """
                                import com.github.javaparser.resolution.Context;
                                import com.github.javaparser.resolution.Test;
                                public interface ResolvedType {
                                    default boolean isArray() {
                                        return false;
                                    }
                                }

                                public class InferenceVariableType implements ResolvedType {
                                    public void registerEquivalentType(ResolvedType type) {
                                    }
                                }

                                public interface ResolvedValueDeclaration extends ResolvedDeclaration {
                                    ResolvedType getType();
                                }

                                """;
        var options = new JavaConversionOptions { StartInterfaceNamesWithI = true };
        options.WarningEncountered += (_, eventArgs)
            => Console.WriteLine("Line {0}: {1}", eventArgs.JavaLineNumber, eventArgs.Message);
        var parsed = JavaToCSharpConverter.ConvertText(javaCode, options) ?? "";

        const string expectedCSharpCode = """
                                          using Com.Github.Javaparser.Resolution;
                                          
                                          namespace MyApp
                                          {
                                              public interface IResolvedType
                                              {
                                                  bool IsArray()
                                                  {
                                                      return false;
                                                  }
                                              }
                                          
                                              public class InferenceVariableType : IResolvedType
                                              {
                                                  public virtual void RegisterEquivalentType(IResolvedType type)
                                                  {
                                                  }
                                              }
                                          
                                              public interface IResolvedValueDeclaration : ResolvedDeclaration
                                              {
                                                  IResolvedType GetType();
                                              }
                                          }
                                          """;

        Assert.Equal(expectedCSharpCode.ReplaceLineEndings(), parsed.ReplaceLineEndings());
    }

    [Fact]
    public void Verify_Interface_Extends_Are_Converted()
    {
        const string javaCode = """
                                public interface CharTermAttribute extends Attribute, CharSequence, Appendable {
                                }
                                """;
        var options = new JavaConversionOptions { StartInterfaceNamesWithI = true };
        options.WarningEncountered += (_, eventArgs)
            => Console.WriteLine("Line {0}: {1}", eventArgs.JavaLineNumber, eventArgs.Message);
        var parsed = JavaToCSharpConverter.ConvertText(javaCode, options) ?? "";

        const string expected = """
                                namespace MyApp
                                {
                                    public interface ICharTermAttribute : Attribute, CharSequence, Appendable
                                    {
                                    }
                                }
                                """;

        Assert.Equal(expected.ReplaceLineEndings(), parsed.ReplaceLineEndings());
    }
}
