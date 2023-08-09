using com.github.javaparser;
using com.github.javaparser.ast.stmt;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements;

public class BreakStatementVisitor : StatementVisitor<BreakStmt>
{
    public override StatementSyntax Visit(ConversionContext context, BreakStmt brk)
    {
        if (brk.getLabel().isPresent())
            context.Options.Warning("Break with label detected, using plain break instead. Check for correctness.", brk.getBegin().FromRequiredOptional<Position>().line);

        return SyntaxFactory.BreakStatement();
    }
}
