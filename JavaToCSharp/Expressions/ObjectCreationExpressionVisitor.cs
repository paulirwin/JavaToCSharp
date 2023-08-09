using System;
using System.Collections.Generic;
using com.github.javaparser.ast.body;
using com.github.javaparser.ast.expr;
using JavaToCSharp.Declarations;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using com.github.javaparser.ast;

namespace JavaToCSharp.Expressions;

public class ObjectCreationExpressionVisitor : ExpressionVisitor<ObjectCreationExpr>
{
    public override ExpressionSyntax Visit(ConversionContext context, ObjectCreationExpr newExpr)
    {
        var anonBody = newExpr.getAnonymousClassBody().FromOptional<NodeList>().ToList<BodyDeclaration>();

        if (anonBody is {Count: > 0})
        {
            return VisitAnonymousClassCreationExpression(context, newExpr, anonBody);
        }

        // TODO: what to do with scope?
        //var scope = newExpr.getScope();
        //ExpressionSyntax scopeSyntax = null;

        //if (scope != null) {
        //    scopeSyntax = ExpressionVisitor.VisitExpression(context, scope);
        //}

        var type = newExpr.getType();

        var typeSyntax = TypeHelper.GetSyntaxFromType(type);

        var args = newExpr.getArguments();
        if (args == null || args.size() == 0)
            return SyntaxFactory.ObjectCreationExpression(typeSyntax).WithArgumentList(SyntaxFactory.ArgumentList());

        return SyntaxFactory.ObjectCreationExpression(typeSyntax, TypeHelper.GetSyntaxFromArguments(context, args), null);
    }

    private static ExpressionSyntax VisitAnonymousClassCreationExpression(ConversionContext context, ObjectCreationExpr newExpr, List<BodyDeclaration> anonBody)
    {
        string baseTypeName = TypeHelper.ConvertType(newExpr.getType().getNameAsString());
        string anonTypeName = string.Empty;

        for (int i = 0; i <= 100; i++)
        {
            if (i == 100)
                throw new InvalidOperationException("Too many anonymous types");

            anonTypeName = $"Anonymous{baseTypeName}{(i == 0 ? String.Empty : i.ToString())}";

            if (!context.UsedAnonymousTypeNames.Contains(anonTypeName))
            {
                context.UsedAnonymousTypeNames.Add(anonTypeName);
                break; // go with this one
            }
        }

        var classSyntax = SyntaxFactory.ClassDeclaration(anonTypeName)
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.SealedKeyword))
            .WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SeparatedList(new List<BaseTypeSyntax>
            {
                SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(baseTypeName))
            })));

        string? contextLastTypeName = context.LastTypeName;
        if (contextLastTypeName is not null)
        {
            var parentField = SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(contextLastTypeName))
                                                                          .AddVariables(SyntaxFactory.VariableDeclarator("parent")))
                                           .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));

            var ctorSyntax = SyntaxFactory.ConstructorDeclaration(anonTypeName)
                                          .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                                          .AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.ParseToken("parent")).WithType(SyntaxFactory.ParseTypeName(contextLastTypeName)))
                                          .AddBodyStatements(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ThisExpression(), SyntaxFactory.IdentifierName("parent")), SyntaxFactory.IdentifierName("parent"))));

            classSyntax = classSyntax.AddMembers(ctorSyntax, parentField);
        }

        foreach (var memberSyntax in anonBody.Select(member => BodyDeclarationVisitor.VisitBodyDeclarationForClass(context, classSyntax, member)).Where(memberSyntax => memberSyntax != null))
        {
            classSyntax = classSyntax.AddMembers(memberSyntax!);
        }

        context.PendingAnonymousTypes.Enqueue(classSyntax);

        var args = newExpr.getArguments();
        if (args == null || args.size() == 0)
            return SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(anonTypeName))
                .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.ThisExpression()));

        return SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(anonTypeName),
            TypeHelper.GetSyntaxFromArguments(context, args),
            null);
    }
}
