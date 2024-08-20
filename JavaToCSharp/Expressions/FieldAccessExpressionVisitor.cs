using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions;

public class FieldAccessExpressionVisitor : ExpressionVisitor<FieldAccessExpr>
{
    protected override ExpressionSyntax? Visit(ConversionContext context, FieldAccessExpr fieldAccessExpr)
    {
        var scope = fieldAccessExpr.getScope();
        ExpressionSyntax? scopeSyntax = null;

        if (scope != null)
        {
            scopeSyntax = VisitExpression(context, scope);

            // TODO.PI: This should probably live in TypeHelper somehow
            if (context.Options.ConvertSystemOutToConsole)
            {
                if (scopeSyntax is IdentifierNameSyntax { Identifier.Text: "System" }
                    && fieldAccessExpr.getNameAsString() == "out")
                {
                    return SyntaxFactory.IdentifierName("Console");
                }
            }
        }

        if (scopeSyntax is null)
        {
            return null;
        }

        var field = TypeHelper.EscapeIdentifier(fieldAccessExpr.getNameAsString());
        // array length accessor should be capitalized
        field = field == "length" ? "Length" : field;

        return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, scopeSyntax, SyntaxFactory.IdentifierName(field));
    }
}
