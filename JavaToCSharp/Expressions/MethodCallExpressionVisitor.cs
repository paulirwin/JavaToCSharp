using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions;

public class MethodCallExpressionVisitor : ExpressionVisitor<MethodCallExpr>
{
    protected override ExpressionSyntax? Visit(ConversionContext context, MethodCallExpr methodCallExpr)
    {
        if (TypeHelper.TryTransformMethodCall(context, methodCallExpr, out var transformedSyntax))
        {
            return transformedSyntax;
        }

        var scope = methodCallExpr.getScope().FromOptional<Expression>();
        ExpressionSyntax? scopeSyntax = null;

        var methodName = TypeHelper.Capitalize(methodCallExpr.getNameAsString());
        methodName = TypeHelper.ReplaceCommonMethodNames(methodName);

        if (scope is not null)
        {
            scopeSyntax = VisitExpression(context, scope);

            if (context.Options.ConvertSystemOutToConsole
                && scope is FieldAccessExpr fieldAccessExpr
                && fieldAccessExpr.getScope() is NameExpr nameExpr
                && nameExpr.getNameAsString() == "System"
                && fieldAccessExpr.getNameAsString() == "out"
                && scopeSyntax is IdentifierNameSyntax { Identifier: { Text: "Console" } })
            {
                if (methodCallExpr.getNameAsString() == "println")
                {
                    methodName = "WriteLine";
                }
                else if (methodCallExpr.getNameAsString() == "print")
                {
                    methodName = "Write";
                }
            }
        }

        // Override methodName if a mapping is found
        if (TryGetMappedMethodName(methodCallExpr.getNameAsString(), scope, context, out var mappedMethodName))
        {
            methodName = mappedMethodName;
        }

        ExpressionSyntax methodExpression;

        if (scopeSyntax is null)
        {
            methodExpression = SyntaxFactory.IdentifierName(methodName);
        }
        else
        {
            methodExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, scopeSyntax, SyntaxFactory.IdentifierName(methodName));
        }

        var args = methodCallExpr.getArguments();

        if (args is null || args.size() == 0)
        {
            return SyntaxFactory.InvocationExpression(methodExpression);
        }

        return SyntaxFactory.InvocationExpression(methodExpression, TypeHelper.GetSyntaxFromArguments(context, args));
    }

    private static bool TryGetMappedMethodName(string methodName, Expression? scope, ConversionContext context, out string mappedMethodName)
    {
        var mappings = context.Options.SyntaxMappings;
        if (scope is null && mappings.VoidMethodMappings.TryGetValue(methodName, out var voidMapping))
        {
            mappedMethodName = voidMapping;
            return true;
        }
        else if (scope is not null && mappings.NonVoidMethodMappings.TryGetValue(methodName, out var nonVoidMapping))
        {
            mappedMethodName = nonVoidMapping;
            return true;
        }
        mappedMethodName = methodName;
        return false;
    }
}
