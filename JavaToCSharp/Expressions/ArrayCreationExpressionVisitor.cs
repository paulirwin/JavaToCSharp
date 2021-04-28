using System.Collections.Generic;
using System.Linq;
using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions
{
    public class ArrayCreationExpressionVisitor : ExpressionVisitor<ArrayCreationExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, ArrayCreationExpr expr)
        {
            var type = TypeHelper.ConvertTypeOf(expr);

            var rankDimensions = expr.getDimensions().ToList<Expression>();

            var initializer = expr.getInitializer();

            var rankSyntaxes = new List<ExpressionSyntax>();

            if (rankDimensions != null)
            {
                rankSyntaxes.AddRange(rankDimensions.Select(dimension => VisitExpression(context, dimension)));
            }

            if (initializer == null)
                return SyntaxFactory.ArrayCreationExpression(SyntaxFactory.ArrayType(SyntaxFactory.ParseTypeName(type)))
                    .AddTypeRankSpecifiers(SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SeparatedList(rankSyntaxes, Enumerable.Repeat(SyntaxFactory.Token(SyntaxKind.CommaToken), rankSyntaxes.Count - 1))));

            // todo: support multi-dimensional and jagged arrays

            var values = initializer.getValues().ToList<Expression>();

            var syntaxes = values.Select(value => VisitExpression(context, value)).ToList();

            var initSyntax =
                syntaxes.Any() ?
                SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression, SyntaxFactory.SeparatedList(syntaxes, Enumerable.Repeat(SyntaxFactory.Token(SyntaxKind.CommaToken), syntaxes.Count - 1))) :
                SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression);

            return SyntaxFactory.ArrayCreationExpression(SyntaxFactory.ArrayType(SyntaxFactory.ParseTypeName(type)), initSyntax);
        }
    }
}
