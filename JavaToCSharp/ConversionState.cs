namespace JavaToCSharp;

public enum ConversionState
{
    Starting = 0,
    ParsingJavaAst,
    BuildingCSharpAst,
    Done
}
