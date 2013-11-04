using japa.parser.ast.expr;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavaToCSharp.Expressions
{
    public class FieldAccessExpressionVisitor : ExpressionVisitor<FieldAccessExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, FieldAccessExpr fieldAccessExpr)
        {
            var scope = fieldAccessExpr.getScope();
            ExpressionSyntax scopeSyntax = null;

            if (scope != null)
            {
                scopeSyntax = ExpressionVisitor.VisitExpression(context, scope);
            }

            var field = fieldAccessExpr.getField();

            return Syntax.MemberAccessExpression(SyntaxKind.MemberAccessExpression, scopeSyntax, Syntax.IdentifierName(field));
        }
    }
}
