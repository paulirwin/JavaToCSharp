using com.github.javaparser.ast.expr;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavaToCSharp.Expressions
{
    public class DoubleLiteralExpressionVisitor : ExpressionVisitor<DoubleLiteralExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, DoubleLiteralExpr expr)
        {
            // note: this must come before the check for StringLiteralExpr because DoubleLiteralExpr : StringLiteralExpr
            var dbl = (DoubleLiteralExpr)expr;

            if (dbl.getValue().EndsWith("f", StringComparison.OrdinalIgnoreCase))
                return Syntax.LiteralExpression(SyntaxKind.NumericLiteralExpression, Syntax.Literal(float.Parse(dbl.getValue().TrimEnd('f', 'F'))));
            else
                return Syntax.LiteralExpression(SyntaxKind.NumericLiteralExpression, Syntax.Literal(double.Parse(dbl.getValue().TrimEnd('d', 'D'))));
        }
    }
}
