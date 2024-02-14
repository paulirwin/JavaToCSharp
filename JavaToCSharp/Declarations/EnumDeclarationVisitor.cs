using com.github.javaparser;
using com.github.javaparser.ast;
using com.github.javaparser.ast.body;
using com.github.javaparser.ast.type;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Declarations;

public class EnumDeclarationVisitor : BodyDeclarationVisitor<EnumDeclaration>
{
    public override MemberDeclarationSyntax VisitForClass(
        ConversionContext context,
        ClassDeclarationSyntax? classSyntax,
        EnumDeclaration enumDecl,
        IReadOnlyList<ClassOrInterfaceType> extends,
        IReadOnlyList<ClassOrInterfaceType> implements)
    {
        return VisitEnumDeclaration(context, enumDecl);
    }

    public override MemberDeclarationSyntax VisitForInterface(ConversionContext context,
        InterfaceDeclarationSyntax interfaceSyntax, EnumDeclaration declaration)
    {
        return VisitEnumDeclaration(context, declaration);
    }

    public static EnumDeclarationSyntax VisitEnumDeclaration(ConversionContext context, EnumDeclaration enumDecl)
    {
        var name = enumDecl.getNameAsString();
        context.LastTypeName = name;

        var enumSyntax = SyntaxFactory.EnumDeclaration(name);

        var typeConstants = enumDecl.getEntries().ToList<EnumConstantDeclaration>();

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
                var memberDecl = SyntaxFactory.EnumMemberDeclaration(itemConst.getNameAsString())
                    .WithJavaComments(context, itemConst);

                if (useCodeToComment)
                {
                    //java-enum `body/args` to `code Comment`
                    var constArgs = itemConst.getArguments();
                    var classBody = itemConst.getClassBody();
                    if (!constArgs.isEmpty() || !classBody.isEmpty())
                    {
                        var bodyCodes = CommentsHelper.ConvertToComment(new[] { itemConst }, "enum member body", false);

                        if (memberDecl.HasLeadingTrivia)
                        {
                            var firstLeadingTrivia = memberDecl.GetLeadingTrivia().Last();
                            memberDecl = memberDecl.InsertTriviaAfter(firstLeadingTrivia, bodyCodes);
                        }
                        else
                        {
                            memberDecl = memberDecl.WithLeadingTrivia(bodyCodes);
                        }

                        showNoPortedWarning = true;
                    }

                    //java-enum `method-body` to `code Comment`
                    if (i == lastMembersIndex)
                    {
                        memberDecl = MembersToCommentTrivia(memberDecl, ref showNoPortedWarning);
                    }
                }

                enumMembers.Add(memberDecl);
            }

            if (showNoPortedWarning)
                context.Options.Warning($"Members found in enum {name} will not be ported. Check for correctness.",
                    enumDecl.getBegin().FromRequiredOptional<Position>().line);

            enumSyntax = enumSyntax.AddMembers(enumMembers.ToArray());
        }

        var mods = enumDecl.getModifiers().ToModifierKeywordSet();

        if (mods.Contains(Modifier.Keyword.PRIVATE))
            enumSyntax = enumSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
        if (mods.Contains(Modifier.Keyword.PROTECTED))
            enumSyntax = enumSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));
        if (mods.Contains(Modifier.Keyword.PUBLIC))
            enumSyntax = enumSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

        context.Options.StaticUsingEnumNames.Add(name);

        return enumSyntax.WithJavaComments(context, enumDecl);

        EnumMemberDeclarationSyntax MembersToCommentTrivia(EnumMemberDeclarationSyntax lastMemberDecl,
            ref bool showNoPortedWarning)
        {
            var members = enumDecl.getMembers().ToList<BodyDeclaration>();
            if (members is { Count: > 0 })
            {
                var todoCodes = CommentsHelper.ConvertToComment(members, "enum body members");
                var lastMemberTrailingTrivia = lastMemberDecl.GetTrailingTrivia();
                lastMemberDecl = lastMemberTrailingTrivia.Count > 0
                    ? lastMemberDecl.InsertTriviaAfter(lastMemberTrailingTrivia.Last(), todoCodes)
                    : lastMemberDecl.WithTrailingTrivia(todoCodes);
                showNoPortedWarning = true;
            }

            return lastMemberDecl;
        }
    }
}
