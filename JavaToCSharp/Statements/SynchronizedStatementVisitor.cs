using com.github.javaparser.ast.stmt;
using JavaToCSharp.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements;

public class SynchronizedStatementVisitor : StatementVisitor<SynchronizedStmt>
{
    public override StatementSyntax? Visit(ConversionContext context, SynchronizedStmt synchronizedStmt)
    {
        var lockExpr = synchronizedStmt.getExpression();
        var lockSyntax = ExpressionVisitor.VisitExpression(context, lockExpr);
        if (lockSyntax is null)
        {
            return null;
        }

        var body = synchronizedStmt.getBody();
        var bodySyntax = new BlockStatementVisitor().Visit(context, body);
        return SyntaxFactory.LockStatement(lockSyntax, bodySyntax);
    }
}