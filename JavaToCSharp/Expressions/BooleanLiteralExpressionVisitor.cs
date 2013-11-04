using japa.parser.ast.expr;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavaToCSharp.Expressions
{
    public class BooleanLiteralExpressionVisitor : ExpressionVisitor<BooleanLiteralExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, BooleanLiteralExpr expr)
        {
            if (expr.getValue())
                return Syntax.LiteralExpression(SyntaxKind.TrueLiteralExpression);
            else
                return Syntax.LiteralExpression(SyntaxKind.FalseLiteralExpression);
        }
    }
}
