using com.github.javaparser.ast.stmt;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements
{
	public class BlockStatementVisitor : StatementVisitor<BlockStmt>
    {
        public override StatementSyntax Visit(ConversionContext context, BlockStmt blockStmt)
        {
            var stmts = blockStmt.getStmts().ToList<Statement>();

            var syntaxes = StatementVisitor.VisitStatements(context, stmts);

            return SyntaxFactory.Block(syntaxes);
        }
    }
}
