using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp
{
    public class ConversionContext
    {
        public ConversionContext(JavaConversionOptions options)
        {
            PendingAnonymousTypes = new Queue<ClassDeclarationSyntax>();
            UsedAnonymousTypeNames = new HashSet<string>();
            Options = options;
        }

        public Queue<ClassDeclarationSyntax> PendingAnonymousTypes { get; }

        public ISet<string> UsedAnonymousTypeNames { get; }

        public JavaConversionOptions Options { get; }

        public string RootTypeName { get; set; }

        public string LastTypeName { get; set; }
    }
}
