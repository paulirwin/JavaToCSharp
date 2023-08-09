using com.github.javaparser.ast;
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
            ExpressionSyntax? scopeSyntax = null;

            if (scope != null)
            {
                scopeSyntax = VisitExpression(context, scope);
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

            var args = expr.getTypeArguments().FromOptional<NodeList>();
            if (args == null || args.size() == 0)
                return SyntaxFactory.InvocationExpression(methodExpression);

            return SyntaxFactory.InvocationExpression(methodExpression, TypeHelper.GetSyntaxFromArguments(context, args));
        }
    }
}
