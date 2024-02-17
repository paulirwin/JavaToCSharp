using JavaToCSharp;
using JavaToCSharpGui.Properties;

namespace JavaToCSharpGui;

public static class CurrentOptions
{
    static CurrentOptions()
    {
        Options.IncludeUsings = Settings.Default.UseUsingsPreference;
        Options.IncludeNamespace = Settings.Default.UseNamespacePreference;
        Options.IncludeComments = Settings.Default.IncludeComments;
        Options.UseDebugAssertForAsserts = Settings.Default.UseDebugAssertPreference;
        Options.UseUnrecognizedCodeToComment = Settings.Default.UseUnrecognizedCodeToComment;
        Options.ConvertSystemOutToConsole = Settings.Default.ConvertSystemOutToConsole;

        Options.SetUsings(Settings.Default.Usings.Split(';'));
    }

    public static JavaConversionOptions Options { get; } = new JavaConversionOptions();

    public static void Persist()
    {
        Settings.Default.UseUsingsPreference = Options.IncludeUsings;
        Settings.Default.UseNamespacePreference = Options.IncludeNamespace;
        Settings.Default.IncludeComments = Options.IncludeComments;
        Settings.Default.UseDebugAssertPreference = Options.UseDebugAssertForAsserts;
        Settings.Default.UseUnrecognizedCodeToComment = Options.UseUnrecognizedCodeToComment;
        Settings.Default.ConvertSystemOutToConsole = Options.ConvertSystemOutToConsole;
        Settings.Default.Usings = string.Join(";", Options.Usings);

        Settings.Default.Save();
    }
}
