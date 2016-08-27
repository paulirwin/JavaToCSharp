using com.github.javaparser.ast.expr;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavaToCSharp.Expressions
{
    public class ArrayAccessExpressionVisitor : ExpressionVisitor<ArrayAccessExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, ArrayAccessExpr expr)
        {
            var nameExpr = expr.getName();
            var nameSyntax = ExpressionVisitor.VisitExpression(context, nameExpr);

            var indexExpr = expr.getIndex();
            var indexSyntax = ExpressionVisitor.VisitExpression(context, indexExpr);

            return Syntax.ElementAccessExpression(nameSyntax, Syntax.BracketedArgumentList(Syntax.SeparatedList(Syntax.Argument(indexSyntax))));
        }
    }
}
