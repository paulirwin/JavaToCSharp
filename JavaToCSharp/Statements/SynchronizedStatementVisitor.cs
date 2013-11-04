using japa.parser.ast.stmt;
using JavaToCSharp.Expressions;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavaToCSharp.Statements
{
    public class SynchronizedStatementVisitor : StatementVisitor<SynchronizedStmt>
    {
        public override StatementSyntax Visit(ConversionContext context, SynchronizedStmt synchronizedStmt)
        {
            var lockExpr = synchronizedStmt.getExpr();
            var lockSyntax = ExpressionVisitor.VisitExpression(context, lockExpr);

            var body = synchronizedStmt.getBlock();
            var bodySyntax = new BlockStatementVisitor().Visit(context, body);

            if (bodySyntax == null)
                return null;

            return Syntax.LockStatement(lockSyntax, bodySyntax);
        }
    }
}
