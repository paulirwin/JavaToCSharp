using System;
using System.Collections.Generic;
using System.Linq;
using com.github.javaparser.ast;
using com.github.javaparser.ast.body;
using com.github.javaparser.ast.expr;
using com.github.javaparser.ast.stmt;
using JavaToCSharp.Expressions;
using JavaToCSharp.Statements;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Declarations
{
    public class ConstructorDeclarationVisitor : BodyDeclarationVisitor<ConstructorDeclaration>
    {
        public override MemberDeclarationSyntax VisitForClass(ConversionContext context, ClassDeclarationSyntax classSyntax, ConstructorDeclaration ctorDecl)
        {
            var ctorSyntax = SyntaxFactory.ConstructorDeclaration(classSyntax.Identifier.Value.ToString())
                .WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed);

            var mods = ctorDecl.getModifiers();

            if (mods.HasFlag(Modifier.PUBLIC))
                ctorSyntax = ctorSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            if (mods.HasFlag(Modifier.PROTECTED))
                ctorSyntax = ctorSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));
            if (mods.HasFlag(Modifier.PRIVATE))
                ctorSyntax = ctorSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));

            var parameters = ctorDecl.getParameters().ToList<Parameter>();

            if (parameters != null && parameters.Count > 0)
            {
                var paramSyntaxes = new List<ParameterSyntax>();

                foreach (var param in parameters)
                {
                    var name = param.getId().toString();
                    var paramSyntax = SyntaxFactory.Parameter(SyntaxFactory.ParseToken(TypeHelper.EscapeIdentifier(name)));

                    if (param.isVarArgs())
                    {
                        paramSyntax = paramSyntax.WithType(SyntaxFactory.ParseTypeName(TypeHelper.ConvertTypeOf(param) + "[]"))
                            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ParamsKeyword)));
                    }
                    else
                    {
                        paramSyntax = paramSyntax.WithType(SyntaxFactory.ParseTypeName(TypeHelper.ConvertTypeOf(param)));
                    }

                    paramSyntaxes.Add(paramSyntax);
                }

                ctorSyntax = ctorSyntax.AddParameterListParameters(paramSyntaxes.ToArray());
            }

            //chaws: var block = ctorDecl.getBlock();
            var block = ctorDecl.getBody();
            var statements = block.getStmts().ToList<Statement>();

            // handle special case for constructor invocation
            if (statements != null && statements.Count > 0 && statements[0] is ExplicitConstructorInvocationStmt)
            {
                var ctorInvStmt = (ExplicitConstructorInvocationStmt)statements[0];
                statements.RemoveAt(0);

                ArgumentListSyntax argsSyntax = null;

                var initargs = ctorInvStmt.getArgs();
                if (initargs != null && initargs.size() > 0)
                {
                    argsSyntax = TypeHelper.GetSyntaxFromArguments(context, initargs);
                }

                ConstructorInitializerSyntax ctorinitsyn;

                if (ctorInvStmt.isThis())
                    ctorinitsyn = SyntaxFactory.ConstructorInitializer(SyntaxKind.ThisConstructorInitializer, argsSyntax);
                else
                    ctorinitsyn = SyntaxFactory.ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, argsSyntax);

                ctorSyntax = ctorSyntax.WithInitializer(ctorinitsyn);
            }

            var statementSyntax = StatementVisitor.VisitStatements(context, statements);

            ctorSyntax = ctorSyntax.AddBodyStatements(statementSyntax.ToArray());

            return ctorSyntax;
        }

        public override MemberDeclarationSyntax VisitForInterface(ConversionContext context, InterfaceDeclarationSyntax interfaceSyntax, ConstructorDeclaration declaration)
        {
            throw new InvalidOperationException("Constructors are not valid on interfaces.");
        }
    }
}
