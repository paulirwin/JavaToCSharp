using System;
using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions
{
    public class LongLiteralExpressionVisitor : ExpressionVisitor<StringLiteralExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, StringLiteralExpr expr)
        {
            var value = Convert.ToInt64(expr.toString().Trim('\"').Replace("L", String.Empty));

            return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(value));
        }
    }
}