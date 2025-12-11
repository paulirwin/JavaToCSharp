using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp;

public class ConversionContext(JavaConversionOptions options)
{
    public Queue<ClassDeclarationSyntax> PendingAnonymousTypes { get; } = new();

    public ISet<string> UsedAnonymousTypeNames { get; } = new HashSet<string>();

    public JavaConversionOptions Options { get; } = options;

    public string? RootTypeName { get; set; }

    public string? LastTypeName { get; set; }
}
