using com.github.javaparser;
using com.github.javaparser.ast.body;
using com.github.javaparser.ast.type;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Declarations;

public class AnnotationDeclarationVisitor : BodyDeclarationVisitor<AnnotationDeclaration>
{
    public override MemberDeclarationSyntax? VisitForClass(
        ConversionContext context, 
        ClassDeclarationSyntax classSyntax,
        AnnotationDeclaration declaration,
        IReadOnlyList<ClassOrInterfaceType> extends,
        IReadOnlyList<ClassOrInterfaceType> implements)
    {
        context.Options.Warning("Declaring an annotation inside a class NotImplemented.",
                                declaration.getBegin().FromRequiredOptional<Position>().line);
        return null;
    }

    public override MemberDeclarationSyntax VisitForInterface(ConversionContext context, InterfaceDeclarationSyntax interfaceSyntax,
        AnnotationDeclaration declaration)
    {
        throw new NotImplementedException();
    }
}