using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions;

public class MethodCallExpressionVisitor : ExpressionVisitor<MethodCallExpr>
{
    public override ExpressionSyntax? Visit(ConversionContext context, MethodCallExpr methodCallExpr)
    {
        if (TypeHelper.TryTransformMethodCall(context, methodCallExpr, out var transformedSyntax))
        {
            return transformedSyntax;
        }

        var scope = methodCallExpr.getScope().FromOptional<Expression>();
        ExpressionSyntax? scopeSyntax = null;
        
        var methodName = TypeHelper.Capitalize(methodCallExpr.getNameAsString());
        methodName = TypeHelper.ReplaceCommonMethodNames(methodName);

        if (scope != null)
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

        ExpressionSyntax methodExpression;

        if (scopeSyntax == null)
        {
            methodExpression = SyntaxFactory.IdentifierName(methodName);
        }
        else
        {
            methodExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, scopeSyntax, SyntaxFactory.IdentifierName(methodName));
        }

        var args = methodCallExpr.getArguments();
        if (args == null || args.size() == 0)
            return SyntaxFactory.InvocationExpression(methodExpression);

        return SyntaxFactory.InvocationExpression(methodExpression, TypeHelper.GetSyntaxFromArguments(context, args));
    }
}
