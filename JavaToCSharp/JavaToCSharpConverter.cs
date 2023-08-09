using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using com.github.javaparser;
using com.github.javaparser.ast;
using com.github.javaparser.ast.body;
using ikvm.io;
using JavaToCSharp.Declarations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp
{
    public static class JavaToCSharpConverter
    {
        public static string? ConvertText(string? javaText, JavaConversionOptions? options = null)
        {
            options ??= new JavaConversionOptions();

            options.ConversionStateChanged(ConversionState.Starting);

            var context = new ConversionContext(options);

            var textBytes = Encoding.UTF8.GetBytes(javaText ?? string.Empty);

            using var memoryStream = new MemoryStream(textBytes);
            using var wrapper = new InputStreamWrapper(memoryStream);

            options.ConversionStateChanged(ConversionState.ParsingJavaAst);

            var parser = new JavaParser();
            
            var parsed = parser.parse(wrapper);

            if (!parsed.isSuccessful())
            {
                var problems = parsed.getProblems();
                var problemText = new StringBuilder();

                foreach (var problem in problems.OfType<Problem>())
                {
                    problemText.AppendLine(problem.getMessage());
                }

                throw new InvalidOperationException($"Parsing failed:{Environment.NewLine}{problemText}");
            }

            var result = parsed.getResult().FromRequiredOptional<CompilationUnit>();

            options.ConversionStateChanged(ConversionState.BuildingCSharpAst);

            var types = result.getTypes().ToList<TypeDeclaration>() ?? new List<TypeDeclaration>();
            var imports = result.getImports()?.ToList<ImportDeclaration>() ?? new List<ImportDeclaration>();
            var package = result.getPackageDeclaration().FromOptional<PackageDeclaration>();

            var rootMembers = new List<MemberDeclarationSyntax>();
            NamespaceDeclarationSyntax? namespaceSyntax = null;

            if (options.IncludeNamespace)
            {
                string packageName = package?.getName()?.toString() ?? "MyApp";

                foreach (var packageReplacement in options.PackageReplacements)
                {
                    if (string.IsNullOrWhiteSpace(packageName))
                    {
                        continue;
                    }
                    
                    packageName = packageReplacement.Replace(packageName)!;
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
                        var interfaceSyntax = ClassOrInterfaceDeclarationVisitor.VisitInterfaceDeclaration(context, classOrIntType);

                        if (namespaceSyntax != null && interfaceSyntax != null)
                            namespaceSyntax = namespaceSyntax.AddMembers(interfaceSyntax);
                        else if(interfaceSyntax != null)
                            rootMembers.Add(interfaceSyntax);
                    }
                    else
                    {
                        var classSyntax = ClassOrInterfaceDeclarationVisitor.VisitClassDeclaration(context, classOrIntType);

                        if (namespaceSyntax != null && classSyntax != null)
                            namespaceSyntax = namespaceSyntax.AddMembers(classSyntax);
                        else if(classSyntax != null)
                            rootMembers.Add(classSyntax);
                    }
                }
                else if (type is EnumDeclaration enumType)
                {
                    var classSyntax = EnumDeclarationVisitor.VisitEnumDeclaration(context, enumType);

                    if (namespaceSyntax != null && classSyntax != null)
                        namespaceSyntax = namespaceSyntax.AddMembers(classSyntax);
                    else if(classSyntax != null)
                        rootMembers.Add(classSyntax);
                }
            }

            if (namespaceSyntax != null)
                rootMembers.Add(namespaceSyntax);

            var root = SyntaxFactory.CompilationUnit(
                    externs: new SyntaxList<ExternAliasDirectiveSyntax>(),
                    usings: SyntaxFactory.List(UsingsHelper.GetUsings(imports, options)),
                    attributeLists: new SyntaxList<AttributeListSyntax>(),
                    members: SyntaxFactory.List(rootMembers)
                )
                .NormalizeWhitespace();

            root = root.WithJavaComments(result, "\r\n");
            if (root is null)
            {
                return null;
            }

            var postConversionSanitizer = new SanitizingSyntaxRewriter();
            var sanitizedRoot = postConversionSanitizer.VisitCompilationUnit(root);
            if (sanitizedRoot is null)
            {
                return null;
            }

            var tree = SyntaxFactory.SyntaxTree(sanitizedRoot);

            options.ConversionStateChanged(ConversionState.Done);

            return tree.GetText().ToString();
        }
    }
}
