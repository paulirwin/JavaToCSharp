using com.github.javaparser.ast.stmt;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements;

public class LabeledStatementVisitor : StatementVisitor<LabeledStmt>
{
    public override StatementSyntax? Visit(ConversionContext context, LabeledStmt labeledStmt)
    {
        var statement = labeledStmt.getStatement();
        var syntax = VisitStatement(context, statement);

        return syntax is null ? null : SyntaxFactory.LabeledStatement(labeledStmt.getLabel().asString(), syntax);
    }
}
