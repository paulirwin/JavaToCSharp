using com.github.javaparser.ast.expr;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            return Syntax.ImplicitArrayCreationExpression(
                Syntax.InitializerExpression(
                    SyntaxKind.ArrayInitializerExpression, 
                    Syntax.SeparatedList(syntaxes, Enumerable.Repeat(Syntax.Token(SyntaxKind.CommaToken), syntaxes.Count - 1))));
        }
    }
}
