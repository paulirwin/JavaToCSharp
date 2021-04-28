using com.github.javaparser.ast.stmt;
using JavaToCSharp.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements
{
    public class IfStatementVisitor : StatementVisitor<IfStmt>
    {
        public override StatementSyntax Visit(ConversionContext context, IfStmt ifStmt)
        {
            var condition = ifStmt.getCondition();
            var conditionSyntax = ExpressionVisitor.VisitExpression(context, condition);

            var thenStmt = ifStmt.getThenStmt();
            var thenSyntax = VisitStatement(context, thenStmt);

            if (thenSyntax == null)
                return null;

            var elseStmt = ifStmt.getElseStmt();

            if (elseStmt == null)
                return SyntaxFactory.IfStatement(conditionSyntax, thenSyntax);

            var elseStatementSyntax = VisitStatement(context, elseStmt);
            var elseSyntax = SyntaxFactory.ElseClause(elseStatementSyntax);

            return elseSyntax == null 
                ? SyntaxFactory.IfStatement(conditionSyntax, thenSyntax) 
                : SyntaxFactory.IfStatement(conditionSyntax, thenSyntax, elseSyntax);
        }
    }
}
