using com.github.javaparser.ast;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp;

public static class UsingsHelper
{
    public static IList<UsingDirectiveSyntax> GetUsings(ConversionContext context, List<ImportDeclaration> imports, JavaConversionOptions? options)
    {
        var usings = new List<UsingDirectiveSyntax>();

        foreach (var import in imports)
        {
            // The import directive in Java will import a specific class.
            string importName = import.getNameAsString();
            var lastPartStartIndex = importName.LastIndexOf(".", StringComparison.Ordinal);
            var importNameWithoutClassName = lastPartStartIndex == -1 ?
                                                 importName :
                                                 importName[..lastPartStartIndex];
            var nameSpace = TypeHelper.Capitalize(importNameWithoutClassName);
            var usingSyntax = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(nameSpace));

            if (context.Options.IncludeComments)
            {
                usingSyntax = CommentsHelper.AddUsingComments(usingSyntax, import);
            }

            usings.Add(usingSyntax);
        }

        if (options?.IncludeUsings == true)
        {
            usings.AddRange(options.Usings
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(ns => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(ns))));
        }

        return usings.Distinct(new UsingDirectiveSyntaxComparer()).ToList();
    }
}

public class UsingDirectiveSyntaxComparer : IEqualityComparer<UsingDirectiveSyntax>
{
    public bool Equals(UsingDirectiveSyntax? x, UsingDirectiveSyntax? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;

        return Equals(x.Alias?.ToString(), y.Alias?.ToString()) &&
               Equals(x.Name?.ToString(), y.Name?.ToString());
    }

    public int GetHashCode(UsingDirectiveSyntax obj)
    {
        return HashCode.Combine(obj.Alias?.ToString() ?? "", obj.Name?.ToString() ?? "");
    }
}
