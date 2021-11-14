using System.Collections.Generic;

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

        public static EnumDeclarationSyntax VisitEnumDeclaration(ConversionContext context, EnumDeclaration javai)
        {
            var name = javai.getName();
            context.LastTypeName = name;

            var classSyntax = SyntaxFactory.EnumDeclaration(name);

            var typeConstants = javai.getEntries().ToList<EnumConstantDeclaration>();
            if (typeConstants is { Count: > 0 })
            {
                var useCodeToComment = context.Options.UseUnrecognizedCodeToComment;
                var membersCount = typeConstants.Count;
                var enumMembers = new List<EnumMemberDeclarationSyntax>(membersCount);
                var lastMembersIndex = membersCount - 1;
                var showNoPortedWarning = false;
                for (int i = 0; i < membersCount; i++)
                {
                    var itemConst = typeConstants[i];
                    var memberDecl = SyntaxFactory.EnumMemberDeclaration(itemConst.getName())
                                                  .WithJavaComments(itemConst);

                    if (useCodeToComment)
                    {
                        //java-enum `body/args` to `code Comment`
                        var constArgs = itemConst.getArgs();
                        var classBody = itemConst.getClassBody();
                        if (!constArgs.isEmpty() || !classBody.isEmpty())
                        {
                            var bodyCodes = CommentsHelper.ConvertToComment(new[] { itemConst }, "enum member body", false);
                            var firstLeadingTrivia = memberDecl.GetLeadingTrivia().Last();
                            memberDecl = memberDecl.InsertTriviaAfter(firstLeadingTrivia, bodyCodes);

                            showNoPortedWarning = true;
                        }

                        //java-enum `method-body` to `code Comment`
                        if (i == lastMembersIndex)
                            memberDecl = MembersToCommentTrivia(memberDecl, ref showNoPortedWarning);
                    }

                    enumMembers.Add(memberDecl);
                }

                if (showNoPortedWarning)
                    context.Options.Warning($"Members found in enum {name} will not be ported. Check for correctness.", javai.getBegin().line);

                classSyntax = classSyntax.AddMembers(enumMembers.ToArray());
            }

            var mods = javai.getModifiers();
            if (mods.HasFlag(Modifier.PRIVATE))
                classSyntax = classSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
            if (mods.HasFlag(Modifier.PROTECTED))
                classSyntax = classSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));
            if (mods.HasFlag(Modifier.PUBLIC))
                classSyntax = classSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            return classSyntax.WithJavaComments(javai);

            EnumMemberDeclarationSyntax MembersToCommentTrivia(EnumMemberDeclarationSyntax lastMemberDecl, ref bool showNoPortedWarning)
            {
                var members = javai.getMembers().ToList<BodyDeclaration>();
                if (members is { Count: > 0 })
                {
                    var todoCodes = CommentsHelper.ConvertToComment(members, "enum body members");
                    if (todoCodes != null)
                    {
                        var lastMemberTrailingTrivia = lastMemberDecl.GetTrailingTrivia();
                        if (lastMemberTrailingTrivia.Count > 0)
                            lastMemberDecl = lastMemberDecl.InsertTriviaAfter(lastMemberTrailingTrivia.Last(), todoCodes);
                        else
                            lastMemberDecl = lastMemberDecl.WithTrailingTrivia(todoCodes);

                        showNoPortedWarning = true;
                    }
                }

                return lastMemberDecl;
            }
        }
    }
}