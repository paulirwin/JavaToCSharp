using System.Collections.Generic;
using System.Linq;
using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions
{
    public class MethodReferenceExpressionVisitor : ExpressionVisitor<MethodReferenceExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, MethodReferenceExpr expr)
        {
            var scope = expr.getScope();
            ExpressionSyntax scopeSyntax = null;

            if (scope != null)
            {
                scopeSyntax = ExpressionVisitor.VisitExpression(context, scope);
            }

            var methodName = TypeHelper.Capitalize(expr.getIdentifier());
            methodName = TypeHelper.ReplaceCommonMethodNames(methodName);

            ExpressionSyntax methodExpression;

            if (scopeSyntax == null)
            {
                methodExpression = SyntaxFactory.IdentifierName(methodName);
            }
            else
            {
                methodExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, scopeSyntax, SyntaxFactory.IdentifierName(methodName));
            }

            var args = expr.getTypeArguments().getTypeArguments().ToList<Expression>();

            if (args == null || args.Count == 0)
                return SyntaxFactory.InvocationExpression(methodExpression);

            var argSyntaxes = new List<ArgumentSyntax>();

            foreach (var arg in args)
            {
                var argSyntax = ExpressionVisitor.VisitExpression(context, arg);
                argSyntaxes.Add(SyntaxFactory.Argument(argSyntax));
            }

            return SyntaxFactory.InvocationExpression(methodExpression, SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(argSyntaxes, Enumerable.Repeat(SyntaxFactory.Token(SyntaxKind.CommaToken), argSyntaxes.Count - 1))));
        }
    }
}
