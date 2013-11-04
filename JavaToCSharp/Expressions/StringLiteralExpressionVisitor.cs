using japa.parser.ast.expr;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavaToCSharp.Expressions
{
    public class StringLiteralExpressionVisitor : ExpressionVisitor<StringLiteralExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, StringLiteralExpr expr)
        {
            return Syntax.LiteralExpression(SyntaxKind.StringLiteralExpression, Syntax.Literal(expr.toString().Trim('\"')));
        }
    }
}
