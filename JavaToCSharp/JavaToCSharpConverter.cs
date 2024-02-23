using System.Text;
using com.github.javaparser;
using com.github.javaparser.ast;
using com.github.javaparser.ast.body;
using ikvm.io;
using JavaToCSharp.Declarations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp;

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
        parser.getParserConfiguration().setLanguageLevel(ParserConfiguration.LanguageLevel.JAVA_17);

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
        NameSyntax? namespaceNameSyntax = null;

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

            namespaceNameSyntax = SyntaxFactory.ParseName(packageName);
        }

        foreach (var type in types)
        {
            if (type is ClassOrInterfaceDeclaration classOrIntType)
            {
                if (classOrIntType.isInterface())
                {
                    var interfaceSyntax = ClassOrInterfaceDeclarationVisitor.VisitInterfaceDeclaration(context, classOrIntType);
                    rootMembers.Add(interfaceSyntax.NormalizeWhitespace().WithTrailingNewLines());
                }
                else
                {
                    var classSyntax = ClassOrInterfaceDeclarationVisitor.VisitClassDeclaration(context, classOrIntType);
                    rootMembers.Add(classSyntax.NormalizeWhitespace().WithTrailingNewLines());
                }
            }
            else if (type is EnumDeclaration enumType)
            {
                var enumSyntax = EnumDeclarationVisitor.VisitEnumDeclaration(context, enumType);
                rootMembers.Add(enumSyntax.NormalizeWhitespace().WithTrailingNewLines());
            }
        }

        if (rootMembers.Count > 1)
        {
            for (int i = 1; i < rootMembers.Count; i++)
            {
                rootMembers[i] = rootMembers[i].WithLeadingNewLines();
            }
        }

        if (namespaceNameSyntax != null)
        {
            if (options.UseFileScopedNamespaces && rootMembers.Count > 0)
            {
                rootMembers[0] = rootMembers[0].WithLeadingNewLines();
            }

            MemberDeclarationSyntax namespaceSyntax =
                options.UseFileScopedNamespaces
                ? SyntaxFactory.FileScopedNamespaceDeclaration(namespaceNameSyntax)
                    .NormalizeWhitespace()
                    .WithTrailingNewLines()
                    .WithMembers(SyntaxFactory.List(rootMembers))
                : SyntaxFactory.NamespaceDeclaration(namespaceNameSyntax)
                    .WithMembers(SyntaxFactory.List(rootMembers))
                    .NormalizeWhitespace();

            rootMembers = [namespaceSyntax];
        }

        var root = SyntaxFactory.CompilationUnit(
                externs: [],
                usings: SyntaxFactory.List(UsingsHelper.GetUsings(context, imports, options, namespaceNameSyntax)),
                attributeLists: [],
                members: SyntaxFactory.List(rootMembers)
            );

        root = root.WithPackageFileComments(context, result, package);

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
