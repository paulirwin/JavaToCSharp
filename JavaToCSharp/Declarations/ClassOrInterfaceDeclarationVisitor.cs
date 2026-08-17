using com.github.javaparser;
using com.github.javaparser.ast;
using com.github.javaparser.ast.body;
using com.github.javaparser.ast.expr;
using com.github.javaparser.ast.type;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Declarations;

public class ClassOrInterfaceDeclarationVisitor : BodyDeclarationVisitor<ClassOrInterfaceDeclaration>
{
    /// <summary>
    /// Builds the <c>// Java: sealed, permits ...</c> comment describing a sealed hierarchy that C# cannot
    /// fully express, or <see langword="null"/> when the declaration is neither sealed nor non-sealed.
    /// </summary>
    private static string? GetSealedComment(ClassOrInterfaceDeclaration decl, ISet<Modifier.Keyword> mods)
    {
        bool isSealed = mods.Contains(Modifier.Keyword.SEALED);
        bool isNonSealed = mods.Contains(Modifier.Keyword.NON_SEALED);

        if (!isSealed && !isNonSealed)
        {
            return null;
        }

        string keyword = isSealed ? "sealed" : "non-sealed";

        var permitted = decl.getPermittedTypes().ToList<ClassOrInterfaceType>() ?? [];

        return permitted.Count > 0
            ? $"// Java: {keyword}, permits {string.Join(", ", permitted.Select(i => i.getNameAsString()))}"
            : $"// Java: {keyword}";
    }

    private static SyntaxTriviaList BuildSealedTrivia(SyntaxTriviaList existing, string comment)
        => existing
            .Add(SyntaxFactory.Comment(comment))
            .Add(SyntaxFactory.ElasticCarriageReturnLineFeed);

    private static ClassDeclarationSyntax WithSealedComment(ClassDeclarationSyntax syntax, string? comment)
        => comment is null
            ? syntax
            : syntax.WithLeadingTrivia(BuildSealedTrivia(syntax.GetLeadingTrivia(), comment));

    private static InterfaceDeclarationSyntax WithSealedComment(InterfaceDeclarationSyntax syntax, string? comment)
        => comment is null
            ? syntax
            : syntax.WithLeadingTrivia(BuildSealedTrivia(syntax.GetLeadingTrivia(), comment));

    public override MemberDeclarationSyntax? VisitForClass(
        ConversionContext context,
        ClassDeclarationSyntax classSyntax,
        ClassOrInterfaceDeclaration declaration,
        IReadOnlyList<ClassOrInterfaceType> extends,
        IReadOnlyList<ClassOrInterfaceType> implements)
    {
        return VisitClassDeclaration(context, declaration);
    }

    public override MemberDeclarationSyntax? VisitForInterface(ConversionContext context,
        InterfaceDeclarationSyntax interfaceSyntax,
        ClassOrInterfaceDeclaration declaration)
    {
        return VisitClassDeclaration(context, declaration);
    }

