using japa.parser.ast.expr;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavaToCSharp.Expressions
{
    public class NameExpressionVisitor : ExpressionVisitor<NameExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, NameExpr nameExpr)
        {
            return Syntax.IdentifierName(nameExpr.getName());
        }
    }
}
