using com.github.javaparser;
using com.github.javaparser.ast;
using com.github.javaparser.ast.body;
using com.github.javaparser.ast.stmt;
using com.github.javaparser.ast.type;
using JavaToCSharp.Statements;
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

        // Java record components become C# record properties. Component names are kept verbatim
        // rather than capitalized: bodies within the record refer to components directly
        // (e.g. `x + y`), and those references are not rewritten, so renaming would break them.
        var components = recordDecl.getParameters().ToList<Parameter>() ?? [];
        var componentNames = components.Select(i => i.getNameAsString()).ToHashSet(StringComparer.Ordinal);

        // A compact constructor's body runs against the constructor parameters and may reassign
        // them, with the component fields assigned from those parameters afterwards. A positional
        // record cannot express that, so the record is emitted in non-positional form: explicit
        // properties plus a constructor holding the compact body and the trailing assignments.
        var compactConstructors = recordDecl.getCompactConstructors().ToList<CompactConstructorDeclaration>() ?? [];
        var compactCtor = compactConstructors.FirstOrDefault();

        // An explicit canonical constructor assigns the component fields itself, but it still
        // cannot coexist with a generated primary constructor, so it needs the same treatment.
        // Java forbids declaring both forms, so at most one of these is present.
        var canonicalCtor = recordDecl.getMembers()?.ToList<BodyDeclaration>()?
            .OfType<ConstructorDeclaration>()
            .FirstOrDefault(i => IsCanonicalConstructor(i, components));

        var isPositional = compactCtor is null && canonicalCtor is null;

        if (isPositional && components.Count > 0)
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

        if (!isPositional)
        {
            foreach (var component in components)
            {
                recordSyntax = recordSyntax.AddMembers(BuildComponentProperty(component));
            }

            if (compactCtor is not null)
            {
                recordSyntax = recordSyntax.AddMembers(BuildLoweredCompactConstructor(context, name, compactCtor, components));
            }
        }

        foreach (var member in members)
        {
            if (member is CompactConstructorDeclaration)
            {
                // Lowered into an explicit constructor above.
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

            // The component properties are emitted explicitly whenever the record is not
            // positional, so an explicit accessor would be a duplicate member (CS0102).
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

        // A record with no members needs a terminating semicolon rather than an empty body.
        recordSyntax = recordSyntax.Members.Count == 0
            ? recordSyntax.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            : recordSyntax.WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken))
                .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken));

        return recordSyntax.WithJavaComments(context, recordDecl);
    }

    /// <summary>
    /// Builds the `public T name { get; init; }` property that stands in for a record component
    /// when the record cannot be emitted in positional form.
    /// </summary>
    private static PropertyDeclarationSyntax BuildComponentProperty(Parameter component)
        => SyntaxFactory.PropertyDeclaration(
                SyntaxFactory.ParseTypeName(TypeHelper.ConvertTypeOf(component)),
                SyntaxFactory.ParseToken(TypeHelper.EscapeIdentifier(component.getNameAsString())))
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddAccessorListAccessors(
                SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                SyntaxFactory.AccessorDeclaration(SyntaxKind.InitAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));

    /// <summary>
    /// Lowers a Java compact constructor into an explicit C# constructor. The compact body runs
    /// first, against the parameters, and the component properties are assigned from those
    /// parameters afterwards, preserving any reassignment the body performed.
    /// </summary>
    private static ConstructorDeclarationSyntax BuildLoweredCompactConstructor(
        ConversionContext context,
        string name,
        CompactConstructorDeclaration compactCtor,
        IReadOnlyList<Parameter> components)
    {
        var ctorSyntax = SyntaxFactory.ConstructorDeclaration(name).WithLeadingNewLines();

        var mods = compactCtor.getModifiers().ToModifierKeywordSet();

        // A compact constructor is the canonical constructor, so it must be at least as accessible
        // as the record itself; Java requires public when the record is public.
        if (mods.Contains(Modifier.Keyword.PROTECTED))
            ctorSyntax = ctorSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));
        else if (mods.Contains(Modifier.Keyword.PRIVATE))
            ctorSyntax = ctorSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
        else
            ctorSyntax = ctorSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

        ctorSyntax = ctorSyntax.AddParameterListParameters(components.Select(i =>
                SyntaxFactory.Parameter(SyntaxFactory.ParseToken(TypeHelper.EscapeIdentifier(i.getNameAsString())))
                    .WithType(SyntaxFactory.ParseTypeName(TypeHelper.ConvertTypeOf(i))))
            .ToArray());

        var bodyStatements = StatementVisitor.VisitStatements(context,
            compactCtor.getBody().getStatements().ToList<Statement>());

        var assignments = components.Select(i =>
        {
            var identifier = TypeHelper.EscapeIdentifier(i.getNameAsString());

            return (StatementSyntax)SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.ThisExpression(),
                        SyntaxFactory.IdentifierName(identifier)),
                    SyntaxFactory.IdentifierName(identifier)));
        });

        return ctorSyntax.AddBodyStatements([.. bodyStatements, .. assignments]);
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
