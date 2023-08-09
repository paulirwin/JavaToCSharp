using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions
{
    public class FieldAccessExpressionVisitor : ExpressionVisitor<FieldAccessExpr>
    {
        public override ExpressionSyntax? Visit(ConversionContext context, FieldAccessExpr fieldAccessExpr)
        {
            var scope = fieldAccessExpr.getScope();
            ExpressionSyntax? scopeSyntax = null;

            if (scope != null)
            {
                scopeSyntax = VisitExpression(context, scope);
            }

            if (scopeSyntax is null)
            {
                return null;
            }

            var field = TypeHelper.EscapeIdentifier(fieldAccessExpr.getNameAsString());
            return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, scopeSyntax, SyntaxFactory.IdentifierName(field));
        }
    }
}
