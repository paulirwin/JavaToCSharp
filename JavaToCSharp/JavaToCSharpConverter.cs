using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using com.github.javaparser;
using com.github.javaparser.ast;
using com.github.javaparser.ast.body;
using JavaToCSharp.Declarations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp
{
    public static class JavaToCSharpConverter
    {
        public static string ConvertText(string javaText, JavaConversionOptions options = null)
        {
            options ??= new JavaConversionOptions();

            options.ConversionStateChanged(ConversionState.Starting);

            var context = new ConversionContext(options);

            var textBytes = Encoding.UTF8.GetBytes(javaText ?? string.Empty);

            using var memoryStream = new MemoryStream(textBytes);
            using var wrapper = new ikvm.io.InputStreamWrapper(memoryStream);

            options.ConversionStateChanged(ConversionState.ParsingJavaAst);

            var parsed = JavaParser.parse(wrapper);

            options.ConversionStateChanged(ConversionState.BuildingCSharpAst);

            var types = parsed.getTypes().ToList<TypeDeclaration>();
            var imports = parsed.getImports().ToList<ImportDeclaration>();
            var package = parsed.getPackage();

            var usings = new List<UsingDirectiveSyntax>();

            //foreach (var import in imports)
            //{
            //    var usingSyntax = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(import.getName().toString()));
            //    usings.Add(usingSyntax);
            //}

            if (options.IncludeUsings)
            {
                foreach (string ns in options.Usings.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    var usingSyntax = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(ns));
                    usings.Add(usingSyntax);
                }
            }

            var rootMembers = new List<MemberDeclarationSyntax>();
            NamespaceDeclarationSyntax namespaceSyntax = null;

            if (options.IncludeNamespace)
            {
                string packageName = package?.getName()?.toString() ?? "MyApp";

                foreach (var packageReplacement in options.PackageReplacements)
                {
                    packageName = packageReplacement.Replace(packageName);
                }

                packageName = TypeHelper.Capitalize(packageName);

                namespaceSyntax = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(packageName))
                    .WithJavaComments(package);
            }

            foreach (var type in types)
            {
                if (type is ClassOrInterfaceDeclaration classOrIntType)
                {
                    if (classOrIntType.isInterface())
                    {
                        var interfaceSyntax = ClassOrInterfaceDeclarationVisitor.VisitInterfaceDeclaration(context, classOrIntType, false);

                        if (namespaceSyntax != null)
                            namespaceSyntax = namespaceSyntax.AddMembers(interfaceSyntax);
                        else
                            rootMembers.Add(interfaceSyntax);
                    }
                    else
                    {
                        var classSyntax = ClassOrInterfaceDeclarationVisitor.VisitClassDeclaration(context, classOrIntType, false);

                        if (namespaceSyntax != null)
                            namespaceSyntax = namespaceSyntax.AddMembers(classSyntax);
                        else
                            rootMembers.Add(classSyntax);
                    }
                }
            }

            if (namespaceSyntax != null)
                rootMembers.Add(namespaceSyntax);

            var root = SyntaxFactory.CompilationUnit(
                    externs: new SyntaxList<ExternAliasDirectiveSyntax>(),
                    usings: SyntaxFactory.List(usings.ToArray()),
                    attributeLists: new SyntaxList<AttributeListSyntax>(),
                    members: SyntaxFactory.List<MemberDeclarationSyntax>(rootMembers)
                )
                .NormalizeWhitespace();

            root = root.WithJavaComments(parsed, "\r\n");

            var postConversionSanitizer = new SanitizingSyntaxRewriter();
            var sanitizedRoot = postConversionSanitizer.VisitCompilationUnit(root);

            var tree = SyntaxFactory.SyntaxTree(sanitizedRoot);

            options.ConversionStateChanged(ConversionState.Done);

            return tree.GetText().ToString();
        }
    }
}
