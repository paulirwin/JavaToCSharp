using System;
using System.Collections.Generic;
using System.Linq;
using com.github.javaparser.ast;
using com.github.javaparser.ast.body;
using com.github.javaparser.ast.expr;
using com.github.javaparser.ast.stmt;
using JavaToCSharp.Expressions;
using JavaToCSharp.Statements;
using Roslyn.Compilers.CSharp;

namespace JavaToCSharp.Declarations
{
	public class ConstructorDeclarationVisitor : BodyDeclarationVisitor<ConstructorDeclaration>
    {
        public override MemberDeclarationSyntax VisitForClass(ConversionContext context, ClassDeclarationSyntax classSyntax, ConstructorDeclaration ctorDecl)
        {
            var ctorSyntax = Syntax.ConstructorDeclaration(classSyntax.Identifier.Value.ToString())
                .WithLeadingTrivia(Syntax.CarriageReturnLineFeed);

            var mods = ctorDecl.getModifiers();
			
			if (mods.HasFlag(Modifier.PUBLIC))
                ctorSyntax = ctorSyntax.AddModifiers(Syntax.Token(SyntaxKind.PublicKeyword));
            if (mods.HasFlag(Modifier.PROTECTED))
                ctorSyntax = ctorSyntax.AddModifiers(Syntax.Token(SyntaxKind.ProtectedKeyword));
            if (mods.HasFlag(Modifier.PRIVATE))
                ctorSyntax = ctorSyntax.AddModifiers(Syntax.Token(SyntaxKind.PrivateKeyword));

            var parameters = ctorDecl.getParameters().ToList<Parameter>();

            if (parameters != null && parameters.Count > 0)
            {
                var paramSyntaxes = new List<ParameterSyntax>();

                foreach (var param in parameters)
                {
                    var paramSyntax = Syntax.Parameter(Syntax.ParseToken(TypeHelper.ConvertIdentifierName(param.getId().toString())));

                    if (param.isVarArgs())
                    {
                        paramSyntax = paramSyntax.WithType(Syntax.ParseTypeName(TypeHelper.ConvertType(param.getType().toString()) + "[]"))
                            .WithModifiers(Syntax.TokenList(Syntax.Token(SyntaxKind.ParamsKeyword)));
                    }
                    else
                    {
                        paramSyntax = paramSyntax.WithType(Syntax.ParseTypeName(TypeHelper.ConvertType(param.getType().toString())));
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

                var initargs = ctorInvStmt.getArgs().ToList<Expression>();

                if (initargs != null && initargs.Count > 0)
                {
                    var initargslist = new List<ArgumentSyntax>();

                    foreach (var arg in initargs)
                    {
                        var argsyn = ExpressionVisitor.VisitExpression(context, arg);
                        initargslist.Add(Syntax.Argument(argsyn));
                    }

                    argsSyntax = Syntax.ArgumentList(Syntax.SeparatedList(initargslist, Enumerable.Repeat(Syntax.Token(SyntaxKind.CommaToken), initargslist.Count - 1)));
                }

                ConstructorInitializerSyntax ctorinitsyn;

                if (ctorInvStmt.isThis())
                    ctorinitsyn = Syntax.ConstructorInitializer(SyntaxKind.ThisConstructorInitializer, argsSyntax);
                else
                    ctorinitsyn = Syntax.ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, argsSyntax);

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
