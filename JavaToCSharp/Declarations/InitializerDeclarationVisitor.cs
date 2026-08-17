using com.github.javaparser.ast.body;
using com.github.javaparser.ast.type;
using JavaToCSharp.Statements;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Declarations;

public class InitializerDeclarationVisitor : BodyDeclarationVisitor<InitializerDeclaration>
{
    public override MemberDeclarationSyntax? VisitForClass(
        ConversionContext context, 
        ClassDeclarationSyntax classSyntax,
        InitializerDeclaration declaration,
        IReadOnlyList<ClassOrInterfaceType> extends,
        IReadOnlyList<ClassOrInterfaceType> implements)
    {
        var block = declaration.getBody();

        var blockSyntax = (BlockSyntax)new BlockStatementVisitor().Visit(context, block);

        // Java runs an instance initializer block at the start of every constructor, so it cannot be
        // emitted as a member on its own. Stash it for the class visitor to prepend to each
        // constructor body; emitting it as a static constructor (as this once did) would both run at
        // the wrong time and collide with any real static initializer.
        if (!declaration.isStatic())
        {
            context.PendingInstanceInitializers.Add(blockSyntax);
            return null;
        }

        return SyntaxFactory.ConstructorDeclaration(classSyntax.Identifier.ValueText)
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
            .WithBody(blockSyntax);
    }

    public override MemberDeclarationSyntax VisitForInterface(ConversionContext context, InterfaceDeclarationSyntax interfaceSyntax,
        InitializerDeclaration declaration)
    {
        throw new InvalidOperationException("Initializers are not valid on interfaces.");
    }
}
