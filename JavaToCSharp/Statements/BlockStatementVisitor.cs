using com.github.javaparser.ast.stmt;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavaToCSharp.Statements
{
    public class BlockStatementVisitor : StatementVisitor<BlockStmt>
    {
        public override StatementSyntax Visit(ConversionContext context, BlockStmt blockStmt)
        {
            var stmts = blockStmt.getStmts().ToList<Statement>();

            var syntaxes = StatementVisitor.VisitStatements(context, stmts);

            return Syntax.Block(syntaxes);
        }
    }
}
