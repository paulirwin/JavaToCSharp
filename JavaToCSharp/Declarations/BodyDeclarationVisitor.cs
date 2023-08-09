using System;
using System.Collections.Generic;
using com.github.javaparser.ast.body;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Range = com.github.javaparser.Range;

namespace JavaToCSharp.Declarations;

public abstract class BodyDeclarationVisitor<T> : BodyDeclarationVisitor
    where T : BodyDeclaration
{
    public abstract MemberDeclarationSyntax? VisitForClass(ConversionContext context, ClassDeclarationSyntax classSyntax, T declaration);

    public abstract MemberDeclarationSyntax? VisitForInterface(ConversionContext context, InterfaceDeclarationSyntax interfaceSyntax, T declaration);

    protected sealed override MemberDeclarationSyntax? VisitForClass(ConversionContext context, ClassDeclarationSyntax classSyntax, BodyDeclaration declaration)
    {
        return VisitForClass(context, classSyntax, (T)declaration);
    }

    protected sealed override MemberDeclarationSyntax? VisitForInterface(ConversionContext context, InterfaceDeclarationSyntax interfaceSyntax, BodyDeclaration declaration)
    {
        return VisitForInterface(context, interfaceSyntax, (T)declaration);
    }
}

public abstract class BodyDeclarationVisitor
{
    private static readonly IDictionary<Type, BodyDeclarationVisitor> _visitors;

    static BodyDeclarationVisitor()
    {
        _visitors = new Dictionary<Type, BodyDeclarationVisitor>
        {
            { typeof(ConstructorDeclaration), new ConstructorDeclarationVisitor() },
            { typeof(EnumDeclaration), new EnumDeclarationVisitor() },
            { typeof(FieldDeclaration), new FieldDeclarationVisitor() },
            { typeof(MethodDeclaration), new MethodDeclarationVisitor() },
            { typeof(InitializerDeclaration), new InitializerDeclarationVisitor() },
            { typeof(ClassOrInterfaceDeclaration), new ClassOrInterfaceDeclarationVisitor() },
            { typeof(AnnotationDeclaration), new AnnotationDeclarationVisitor() },
        };
    }

    protected abstract MemberDeclarationSyntax? VisitForClass(ConversionContext context, ClassDeclarationSyntax classSyntax, BodyDeclaration declaration);

    protected abstract MemberDeclarationSyntax? VisitForInterface(ConversionContext context, InterfaceDeclarationSyntax interfaceSyntax, BodyDeclaration declaration);

    public static MemberDeclarationSyntax? VisitBodyDeclarationForClass(ConversionContext context, ClassDeclarationSyntax classSyntax, BodyDeclaration declaration)
    {
        if (!_visitors.TryGetValue(declaration.GetType(), out var visitor))
        {
            var message = $"No visitor has been implemented for body declaration `{declaration}`, {declaration.getRange().FromRequiredOptional<Range>().begin} type `{declaration.GetType()}`.";
            throw new InvalidOperationException(message);
        }

        return visitor.VisitForClass(context, classSyntax, declaration)
            .WithJavaComments(declaration);
    }

    public static MemberDeclarationSyntax? VisitBodyDeclarationForInterface(ConversionContext context, InterfaceDeclarationSyntax interfaceSyntax, BodyDeclaration declaration)
    {
        if (!_visitors.TryGetValue(declaration.GetType(), out var visitor))
        {
            var message = $"No visitor has been implemented for body declaration `{declaration}`, {declaration.getRange().FromRequiredOptional<Range>().begin} type `{declaration.GetType()}`.";
            throw new InvalidOperationException(message);
        }

        return visitor.VisitForInterface(context, interfaceSyntax, declaration)
            .WithJavaComments(declaration);
    }
}
