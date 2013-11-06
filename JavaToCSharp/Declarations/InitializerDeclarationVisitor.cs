using japa.parser.ast.body;
using JavaToCSharp.Statements;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            return Syntax.ConstructorDeclaration(classSyntax.Identifier.ValueText)
                .WithModifiers(Syntax.TokenList(Syntax.Token(SyntaxKind.StaticKeyword)))
                .WithBody(blockSyntax);
        }

        public override MemberDeclarationSyntax VisitForInterface(ConversionContext context, InterfaceDeclarationSyntax interfaceSyntax, InitializerDeclaration declaration)
        {
            throw new InvalidOperationException("Initializers are not valid on interfaces.");
        }
    }
}
