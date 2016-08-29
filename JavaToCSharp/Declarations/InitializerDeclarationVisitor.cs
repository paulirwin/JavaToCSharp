using System;
using com.github.javaparser.ast.body;
using JavaToCSharp.Statements;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Declarations
{
    public class InitializerDeclarationVisitor : BodyDeclarationVisitor<InitializerDeclaration>
    {
        public override MemberDeclarationSyntax VisitForClass(ConversionContext context, ClassDeclarationSyntax classSyntax, InitializerDeclaration declaration)
        {
            if (!declaration.isStatic())
                throw new NotImplementedException("Support for non-static initializers is not understood or implemented");

            var block = declaration.getBlock();
            var blockSyntax = (BlockSyntax)new BlockStatementVisitor().Visit(context, block);

            return SyntaxFactory.ConstructorDeclaration(classSyntax.Identifier.ValueText)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                .WithBody(blockSyntax);
        }

        public override MemberDeclarationSyntax VisitForInterface(ConversionContext context, InterfaceDeclarationSyntax interfaceSyntax, InitializerDeclaration declaration)
        {
            throw new InvalidOperationException("Initializers are not valid on interfaces.");
        }
    }
}
