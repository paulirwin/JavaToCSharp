using System;
using System.Collections.Generic;
using System.Linq;
using com.github.javaparser.ast;
using com.github.javaparser.ast.body;
using JavaToCSharp.Expressions;
using Roslyn.Compilers.CSharp;

namespace JavaToCSharp.Declarations
{
	public class FieldDeclarationVisitor : BodyDeclarationVisitor<FieldDeclaration>
    {
        public override MemberDeclarationSyntax VisitForClass(ConversionContext context, ClassDeclarationSyntax classSyntax, FieldDeclaration fieldDecl)
        {
            var variables = new List<VariableDeclaratorSyntax>();

            string typeName = fieldDecl.getType().toString();

            foreach (var item in fieldDecl.getVariables().ToList<VariableDeclarator>())
            {
                var id = item.getId();
                string name = id.getName();

                if (id.getArrayCount() > 0)
                {
                    if (!typeName.EndsWith("[]"))
                        typeName += "[]";
                    if (name.EndsWith("[]"))
                        name = name.Substring(0, name.Length - 2);
                }

                var initexpr = item.getInit();

                if (initexpr != null)
                {
                    var initsyn = ExpressionVisitor.VisitExpression(context, initexpr);
                    var vardeclsyn = Syntax.VariableDeclarator(name).WithInitializer(Syntax.EqualsValueClause(initsyn));
                    variables.Add(vardeclsyn);
                }
                else
                    variables.Add(Syntax.VariableDeclarator(name));
            }

            typeName = TypeHelper.ConvertType(typeName);

            var fieldSyntax = Syntax.FieldDeclaration(
                Syntax.VariableDeclaration(
                    Syntax.ParseTypeName(typeName),
                    Syntax.SeparatedList(variables, Enumerable.Repeat(Syntax.Token(SyntaxKind.CommaToken), variables.Count - 1))));

            var mods = fieldDecl.getModifiers();

            if (mods.HasFlag(Modifier.PUBLIC))
                fieldSyntax = fieldSyntax.AddModifiers(Syntax.Token(SyntaxKind.PublicKeyword));
            if (mods.HasFlag(Modifier.PROTECTED))
                fieldSyntax = fieldSyntax.AddModifiers(Syntax.Token(SyntaxKind.ProtectedKeyword));
            if (mods.HasFlag(Modifier.PRIVATE))
                fieldSyntax = fieldSyntax.AddModifiers(Syntax.Token(SyntaxKind.PrivateKeyword));
            if (mods.HasFlag(Modifier.STATIC))
                fieldSyntax = fieldSyntax.AddModifiers(Syntax.Token(SyntaxKind.StaticKeyword));
            if (mods.HasFlag(Modifier.FINAL))
                fieldSyntax = fieldSyntax.AddModifiers(Syntax.Token(SyntaxKind.ReadOnlyKeyword));
            if (mods.HasFlag(Modifier.VOLATILE))
                fieldSyntax = fieldSyntax.AddModifiers(Syntax.Token(SyntaxKind.VolatileKeyword));

            return fieldSyntax;
        }

        public override MemberDeclarationSyntax VisitForInterface(ConversionContext context, InterfaceDeclarationSyntax interfaceSyntax, FieldDeclaration declaration)
        {
            throw new NotImplementedException("Need to implement diversion of static fields from interface declaration to static class");
        }
    }
}
