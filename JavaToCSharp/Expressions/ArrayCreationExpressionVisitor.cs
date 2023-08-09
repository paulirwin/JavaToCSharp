using System.Collections.Generic;
using System.Linq;
using com.github.javaparser.ast;
using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions
{
    public class ArrayCreationExpressionVisitor : ExpressionVisitor<ArrayCreationExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, ArrayCreationExpr expr)
        {
            var type = TypeHelper.ConvertType(expr.getElementType());

            var rankDimensions = expr.getLevels().ToList<ArrayCreationLevel>();

            var initializer = expr.getInitializer().FromOptional<ArrayInitializerExpr>();

            var rankSyntaxes = new List<ExpressionSyntax>();

            if (rankDimensions != null)
            {
                var expressionSyntaxes = rankDimensions.Select(dimension => VisitExpression(context, dimension.getDimension().FromOptional<Expression>()))
                                                       .Where(syntax => syntax != null);
                rankSyntaxes.AddRange(expressionSyntaxes!);
            }

            if (initializer == null)
                return SyntaxFactory.ArrayCreationExpression(SyntaxFactory.ArrayType(SyntaxFactory.ParseTypeName(type)))
                    .AddTypeRankSpecifiers(SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SeparatedList(rankSyntaxes, Enumerable.Repeat(SyntaxFactory.Token(SyntaxKind.CommaToken), rankSyntaxes.Count - 1))));

            // todo: support multi-dimensional and jagged arrays

            var values = initializer.getValues()?.ToList<Expression>() ?? new List<Expression>();
            var syntaxes = values.Select(value => VisitExpression(context, value))
                                 .Where(syntax => syntax != null)!
                                 .ToList<ExpressionSyntax>();
            var initSyntax =
                syntaxes.Any() ?
                SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression, SyntaxFactory.SeparatedList(syntaxes, Enumerable.Repeat(SyntaxFactory.Token(SyntaxKind.CommaToken), syntaxes.Count - 1))) :
                SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression);

            return SyntaxFactory.ArrayCreationExpression(SyntaxFactory.ArrayType(SyntaxFactory.ParseTypeName(type)), initSyntax);
        }
    }
}
