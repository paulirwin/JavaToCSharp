using com.github.javaparser.ast.expr;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavaToCSharp.Expressions
{
    public class CharLiteralExpressionVisitor : ExpressionVisitor<CharLiteralExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, CharLiteralExpr expr)
        {
            return Syntax.LiteralExpression(SyntaxKind.CharacterLiteralExpression, Syntax.Literal(expr.toString().Trim('\'')[0]));
        }
    }
}