    public static InterfaceDeclarationSyntax VisitInterfaceDeclaration(ConversionContext context,
        ClassOrInterfaceDeclaration interfaceDecl, bool isNested = false)
    {
        var originalTypeName = interfaceDecl.getName();
        var newTypeName = context.Options.StartInterfaceNamesWithI
            ? $"I{originalTypeName.getIdentifier()}"
            : originalTypeName.getIdentifier();

        if (context.Options.StartInterfaceNamesWithI)
        {
            TypeHelper.AddOrUpdateTypeNameConversions(originalTypeName.getIdentifier(), newTypeName);
        }

        if (!isNested)
        {
            context.RootTypeName = newTypeName;
        }

        context.LastTypeName = newTypeName;

        var classSyntax = SyntaxFactory.InterfaceDeclaration(newTypeName);

        var typeParams = interfaceDecl.getTypeParameters().ToList<TypeParameter>();

        if (typeParams is { Count: > 0 })
        {
            classSyntax = classSyntax.AddTypeParameterListParameters(typeParams
                    .Select(i => SyntaxFactory.TypeParameter(i.getNameAsString())).ToArray());
        }

        var mods = interfaceDecl.getModifiers().ToModifierKeywordSet();

        if (mods.Contains(Modifier.Keyword.PRIVATE))
            classSyntax = classSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
        if (mods.Contains(Modifier.Keyword.PROTECTED))
            classSyntax = classSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));
        if (mods.Contains(Modifier.Keyword.PUBLIC))
            classSyntax = classSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
        if (mods.Contains(Modifier.Keyword.FINAL))
            classSyntax = classSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.SealedKeyword));

        string? sealedComment = GetSealedComment(interfaceDecl, mods);

        if (mods.Contains(Modifier.Keyword.SEALED))
        {
            context.Options.Warning(
                $"Sealed interface {newTypeName} has no C# equivalent and was converted to a non-sealed interface. Check for correctness.",
                interfaceDecl.getBegin().FromRequiredOptional<Position>().line);
        }

        var extends = interfaceDecl.getExtendedTypes().ToList<ClassOrInterfaceType>();

        if (extends is not null)
        {
            foreach (var extend in extends)
            {
                classSyntax = classSyntax.AddBaseListTypes(SyntaxFactory.SimpleBaseType(TypeHelper.GetSyntaxFromType(extend)));
            }
        }

        var implements = interfaceDecl.getImplementedTypes().ToList<ClassOrInterfaceType>();

        if (implements is not null)
        {
            foreach (var implement in implements)
            {
                classSyntax = classSyntax.AddBaseListTypes(SyntaxFactory.SimpleBaseType(TypeHelper.GetSyntaxFromType(implement)));
            }
        }

        var members = interfaceDecl.getMembers()?.ToList<BodyDeclaration>();

        if (members is not null)
        {
            foreach (var member in members)
            {
                var syntax = VisitBodyDeclarationForInterface(context, classSyntax, member);
                var memberWithComments = syntax?.WithJavaComments(context, member);

                if (memberWithComments is not null)
                {
                    classSyntax = classSyntax.AddMembers(memberWithComments);
                }
            }
        }

        return WithSealedComment(classSyntax.WithJavaComments(context, interfaceDecl), sealedComment);
    }

    /// <summary>
    /// Prepends any collected instance initializer blocks to each constructor body, mirroring Java's
    /// rule that they run at the start of every constructor. Constructors that chain to another
    /// constructor of the same class via <c>this(...)</c> are skipped, since the initializers already
    /// ran in the constructor being chained to. When the class declares no constructor at all, a
    /// default one is synthesized to hold them.
    /// </summary>
    private static ClassDeclarationSyntax ApplyInstanceInitializers(ConversionContext context, ClassDeclarationSyntax classSyntax)
    {
        if (context.PendingInstanceInitializers.Count == 0)
        {
            return classSyntax;
        }

        var initializerStatements = context.PendingInstanceInitializers
            .SelectMany(block => block.Statements)
            .ToArray();

        var constructors = classSyntax.Members.OfType<ConstructorDeclarationSyntax>()
            .Where(ctor => !ctor.Modifiers.Any(SyntaxKind.StaticKeyword))
            .ToList();

        if (constructors.Count == 0)
        {
            return classSyntax.AddMembers(
                SyntaxFactory.ConstructorDeclaration(classSyntax.Identifier.ValueText)
                    .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                    .WithBody(SyntaxFactory.Block(initializerStatements)));
        }

        return classSyntax.ReplaceNodes(
            constructors,
            (original, _) =>
            {
                if (original.Initializer?.ThisOrBaseKeyword.IsKind(SyntaxKind.ThisKeyword) == true)
                {
                    return original;
                }

                var body = original.Body ?? SyntaxFactory.Block();

                return original.WithBody(body.WithStatements(body.Statements.InsertRange(0, initializerStatements)));
            });
    }

    public static ClassDeclarationSyntax VisitClassDeclaration(ConversionContext context,
        ClassOrInterfaceDeclaration classDecl, bool isNested = false)
    {
        string name = classDecl.getNameAsString();

        if (!isNested)
        {
            context.RootTypeName = name;
        }

        context.LastTypeName = name;

        var classSyntax = SyntaxFactory.ClassDeclaration(name);

        var typeParams = classDecl.getTypeParameters().ToList<TypeParameter>();

        if (typeParams is { Count: > 0 })
        {
            classSyntax = classSyntax.AddTypeParameterListParameters(typeParams
                    .Select(i => SyntaxFactory.TypeParameter(i.getNameAsString())).ToArray());
            classSyntax = classSyntax.AddConstraintClauses(TypeHelper.GetTypeParameterListConstraints(typeParams).ToArray());
        }

        var mods = classDecl.getModifiers().ToModifierKeywordSet();

        if (mods.Contains(Modifier.Keyword.PRIVATE))
            classSyntax = classSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
        if (mods.Contains(Modifier.Keyword.PROTECTED))
            classSyntax = classSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));
        if (mods.Contains(Modifier.Keyword.PUBLIC))
            classSyntax = classSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
        if (mods.Contains(Modifier.Keyword.ABSTRACT))
            classSyntax = classSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.AbstractKeyword));
        if (mods.Contains(Modifier.Keyword.FINAL))
            classSyntax = classSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.SealedKeyword));

        string? sealedComment = GetSealedComment(classDecl, mods);

        if (mods.Contains(Modifier.Keyword.SEALED))
        {
            if (context.Options.UseClosedForSealedClasses)
            {
                // The `closed` keyword is still marked experimental in Roslyn.
#pragma warning disable RSEXPERIMENTAL006
                classSyntax = classSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.ClosedKeyword));
