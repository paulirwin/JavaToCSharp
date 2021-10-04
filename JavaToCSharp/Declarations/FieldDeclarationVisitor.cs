using System.Collections.Generic;
using System.Linq;

using com.github.javaparser.ast;
using com.github.javaparser.ast.body;
using com.github.javaparser.ast.expr;

using JavaToCSharp.Expressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

                var initExpr = item.getInit();

                if (initExpr != null)
                {
                    var initSyntax = ExpressionVisitor.VisitExpression(context, initExpr);
                    var varDeclarationSyntax = SyntaxFactory.VariableDeclarator(name).WithInitializer(SyntaxFactory.EqualsValueClause(initSyntax));
                    variables.Add(varDeclarationSyntax);
                }
                else
                    variables.Add(SyntaxFactory.VariableDeclarator(name));
            }

            typeName = TypeHelper.ConvertType(typeName);

            var fieldSyntax = SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.ParseTypeName(typeName),
                    SyntaxFactory.SeparatedList(variables, Enumerable.Repeat(SyntaxFactory.Token(SyntaxKind.CommaToken), variables.Count - 1))));

            var mods = fieldDecl.getModifiers();

            if (mods.HasFlag(Modifier.PUBLIC))
                fieldSyntax = fieldSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            if (mods.HasFlag(Modifier.PROTECTED))
                fieldSyntax = fieldSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));
            if (mods.HasFlag(Modifier.PRIVATE))
                fieldSyntax = fieldSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
            if (mods.HasFlag(Modifier.STATIC))
                fieldSyntax = fieldSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
            if (mods.HasFlag(Modifier.FINAL))
                fieldSyntax = fieldSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));
            if (mods.HasFlag(Modifier.VOLATILE))
                fieldSyntax = fieldSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.VolatileKeyword));

            if (context.Options.UseAnnotationsToComment)
                return fieldSyntax.AppendAnnotationsTrivias(fieldDecl);

            return fieldSyntax;
        }

        public override MemberDeclarationSyntax VisitForInterface(ConversionContext context,
            InterfaceDeclarationSyntax interfaceSyntax, FieldDeclaration declaration)
        {
            // TODO: throw new NotImplementedException("Need to implement diversion of static fields from interface declaration to static class");
            return VisitForClass(context, null, declaration);
        }
    }
}
