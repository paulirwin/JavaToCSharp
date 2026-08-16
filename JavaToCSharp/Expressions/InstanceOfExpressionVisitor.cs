using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions;

public class InstanceOfExpressionVisitor : ExpressionVisitor<InstanceOfExpr>
{
    protected override ExpressionSyntax? Visit(ConversionContext context, InstanceOfExpr expr)
    {
        var innerExpr = expr.getExpression();
        var exprSyntax = VisitExpression(context, innerExpr);

        if (exprSyntax is null)
        {
            return null;
        }

        // Java 16+ allows `x instanceof Shape s` and Java 21 `x instanceof Point(int a, int b)`.
        // Both become C# `is` patterns; without a pattern this is a plain type test.
        if (expr.getPattern().FromOptional<PatternExpr>() is { } pattern
            && PatternExpressionVisitor.ConvertPattern(context, pattern) is { } patternSyntax)
        {
            return SyntaxFactory.IsPatternExpression(exprSyntax, patternSyntax);
        }

        var type = TypeHelper.ConvertTypeOf(expr);

        return SyntaxFactory.BinaryExpression(SyntaxKind.IsExpression, exprSyntax, SyntaxFactory.IdentifierName(type));
    }
}
