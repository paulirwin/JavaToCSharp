using System.Collections.Generic;
using System.Linq;
using com.github.javaparser.ast;
using com.github.javaparser.ast.body;
using com.github.javaparser.ast.expr;
using com.github.javaparser.ast.type;
using com.sun.org.apache.bcel.@internal.classfile;
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

        string typeName = fieldDecl.getCommonType().toString();

        var variableDeclarators = fieldDecl.getVariables()?.ToList<VariableDeclarator>() ?? new List<VariableDeclarator>();
        foreach (var item in variableDeclarators)
        {
            var type = item.getType();
            string name = item.getNameAsString();

            if (type.getArrayLevel() > 0)
            {
                if (!typeName.EndsWith("[]"))
                    typeName += "[]";
                if (name.EndsWith("[]"))
                    name = name[..^2];
            }

            var initExpr = item.getInitializer().FromOptional<Expression>();

            if (initExpr != null)
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

        typeName = TypeHelper.ConvertType(typeName);

        var fieldSyntax = SyntaxFactory.FieldDeclaration(
            SyntaxFactory.VariableDeclaration(
                SyntaxFactory.ParseTypeName(typeName),
                SyntaxFactory.SeparatedList(variables, Enumerable.Repeat(SyntaxFactory.Token(SyntaxKind.CommaToken), variables.Count - 1))));

        var mods = fieldDecl.getModifiers().ToList<Modifier>();

        if (mods.Any(i => i.getKeyword() == Modifier.Keyword.PUBLIC))
            fieldSyntax = fieldSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
        if (mods.Any(i => i.getKeyword() == Modifier.Keyword.PROTECTED))
            fieldSyntax = fieldSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));
        if (mods.Any(i => i.getKeyword() == Modifier.Keyword.PRIVATE))
            fieldSyntax = fieldSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
        if (mods.Any(i => i.getKeyword() == Modifier.Keyword.STATIC))
            fieldSyntax = fieldSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
        if (mods.Any(i => i.getKeyword() == Modifier.Keyword.FINAL))
            fieldSyntax = fieldSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));
        if (mods.Any(i => i.getKeyword() == Modifier.Keyword.VOLATILE))
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
