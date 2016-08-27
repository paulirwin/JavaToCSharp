namespace JavaToCSharp
{
	public enum ConversionState
    {
        Starting = 0,
        ParsingJavaAST,
        BuildingCSharpAST,
        Done
    }
}