#pragma warning restore RSEXPERIMENTAL006
            }
            else
            {
                context.Options.Warning(
                    $"Sealed class {name} was converted without the C# 15 `closed` modifier. Check for correctness.",
                    classDecl.getBegin().FromRequiredOptional<Position>().line);
            }
        }
        else if (mods.Contains(Modifier.Keyword.NON_SEALED))
        {
            context.Options.Warning(
                $"Non-sealed class {name} has no C# equivalent and was converted without a modifier. Check for correctness.",
                classDecl.getBegin().FromRequiredOptional<Position>().line);
        }

        var extends = classDecl.getExtendedTypes().ToList<ClassOrInterfaceType>() ?? [];

        foreach (var extend in extends)
        {
            classSyntax = classSyntax.AddBaseListTypes(SyntaxFactory.SimpleBaseType(TypeHelper.GetSyntaxFromType(extend)));
        }

        var implements = classDecl.getImplementedTypes().ToList<ClassOrInterfaceType>() ?? [];

        foreach (var implement in implements)
        {
            classSyntax = classSyntax.AddBaseListTypes(SyntaxFactory.SimpleBaseType(TypeHelper.GetSyntaxFromType(implement)));
        }

        var members = classDecl.getMembers()?.ToList<BodyDeclaration>();

        // Instance initializers are collected per-class. Nested types are visited inline below, so
        // the outer class's pending initializers are set aside to keep them from leaking into a
        // nested type's constructors (and vice versa).
        var outerInstanceInitializers = context.PendingInstanceInitializers.ToList();
        context.PendingInstanceInitializers.Clear();

        if (members is not null)
        {
            foreach (var member in members)
            {
                if (member is ClassOrInterfaceDeclaration childType)
                {
                    if (childType.isInterface())
                    {
                        var childInt = VisitInterfaceDeclaration(context, childType, true);

                        classSyntax = classSyntax.AddMembers(childInt);
                    }
                    else
                    {
                        var childClass = VisitClassDeclaration(context, childType, true);

                        classSyntax = classSyntax.AddMembers(childClass);
                    }
                }
                else
                {
                    var syntax = VisitBodyDeclarationForClass(context, classSyntax, member, extends, implements);
                    var withJavaComments = syntax?.WithJavaComments(context, member);

                    if (withJavaComments is not null)
                    {
                        classSyntax = classSyntax.AddMembers(withJavaComments);
                    }
                }

                while (context.PendingAnonymousTypes.Count > 0)
                {
                    var anon = context.PendingAnonymousTypes.Dequeue();
                    classSyntax = classSyntax.AddMembers(anon);
                }
            }
        }

        classSyntax = ApplyInstanceInitializers(context, classSyntax);

        context.PendingInstanceInitializers.Clear();
        context.PendingInstanceInitializers.AddRange(outerInstanceInitializers);

        var annotations = classDecl.getAnnotations().ToList<AnnotationExpr>();

        if (annotations is { Count: > 0 })
        {
            foreach (var annotation in annotations)
            {
                string annotationName = annotation.getNameAsString();
                const string annotationText = "Obsolete"; // TODO parse from java comment

                if (annotationName == "Deprecated")
                {
                    classSyntax = classSyntax.AddAttributeLists(SyntaxFactory.AttributeList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Obsolete"))
                                    .WithArgumentList(
                                        SyntaxFactory.AttributeArgumentList(
                                            SyntaxFactory.SingletonSeparatedList(
                                                SyntaxFactory.AttributeArgument(
                                                    SyntaxFactory.LiteralExpression(
                                                        SyntaxKind.StringLiteralExpression,
                                                        SyntaxFactory.Literal(annotationText)))))))));

                    break;
                }
            }
        }

        return WithSealedComment(classSyntax.WithJavaComments(context, classDecl), sealedComment);
    }
}
