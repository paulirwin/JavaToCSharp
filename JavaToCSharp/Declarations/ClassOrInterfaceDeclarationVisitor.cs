﻿using System.Collections.Generic;
using System.Linq;
using com.github.javaparser.ast;
using com.github.javaparser.ast.body;
using com.github.javaparser.ast.expr;
using com.github.javaparser.ast.type;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Declarations;

public class ClassOrInterfaceDeclarationVisitor : BodyDeclarationVisitor<ClassOrInterfaceDeclaration>
{
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

        var extends = interfaceDecl.getExtendedTypes().ToList<ClassOrInterfaceType>();

        if (extends != null)
        {
            foreach (var extend in extends)
            {
                classSyntax = classSyntax.AddBaseListTypes(SyntaxFactory.SimpleBaseType(TypeHelper.GetSyntaxFromType(extend)));
            }
        }

        var implements = interfaceDecl.getImplementedTypes().ToList<ClassOrInterfaceType>();

        if (implements != null)
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

                if (memberWithComments != null)
                {
                    classSyntax = classSyntax.AddMembers(memberWithComments);
                }
            }
        }

        return classSyntax.WithJavaComments(context, interfaceDecl);
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

        var extends = classDecl.getExtendedTypes().ToList<ClassOrInterfaceType>() ?? new List<ClassOrInterfaceType>();

        foreach (var extend in extends)
        {
            classSyntax = classSyntax.AddBaseListTypes(SyntaxFactory.SimpleBaseType(TypeHelper.GetSyntaxFromType(extend)));
        }

        var implements = classDecl.getImplementedTypes().ToList<ClassOrInterfaceType>() ?? new List<ClassOrInterfaceType>();

        foreach (var implement in implements)
        {
            classSyntax = classSyntax.AddBaseListTypes(SyntaxFactory.SimpleBaseType(TypeHelper.GetSyntaxFromType(implement)));
        }

        var members = classDecl.getMembers()?.ToList<BodyDeclaration>();

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

                    if (withJavaComments != null)
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

        return classSyntax.WithJavaComments(context, classDecl);
    }
}
