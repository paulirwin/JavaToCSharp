using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp;

public class ConversionContext(JavaConversionOptions options)
{
    public Queue<ClassDeclarationSyntax> PendingAnonymousTypes { get; } = new();

    public ISet<string> UsedAnonymousTypeNames { get; } = new HashSet<string>();

    public ISet<string> StaticUsingEnumNames { get; } = new HashSet<string>();

    public JavaConversionOptions Options { get; } = options;

    public ConversionState ConversionState { get; private set; }

    public string? RootTypeName { get; set; }

    public string? LastTypeName { get; set; }

    /// <summary>
    /// Records the new conversion state and raises <see cref="JavaConversionOptions.StateChanged"/>.
    /// </summary>
    internal void ConversionStateChanged(ConversionState newState)
    {
        ConversionState = newState;

        Options.ConversionStateChanged(newState);
    }
}
