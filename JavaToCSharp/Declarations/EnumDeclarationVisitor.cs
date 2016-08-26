using com.github.javaparser.ast.body;
using java.lang.reflect;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavaToCSharp.Declarations
{
    public class EnumDeclarationVisitor : BodyDeclarationVisitor<EnumDeclaration>
    {
        public override MemberDeclarationSyntax VisitForClass(ConversionContext context, ClassDeclarationSyntax classSyntax, EnumDeclaration enumDecl)
        {
            var name = enumDecl.getName();

            var members = enumDecl.getMembers().ToList<BodyDeclaration>();

            var entries = enumDecl.getEntries().ToList<EnumConstantDeclaration>();
            var memberSyntaxes = new List<EnumMemberDeclarationSyntax>();

            foreach (var entry in entries)
            {
                // TODO: support "equals" value
                memberSyntaxes.Add(Syntax.EnumMemberDeclaration(entry.getName()));
            }

            if (members != null && members.Count > 0)
                context.Options.Warning("Members found in enum " + name + " will not be ported. Check for correctness.", enumDecl.getBeginLine());

            var enumSyntax = Syntax.EnumDeclaration(name)
                .AddMembers(memberSyntaxes.ToArray());

            var mods = enumDecl.getModifiers();

            if (mods.HasFlag(Modifier.PRIVATE))
                enumSyntax = enumSyntax.AddModifiers(Syntax.Token(SyntaxKind.PrivateKeyword));
            if (mods.HasFlag(Modifier.PROTECTED))
                enumSyntax = enumSyntax.AddModifiers(Syntax.Token(SyntaxKind.ProtectedKeyword));
            if (mods.HasFlag(Modifier.PUBLIC))
                enumSyntax = enumSyntax.AddModifiers(Syntax.Token(SyntaxKind.PublicKeyword));

            return enumSyntax;
        }

        public override MemberDeclarationSyntax VisitForInterface(ConversionContext context, InterfaceDeclarationSyntax interfaceSyntax, EnumDeclaration declaration)
        {
            throw new NotImplementedException("Need to implement diversion of nested enums in interfaces to non-nested.");
        }
    }
}
