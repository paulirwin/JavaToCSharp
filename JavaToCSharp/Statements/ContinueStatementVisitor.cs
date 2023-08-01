using com.github.javaparser.ast.stmt;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements;

public class ContinueStatementVisitor : StatementVisitor<ContinueStmt>
{
    public override StatementSyntax Visit(ConversionContext context, ContinueStmt cnt)
    {
        if (!System.String.IsNullOrEmpty(cnt.getId()))
            context.Options.Warning("Continue with label detected, using plain continue instead. Check for correctness.", cnt.getBegin().line);

        return SyntaxFactory.ContinueStatement();
    }
}
