using com.github.javaparser.ast;
using com.github.javaparser.ast.body;
using com.github.javaparser.ast.expr;
using com.github.javaparser.ast.type;
using JavaToCSharp.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Declarations;

public class FieldDeclarationVisitor : BodyDeclarationVisitor<FieldDeclaration>
{
    public override MemberDeclarationSyntax VisitForClass(
        ConversionContext context,
        ClassDeclarationSyntax? classSyntax,
        FieldDeclaration fieldDecl,
        IReadOnlyList<ClassOrInterfaceType> extends,
        IReadOnlyList<ClassOrInterfaceType> implements)
    {
        var variables = new List<VariableDeclaratorSyntax>();

        var commonType = fieldDecl.getCommonType();
        int? arrayRank = null;

        var variableDeclarators = fieldDecl.getVariables()?.ToList<VariableDeclarator>() ?? [];

        foreach (var item in variableDeclarators)
        {
            var type = item.getType();

            if (arrayRank is not null && type.getArrayLevel() != arrayRank)
            {
                throw new InvalidOperationException("Different array levels in the same field declaration are not yet supported");
            }

            arrayRank ??= type.getArrayLevel();

            string name = item.getNameAsString();

            if (type.getArrayLevel() > 0)
            {
                while (name.EndsWith("[]"))
                {
                    name = name[..^2];
                }
            }

            var initExpr = item.getInitializer().FromOptional<Expression>();

            if (initExpr is not null)
            {
                var initSyntax = ExpressionVisitor.VisitExpression(context, initExpr);

                if (initSyntax is not null)
                {
                    var varDeclarationSyntax = SyntaxFactory.VariableDeclarator(name).WithInitializer(SyntaxFactory.EqualsValueClause(initSyntax));
                    variables.Add(varDeclarationSyntax);
                }
            }
            else
                variables.Add(SyntaxFactory.VariableDeclarator(name));
        }

        var typeSyntax = TypeHelper.ConvertTypeSyntax(commonType, arrayRank ?? 0);

        var fieldSyntax = SyntaxFactory.FieldDeclaration(
            SyntaxFactory.VariableDeclaration(
                typeSyntax,
                SyntaxFactory.SeparatedList(variables, Enumerable.Repeat(SyntaxFactory.Token(SyntaxKind.CommaToken), variables.Count - 1))));

        var mods = fieldDecl.getModifiers().ToModifierKeywordSet();

        if (mods.Contains(Modifier.Keyword.PUBLIC))
            fieldSyntax = fieldSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
        if (mods.Contains(Modifier.Keyword.PROTECTED))
            fieldSyntax = fieldSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));
        if (mods.Contains(Modifier.Keyword.PRIVATE))
            fieldSyntax = fieldSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
        if (mods.Contains(Modifier.Keyword.STATIC))
            fieldSyntax = fieldSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
        if (mods.Contains(Modifier.Keyword.FINAL))
            fieldSyntax = fieldSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));
        if (mods.Contains(Modifier.Keyword.VOLATILE))
            fieldSyntax = fieldSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.VolatileKeyword));

        return fieldSyntax;
    }

    public override MemberDeclarationSyntax VisitForInterface(ConversionContext context,
        InterfaceDeclarationSyntax interfaceSyntax, FieldDeclaration declaration)
    {
        // TODO: throw new NotImplementedException("Need to implement diversion of static fields from interface declaration to static class");
        return VisitForClass(context, null, declaration, new List<ClassOrInterfaceType>(), new List<ClassOrInterfaceType>());
    }
}
