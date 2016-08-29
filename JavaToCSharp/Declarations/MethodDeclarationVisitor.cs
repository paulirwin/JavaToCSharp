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

namespace JavaToCSharp.Declarations
{
    public class MethodDeclarationVisitor : BodyDeclarationVisitor<MethodDeclaration>
    {
        public override MemberDeclarationSyntax VisitForClass(ConversionContext context, ClassDeclarationSyntax classSyntax, MethodDeclaration methodDecl)
        {
            var returnType = methodDecl.getType();
            var returnTypeName = TypeHelper.ConvertType(returnType.toString());

            var methodName = TypeHelper.Capitalize(methodDecl.getName());
            methodName = TypeHelper.ReplaceCommonMethodNames(methodName);

            var methodSyntax = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName(returnTypeName), methodName);

            var mods = methodDecl.getModifiers();

            if (mods.HasFlag(Modifier.PUBLIC))
                methodSyntax = methodSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            if (mods.HasFlag(Modifier.PROTECTED))
                methodSyntax = methodSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));
            if (mods.HasFlag(Modifier.PRIVATE))
                methodSyntax = methodSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
            if (mods.HasFlag(Modifier.STATIC))
                methodSyntax = methodSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
            if (mods.HasFlag(Modifier.ABSTRACT))
                methodSyntax = methodSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.AbstractKeyword));

            var annotations = methodDecl.getAnnotations().ToList<AnnotationExpr>();
            bool isOverride = false;

            // TODO: figure out how to check for a non-interface base type
            if (annotations != null
                && annotations.Count > 0)
            {
                foreach (var annotation in annotations)
                {
                    string name = annotation.getName().getName();
                    if (name == "Override")
                    {
                        methodSyntax = methodSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.OverrideKeyword));
                        isOverride = true;
                    }
                }
            }

            if (!mods.HasFlag(Modifier.FINAL)
                && !mods.HasFlag(Modifier.ABSTRACT)
                && !mods.HasFlag(Modifier.STATIC)
                && !mods.HasFlag(Modifier.PRIVATE)
                && !isOverride
                && !classSyntax.Modifiers.Any(i => i.Kind() == SyntaxKind.SealedKeyword))
                methodSyntax = methodSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.VirtualKeyword));

            var parameters = methodDecl.getParameters().ToList<Parameter>();

            if (parameters != null && parameters.Count > 0)
            {
                var paramSyntaxes = new List<ParameterSyntax>();

                foreach (var param in parameters)
                {
                    string typeName = TypeHelper.ConvertType(param.getType().toString());
                    string identifier = TypeHelper.ConvertIdentifierName(param.getId().getName());

                    if ((param.getId().getArrayCount() > 0 && !typeName.EndsWith("[]")) || param.isVarArgs())
                        typeName += "[]";

                    SyntaxTokenList modifiers = SyntaxFactory.TokenList();

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

            var block = methodDecl.getBody();

            if (block == null)
            {
                // i.e. abstract method
                methodSyntax = methodSyntax.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

                return methodSyntax;
            }

            var statements = block.getStmts().ToList<Statement>();

            var statementSyntax = StatementVisitor.VisitStatements(context, statements);

            if (mods.HasFlag(Modifier.SYNCHRONIZED))
            {
                LockStatementSyntax lockSyntax;
                BlockSyntax lockBlock = SyntaxFactory.Block(statementSyntax);

                if (mods.HasFlag(Modifier.STATIC))
                {
                    lockSyntax = SyntaxFactory.LockStatement(SyntaxFactory.TypeOfExpression(SyntaxFactory.ParseTypeName(classSyntax.Identifier.Value.ToString())), lockBlock);
                }
                else
                {
                    lockSyntax = SyntaxFactory.LockStatement(SyntaxFactory.ThisExpression(), lockBlock);
                }

                methodSyntax = methodSyntax.AddBodyStatements(lockSyntax);
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

            var methodName = TypeHelper.Capitalize(methodDecl.getName());
            methodName = TypeHelper.ReplaceCommonMethodNames(methodName);

            var methodSyntax = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName(returnTypeName), methodName);

            var parameters = methodDecl.getParameters().ToList<Parameter>();

            if (parameters != null && parameters.Count > 0)
            {
                var paramSyntax = parameters.Select(i =>
                    SyntaxFactory.Parameter(
                        attributeLists: new SyntaxList<AttributeListSyntax>(),
                        modifiers: SyntaxFactory.TokenList(),
                        type: SyntaxFactory.ParseTypeName(TypeHelper.ConvertType(i.getType().toString())),
                        identifier: SyntaxFactory.ParseToken(TypeHelper.ConvertIdentifierName(i.getId().toString())),
                        @default: null))
                    .ToArray();

                methodSyntax = methodSyntax.AddParameterListParameters(paramSyntax.ToArray());
            }

            methodSyntax = methodSyntax.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

            return methodSyntax;
        }
    }
}
