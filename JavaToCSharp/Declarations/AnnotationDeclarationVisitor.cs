using System;
using com.github.javaparser.ast.body;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Declarations
{
    public class AnnotationDeclarationVisitor : BodyDeclarationVisitor<AnnotationDeclaration>
    {
        public override MemberDeclarationSyntax VisitForClass(ConversionContext context, ClassDeclarationSyntax classSyntax,
            AnnotationDeclaration declaration)
        {
            Console.WriteLine("Declaring an annotation inside a class NotImplemented.");
            return null;
        }

        public override MemberDeclarationSyntax VisitForInterface(ConversionContext context, InterfaceDeclarationSyntax interfaceSyntax,
            AnnotationDeclaration declaration)
        {
            throw new NotImplementedException();
        }
    }
}