using com.github.javaparser.ast.body;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Declarations
{
    public class EmptyMemberDeclarationVisitor : BodyDeclarationVisitor<EmptyMemberDeclaration>
    {
        public override MemberDeclarationSyntax VisitForClass(ConversionContext context, ClassDeclarationSyntax classSyntax,
            EmptyMemberDeclaration declaration)
        {
            return null;
        }

        public override MemberDeclarationSyntax VisitForInterface(ConversionContext context, InterfaceDeclarationSyntax interfaceSyntax,
            EmptyMemberDeclaration declaration)
        {
            return null;
        }
    }
}
