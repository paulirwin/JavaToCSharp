using System;
using System.Collections.Generic;
using System.Linq;
using com.github.javaparser.ast;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp;

public static class UsingsHelper
{
    public static IList<UsingDirectiveSyntax> GetUsings(List<ImportDeclaration> imports, JavaConversionOptions? options)
    {
        var usings = new List<UsingDirectiveSyntax>();

        foreach (var import in imports)
        {
            // The import directive in Java will import a specific class. 
            string importName = import.getName().toString();
            var lastPartStartIndex = importName.LastIndexOf(".", StringComparison.Ordinal);
            var importNameWithoutClassName = lastPartStartIndex == -1 ? 
                                                 importName : 
                                                 importName.Substring(0, lastPartStartIndex);
            var nameSpace = TypeHelper.Capitalize(importNameWithoutClassName);
            var usingSyntax = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(nameSpace));
            usings.Add(usingSyntax);
        }

        if (options?.IncludeUsings == true)
        {
            foreach (string ns in options.Usings.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                var usingSyntax = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(ns));
                usings.Add(usingSyntax);
            }
        }

        return usings.Distinct(new UsingDirectiveSyntaxComparer()).ToList();
    }
}

public class UsingDirectiveSyntaxComparer :  IEqualityComparer<UsingDirectiveSyntax>
{
    public bool Equals(UsingDirectiveSyntax? x, UsingDirectiveSyntax? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null)) return false;
        if (ReferenceEquals(y, null)) return false;
        if (x.GetType() != y.GetType()) return false;
        
        return Equals(x.Alias?.ToString(), y.Alias?.ToString()) && 
               x.Name.ToString().Equals(y.Name.ToString());
    }

    public int GetHashCode(UsingDirectiveSyntax obj)
    {
        return HashCode.Combine(obj.Alias?.ToString() ?? "", obj.Name.ToString());
    }
}

