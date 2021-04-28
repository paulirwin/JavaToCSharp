using com.github.javaparser.ast;
using com.github.javaparser.ast.body;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace JavaToCSharp.Declarations
{
    public class EnumDeclarationVisitor : BodyDeclarationVisitor<EnumDeclaration>
    {
        public override MemberDeclarationSyntax VisitForClass(ConversionContext context, ClassDeclarationSyntax classSyntax, EnumDeclaration enumDecl)
        {
            var name = enumDecl.getName();

            var members = enumDecl.getMembers().ToList<BodyDeclaration>();

            var entries = enumDecl.getEntries().ToList<EnumConstantDeclaration>();

            if (members is {Count: > 0})
                context.Options.Warning("Members found in enum " + name + " will not be ported. Check for correctness.", enumDecl.getBegin().line);

            var enumSyntax = SyntaxFactory.EnumDeclaration(name)
                .AddMembers(entries.Select(entry => SyntaxFactory.EnumMemberDeclaration(entry.getName())).ToArray());

            var mods = enumDecl.getModifiers();

            if (mods.HasFlag(Modifier.PRIVATE))
                enumSyntax = enumSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
            if (mods.HasFlag(Modifier.PROTECTED))
                enumSyntax = enumSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));
            if (mods.HasFlag(Modifier.PUBLIC))
                enumSyntax = enumSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            return enumSyntax;
        }

        public override MemberDeclarationSyntax VisitForInterface(ConversionContext context, InterfaceDeclarationSyntax interfaceSyntax, EnumDeclaration declaration)
        {
            return VisitForClass(context, null, declaration);
        }
    }
}
