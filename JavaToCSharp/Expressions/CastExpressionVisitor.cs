using com.github.javaparser.ast.expr;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavaToCSharp.Expressions
{
    public class CastExpressionVisitor : ExpressionVisitor<CastExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, CastExpr castExpr)
        {
            var expr = castExpr.getExpr();
            var exprSyntax = ExpressionVisitor.VisitExpression(context, expr);

            var type = TypeHelper.ConvertType(castExpr.getType().toString());

            return Syntax.CastExpression(Syntax.ParseTypeName(type), exprSyntax);
        }
    }
}
