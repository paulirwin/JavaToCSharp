using com.github.javaparser;
using com.github.javaparser.ast.stmt;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements
{
    public class ContinueStatementVisitor : StatementVisitor<ContinueStmt>
    {
        public override StatementSyntax Visit(ConversionContext context, ContinueStmt cnt)
        {
            if (cnt.getLabel().isPresent())
                context.Options.Warning("Continue with label detected, using plain continue instead. Check for correctness.", cnt.getBegin().FromRequiredOptional<Position>().line);

            return SyntaxFactory.ContinueStatement();
        }
    }
}
