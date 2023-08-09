using com.github.javaparser.ast.stmt;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements;

public class BlockStatementVisitor : StatementVisitor<BlockStmt>
{
    public override StatementSyntax Visit(ConversionContext context, BlockStmt blockStmt)
    {
        var statements = blockStmt.getStatements().ToList<Statement>();
        var syntaxes = VisitStatements(context, statements);
        return SyntaxFactory.Block(syntaxes);
    }
}