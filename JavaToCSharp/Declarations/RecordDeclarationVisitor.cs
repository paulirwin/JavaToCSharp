using com.github.javaparser;
using com.github.javaparser.ast;
using com.github.javaparser.ast.body;
using com.github.javaparser.ast.type;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Declarations;

public class RecordDeclarationVisitor : BodyDeclarationVisitor<RecordDeclaration>
{
    public override MemberDeclarationSyntax VisitForClass(
        ConversionContext context,
        ClassDeclarationSyntax classSyntax,
        RecordDeclaration declaration,
        IReadOnlyList<ClassOrInterfaceType> extends,
        IReadOnlyList<ClassOrInterfaceType> implements)
    {
        return VisitRecordDeclaration(context, declaration, true);
    }

    public override MemberDeclarationSyntax VisitForInterface(ConversionContext context,
        InterfaceDeclarationSyntax interfaceSyntax, RecordDeclaration declaration)
    {
        return VisitRecordDeclaration(context, declaration, true);
    }

    public static RecordDeclarationSyntax VisitRecordDeclaration(ConversionContext context,
        RecordDeclaration recordDecl, bool isNested = false)
    {
        string name = recordDecl.getNameAsString();

        if (!isNested)
        {
            context.RootTypeName = name;
        }

        context.LastTypeName = name;

        var recordSyntax = SyntaxFactory.RecordDeclaration(
            SyntaxFactory.Token(SyntaxKind.RecordKeyword),
            name);

        var typeParams = recordDecl.getTypeParameters().ToList<TypeParameter>();

        if (typeParams is { Count: > 0 })
        {
            recordSyntax = recordSyntax.AddTypeParameterListParameters(typeParams
                .Select(i => SyntaxFactory.TypeParameter(i.getNameAsString())).ToArray());
            recordSyntax = recordSyntax.AddConstraintClauses(TypeHelper.GetTypeParameterListConstraints(typeParams).ToArray());
        }

        var mods = recordDecl.getModifiers().ToModifierKeywordSet();

        if (mods.Contains(Modifier.Keyword.PRIVATE))
            recordSyntax = recordSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
        if (mods.Contains(Modifier.Keyword.PROTECTED))
            recordSyntax = recordSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));
        if (mods.Contains(Modifier.Keyword.PUBLIC))
            recordSyntax = recordSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

        // Java record components become C# positional record parameters. Component names are kept
        // verbatim rather than capitalized: bodies within the record refer to components directly
        // (e.g. `x + y`), and those references are not rewritten, so renaming would break them.
        var components = recordDecl.getParameters().ToList<Parameter>() ?? [];
        var componentNames = components.Select(i => i.getNameAsString()).ToHashSet(StringComparer.Ordinal);

        if (components.Count > 0)
        {
            var paramSyntaxes = components.Select(i =>
                SyntaxFactory.Parameter(SyntaxFactory.ParseToken(TypeHelper.EscapeIdentifier(i.getNameAsString())))
                    .WithType(SyntaxFactory.ParseTypeName(TypeHelper.ConvertTypeOf(i))))
                .ToArray();

            recordSyntax = recordSyntax.AddParameterListParameters(paramSyntaxes);
        }

        // Java records cannot extend, so only implemented types contribute to the base list.
        var implements = recordDecl.getImplementedTypes().ToList<ClassOrInterfaceType>() ?? [];

        foreach (var implement in implements)
        {
            recordSyntax = recordSyntax.AddBaseListTypes(SyntaxFactory.SimpleBaseType(TypeHelper.GetSyntaxFromType(implement)));
        }

        // Members are converted through the shared class pipeline, which only reads the type's
        // identifier and modifiers, so a stand-in class declaration carries enough context.
        var memberHostSyntax = SyntaxFactory.ClassDeclaration(name).WithModifiers(recordSyntax.Modifiers);

        var members = recordDecl.getMembers()?.ToList<BodyDeclaration>() ?? [];

        var compactConstructors = recordDecl.getCompactConstructors().ToList<CompactConstructorDeclaration>() ?? [];

        foreach (var compactCtor in compactConstructors)
        {
            context.Options.Warning(
                $"Compact constructor in record {name} was not ported; its validation and normalization logic must be applied manually.",
                compactCtor.getBegin().FromRequiredOptional<Position>().line);
        }

        foreach (var member in members)
        {
            if (member is CompactConstructorDeclaration)
            {
                // Warned about above; there is no C# equivalent of a compact constructor.
                continue;
            }

            if (member is RecordDeclaration childRecord)
            {
                recordSyntax = recordSyntax.AddMembers(VisitRecordDeclaration(context, childRecord, true));
                continue;
            }

            if (member is ClassOrInterfaceDeclaration childType)
            {
                recordSyntax = recordSyntax.AddMembers(childType.isInterface()
                    ? ClassOrInterfaceDeclarationVisitor.VisitInterfaceDeclaration(context, childType, true)
                    : ClassOrInterfaceDeclarationVisitor.VisitClassDeclaration(context, childType, true));
                continue;
            }

            // An explicit canonical constructor collides with the positional record's primary
            // constructor (CS0111), and an explicit accessor collides with its property (CS0102).
            if (member is ConstructorDeclaration ctorDecl && IsCanonicalConstructor(ctorDecl, components))
            {
                context.Options.Warning(
                    $"Canonical constructor in record {name} was not ported because it conflicts with the generated primary constructor.",
                    ctorDecl.getBegin().FromRequiredOptional<Position>().line);
                continue;
            }

            if (member is MethodDeclaration methodDecl && IsExplicitAccessor(methodDecl, componentNames))
            {
                context.Options.Warning(
                    $"Accessor `{methodDecl.getNameAsString()}()` in record {name} was not ported because it conflicts with the generated property.",
                    methodDecl.getBegin().FromRequiredOptional<Position>().line);
                continue;
            }

            var syntax = VisitBodyDeclarationForClass(context, memberHostSyntax, member, [], implements);
            var memberWithComments = syntax?.WithJavaComments(context, member);

            if (memberWithComments is not null)
            {
                recordSyntax = recordSyntax.AddMembers(memberWithComments);
            }

            while (context.PendingAnonymousTypes.Count > 0)
            {
                var anon = context.PendingAnonymousTypes.Dequeue();
                recordSyntax = recordSyntax.AddMembers(anon);
            }
        }

        // A positional record with no body needs a terminating semicolon rather than braces.
        recordSyntax = recordSyntax.Members.Count == 0
            ? recordSyntax.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            : recordSyntax.WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken))
                .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken));

        return recordSyntax.WithJavaComments(context, recordDecl);
    }

    /// <summary>
    /// Determines whether a constructor is the record's canonical constructor, i.e. one whose
    /// parameter types match the record components in order. Such a constructor has no C#
    /// equivalent on a positional record, which already generates it.
    /// </summary>
    private static bool IsCanonicalConstructor(ConstructorDeclaration ctorDecl, IReadOnlyList<Parameter> components)
    {
        var parameters = ctorDecl.getParameters().ToList<Parameter>() ?? [];

        if (parameters.Count != components.Count)
        {
            return false;
        }

        return !parameters.Where((t, i) =>
            !string.Equals(t.getType().toString(), components[i].getType().toString(), StringComparison.Ordinal)).Any();
    }

    /// <summary>
    /// Determines whether a method is an explicit override of a record component accessor, i.e. a
    /// no-argument method named after one of the components.
    /// </summary>
    private static bool IsExplicitAccessor(MethodDeclaration methodDecl, ISet<string> componentNames)
        => methodDecl.getParameters().size() == 0 && componentNames.Contains(methodDecl.getNameAsString());
}
