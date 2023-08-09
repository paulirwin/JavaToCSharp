using System.Collections.Generic;
using System.Linq;
using com.github.javaparser.ast;
using com.github.javaparser.ast.body;
using com.github.javaparser.ast.expr;
using com.github.javaparser.ast.stmt;
using JavaToCSharp.Statements;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Parameter = com.github.javaparser.ast.body.Parameter;

namespace JavaToCSharp.Declarations;

public class MethodDeclarationVisitor : BodyDeclarationVisitor<MethodDeclaration>
{
    public override MemberDeclarationSyntax VisitForClass(ConversionContext context, ClassDeclarationSyntax classSyntax, MethodDeclaration methodDecl)
    {
        var returnType = methodDecl.getType();
        var returnTypeName = TypeHelper.ConvertType(returnType.toString());

        var methodName = TypeHelper.Capitalize(methodDecl.getNameAsString());
        methodName = TypeHelper.ReplaceCommonMethodNames(methodName);

        string typeParameters = methodDecl.getTypeParameters().ToString() ?? "";
        if (typeParameters.Length > 2)
        {
            // Looks like "[T, U]". Convert to "<T, U>"
            methodName += typeParameters.Replace('[', '<').Replace(']', '>');
        }

        var methodSyntax = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName(returnTypeName), methodName);

        var mods = methodDecl.getModifiers().ToList<Modifier>() ?? new List<Modifier>();

        if (mods.Any(i => i.getKeyword() == Modifier.Keyword.PUBLIC))
            methodSyntax = methodSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
        if (mods.Any(i => i.getKeyword() == Modifier.Keyword.PROTECTED))
            methodSyntax = methodSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));
        if (mods.Any(i => i.getKeyword() == Modifier.Keyword.PRIVATE))
            methodSyntax = methodSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
        if (mods.Any(i => i.getKeyword() == Modifier.Keyword.STATIC))
            methodSyntax = methodSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
        if (mods.Any(i => i.getKeyword() == Modifier.Keyword.ABSTRACT))
            methodSyntax = methodSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.AbstractKeyword));

        var annotations = methodDecl.getAnnotations().ToList<AnnotationExpr>();
        bool isOverride = false;

        // TODO: figure out how to check for a non-interface base type
        if (annotations is {Count: > 0})
        {
            foreach (var annotation in annotations)
            {
                string name = annotation.getNameAsString();
                if (name == "Override")
                {
                    methodSyntax = methodSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.OverrideKeyword));
                    isOverride = true;
                }
            }
        }

        if (!mods.Any(i => i.getKeyword() == Modifier.Keyword.FINAL)
            && !mods.Any(i => i.getKeyword() == Modifier.Keyword.ABSTRACT)
            && !mods.Any(i => i.getKeyword() == Modifier.Keyword.STATIC)
            && !mods.Any(i => i.getKeyword() == Modifier.Keyword.PRIVATE)
            && !isOverride
            && !classSyntax.Modifiers.Any(i => i.IsKind(SyntaxKind.SealedKeyword)))
            methodSyntax = methodSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.VirtualKeyword));

        var parameters = methodDecl.getParameters().ToList<Parameter>();

        if (parameters is {Count: > 0})
        {
            var paramSyntaxes = new List<ParameterSyntax>();

            foreach (var param in parameters)
            {
                string typeName = TypeHelper.ConvertTypeOf(param);
                string identifier = TypeHelper.EscapeIdentifier(param.getNameAsString());

                if ((param.getType().getArrayLevel() > 0 && !typeName.EndsWith("[]")) || param.isVarArgs())
                    typeName += "[]";

                var modifiers = SyntaxFactory.TokenList();

                if (param.isVarArgs())
                    modifiers = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ParamsKeyword));

                var paramSyntax = SyntaxFactory.Parameter(
                    attributeLists: new SyntaxList<AttributeListSyntax>(),
                    modifiers: modifiers,
                    type: SyntaxFactory.ParseTypeName(typeName),
                    identifier: SyntaxFactory.ParseToken(identifier),
                    @default: null);

                paramSyntaxes.Add(paramSyntax);
            }

            methodSyntax = methodSyntax.AddParameterListParameters(paramSyntaxes.ToArray());
        }

        var block = methodDecl.getBody().FromOptional<BlockStmt>();

        if (block == null)
        {
            // i.e. abstract method
            methodSyntax = methodSyntax.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

            return methodSyntax;
        }

        var statements = block.getStatements().ToList<Statement>();

        var statementSyntax = StatementVisitor.VisitStatements(context, statements);

        if (mods.Any(i => i.getKeyword() == Modifier.Keyword.SYNCHRONIZED))
        {
            var lockBlock = SyntaxFactory.Block(statementSyntax);

            LockStatementSyntax? lockSyntax = null;
            if (mods.Any(i => i.getKeyword() == Modifier.Keyword.STATIC))
            {
                string? identifier = classSyntax.Identifier.Value?.ToString();
                if (identifier is not null)
                {
                    lockSyntax = SyntaxFactory.LockStatement(SyntaxFactory.TypeOfExpression(SyntaxFactory.ParseTypeName(identifier)), lockBlock);
                }
            }
            else
            {
                lockSyntax = SyntaxFactory.LockStatement(SyntaxFactory.ThisExpression(), lockBlock);
            }

            if (lockSyntax is not null)
            {
                methodSyntax = methodSyntax.AddBodyStatements(lockSyntax);
            }
        }
        else
        {
            methodSyntax = methodSyntax.AddBodyStatements(statementSyntax.ToArray());
        }

        return methodSyntax;
    }

    public override MemberDeclarationSyntax VisitForInterface(ConversionContext context, InterfaceDeclarationSyntax interfaceSyntax, MethodDeclaration methodDecl)
    {
        var returnType = methodDecl.getType();
        var returnTypeName = TypeHelper.ConvertType(returnType.toString());

        var methodName = TypeHelper.Capitalize(methodDecl.getNameAsString());
        methodName = TypeHelper.ReplaceCommonMethodNames(methodName);

        string typeParameters = methodDecl.getTypeParameters().ToString() ?? "";
        if (typeParameters.Length > 2)
        {
            // Looks like "[T, U]". Convert to "<T, U>"
            methodName += typeParameters.Replace('[', '<').Replace(']', '>');
        }

        var methodSyntax = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName(returnTypeName), methodName);

        var parameters = methodDecl.getParameters().ToList<Parameter>();

        if (parameters is {Count: > 0})
        {
            var paramSyntax = parameters.Select(i =>
                SyntaxFactory.Parameter(
                    attributeLists: new SyntaxList<AttributeListSyntax>(),
                    modifiers: SyntaxFactory.TokenList(),
                    type: SyntaxFactory.ParseTypeName(TypeHelper.ConvertTypeOf(i)),
                    identifier: SyntaxFactory.ParseToken(TypeHelper.EscapeIdentifier(i.getNameAsString())),
                    @default: null))
                .ToArray();

            methodSyntax = methodSyntax.AddParameterListParameters(paramSyntax.ToArray());
        }

        methodSyntax = methodSyntax.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

        return methodSyntax;
    }
}
