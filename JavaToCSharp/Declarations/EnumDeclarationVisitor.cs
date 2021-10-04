using System.Linq;

using com.github.javaparser.ast;
using com.github.javaparser.ast.body;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Declarations
{
    public class EnumDeclarationVisitor : BodyDeclarationVisitor<EnumDeclaration>
    {
        public override MemberDeclarationSyntax VisitForClass(ConversionContext context, ClassDeclarationSyntax classSyntax, EnumDeclaration enumDecl)
        {
            return VisitEnumDeclaration(context, enumDecl);
        }

        public override MemberDeclarationSyntax VisitForInterface(ConversionContext context, InterfaceDeclarationSyntax interfaceSyntax, EnumDeclaration declaration)
        {
            return VisitForClass(context, null, declaration);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="context"></param>
        /// <param name="javai"></param>
        /// <returns></returns>
        public static EnumDeclarationSyntax VisitEnumDeclaration(ConversionContext context, EnumDeclaration javai)
        {
            var name = javai.getName();
            context.LastTypeName = name;

            var classSyntax = SyntaxFactory.EnumDeclaration(name);

            var typeConstants = javai.getEntries().ToList<EnumConstantDeclaration>();
            if (typeConstants is { Count: > 0 })
            {
                classSyntax = classSyntax.AddMembers(typeConstants.Select(i => SyntaxFactory.EnumMemberDeclaration(i.getName())
                                                                                                .WithJavaComments(i)).ToArray());
            }

            var mods = javai.getModifiers();
            if (mods.HasFlag(Modifier.PRIVATE))
                classSyntax = classSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
            if (mods.HasFlag(Modifier.PROTECTED))
                classSyntax = classSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));
            if (mods.HasFlag(Modifier.PUBLIC))
                classSyntax = classSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            var members = javai.getMembers().ToList<BodyDeclaration>();
            if (members is { Count: > 0 })
            {
                var todoCodes = CommentsHelper.ConvertToComment(members, "enum members");
                if (todoCodes != null)
                {
                    var lastMember = classSyntax.Members.Last();
                    var lastMemberTrailingTrivia = lastMember.GetTrailingTrivia();
                    var lastMemberLeadingTrivia = lastMember.GetLeadingTrivia();

                    if (lastMemberTrailingTrivia.Count > 0)
                        classSyntax = classSyntax.InsertTriviaAfter(lastMemberTrailingTrivia.Last(), todoCodes);
                    else if (lastMemberLeadingTrivia.Count > 0)
                        classSyntax = classSyntax.InsertTriviaBefore(lastMemberLeadingTrivia.First(), todoCodes);
                }
                context.Options.Warning($"Members found in enum {name} will not be ported. Check for correctness.", javai.getBegin().line);
            }

            return classSyntax.WithJavaComments(javai);
        }
    }
}