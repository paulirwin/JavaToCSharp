using com.github.javaparser.ast.expr;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavaToCSharp.Expressions
{
    public class SuperExpressionVisitor : ExpressionVisitor<SuperExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, SuperExpr expr)
        {
            return Syntax.BaseExpression();
        }
    }
}
