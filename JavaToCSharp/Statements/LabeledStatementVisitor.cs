using com.github.javaparser.ast.stmt;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements
{
	public class LabeledStatementVisitor : StatementVisitor<LabeledStmt>
    {
        public override StatementSyntax Visit(ConversionContext context, LabeledStmt labeledStmt)
        {
            var statement = labeledStmt.getStmt();
            var syntax = StatementVisitor.VisitStatement(context, statement);

            if (syntax == null)
                return null;

            return SyntaxFactory.LabeledStatement(labeledStmt.getLabel(), syntax);
        }
    }
}
