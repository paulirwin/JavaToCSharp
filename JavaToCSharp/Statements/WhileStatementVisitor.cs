using com.github.javaparser.ast.stmt;
using JavaToCSharp.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements
{
    public class WhileStatementVisitor : StatementVisitor<WhileStmt>
    {
        public override StatementSyntax Visit(ConversionContext context, WhileStmt whileStmt)
        {
            var expr = whileStmt.getCondition();
            var syntax = ExpressionVisitor.VisitExpression(context, expr);

            var body = whileStmt.getBody();
            var bodySyntax = StatementVisitor.VisitStatement(context, body);

            return SyntaxFactory.WhileStatement(syntax, bodySyntax ?? SyntaxFactory.EmptyStatement());
        }
    }
}
