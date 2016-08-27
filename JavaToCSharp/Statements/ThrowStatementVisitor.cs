using com.github.javaparser.ast.stmt;
using JavaToCSharp.Expressions;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavaToCSharp.Statements
{
    public class ThrowStatementVisitor : StatementVisitor<ThrowStmt>
    {
        public override StatementSyntax Visit(ConversionContext context, ThrowStmt throwStmt)
        {
            var expr = throwStmt.getExpr();

            var exprSyntax = ExpressionVisitor.VisitExpression(context, expr);

            return Syntax.ThrowStatement(exprSyntax);
        }
    }
}
