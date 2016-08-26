using com.github.javaparser.ast.expr;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavaToCSharp.Expressions
{
    public class IntegerLiteralExpressionVisitor : ExpressionVisitor<IntegerLiteralExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, IntegerLiteralExpr expr)
        {
            string value = expr.toString();

            if (value.StartsWith("0x"))
                return Syntax.LiteralExpression(SyntaxKind.NumericLiteralExpression, Syntax.Literal(value, Convert.ToInt32(value.Substring(2), 16)));
            else
                return Syntax.LiteralExpression(SyntaxKind.NumericLiteralExpression, Syntax.Literal(int.Parse(expr.toString())));
        }
    }
}
