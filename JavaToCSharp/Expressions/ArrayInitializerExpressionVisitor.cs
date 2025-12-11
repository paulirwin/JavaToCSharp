using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions;

public class ArrayInitializerExpressionVisitor : ExpressionVisitor<ArrayInitializerExpr>
{
    protected override ExpressionSyntax Visit(ConversionContext context, ArrayInitializerExpr expr)
    {
        var expressions = expr.getValues()?.ToList<Expression>() ?? [];
        var syntaxes = expressions.Select(valueExpression => VisitExpression(context, valueExpression))
                                  .OfType<ExpressionSyntax>() // filter out nulls
                                  .ToList();

        return SyntaxFactory.ImplicitArrayCreationExpression(
            SyntaxFactory.InitializerExpression(
                SyntaxKind.ArrayInitializerExpression,
                SyntaxFactory.SeparatedList(syntaxes, Enumerable.Repeat(SyntaxFactory.Token(SyntaxKind.CommaToken), Math.Max(0, syntaxes.Count - 1)))));
    }
}
