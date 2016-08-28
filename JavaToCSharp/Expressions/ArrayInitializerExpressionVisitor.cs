using System.Collections.Generic;
using System.Linq;
using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions
{
    public class ArrayInitializerExpressionVisitor : ExpressionVisitor<ArrayInitializerExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, ArrayInitializerExpr expr)
        {
            var exprs = expr.getValues().ToList<Expression>();

            var syntaxes = new List<ExpressionSyntax>();

            foreach (var valuexpr in exprs)
            {
                var syntax = ExpressionVisitor.VisitExpression(context, valuexpr);
                syntaxes.Add(syntax);
            }

            return SyntaxFactory.ImplicitArrayCreationExpression(
                SyntaxFactory.InitializerExpression(
                    SyntaxKind.ArrayInitializerExpression, 
                    SyntaxFactory.SeparatedList(syntaxes, Enumerable.Repeat(SyntaxFactory.Token(SyntaxKind.CommaToken), syntaxes.Count - 1))));
        }
    }
}
