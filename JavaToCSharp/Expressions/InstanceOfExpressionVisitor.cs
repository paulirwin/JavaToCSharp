using japa.parser.ast.expr;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavaToCSharp.Expressions
{
    public class InstanceOfExpressionVisitor : ExpressionVisitor<InstanceOfExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, InstanceOfExpr expr)
        {
            var innerExpr = expr.getExpr();
            var exprSyntax = ExpressionVisitor.VisitExpression(context, innerExpr);

            var type = TypeHelper.ConvertType(expr.getType().toString());

            return Syntax.BinaryExpression(SyntaxKind.IsExpression, exprSyntax, Syntax.IdentifierName(type));
        }
    }
}
