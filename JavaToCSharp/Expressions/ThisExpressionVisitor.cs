using japa.parser.ast.expr;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavaToCSharp.Expressions
{
    public class ThisExpressionVisitor : ExpressionVisitor<ThisExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, ThisExpr expr)
        {
            return Syntax.ThisExpression();
        }
    }
}
