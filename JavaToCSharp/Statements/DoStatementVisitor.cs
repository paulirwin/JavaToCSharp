using com.github.javaparser.ast.stmt;
using JavaToCSharp.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements
{
    public class DoStatementVisitor : StatementVisitor<DoStmt>
    {
        public override StatementSyntax Visit(ConversionContext context, DoStmt statement)
        {
            var condition = statement.getCondition();
            var conditionSyntax = ExpressionVisitor.VisitExpression(context, condition);

            var body = statement.getBody();
            var bodySyntax = StatementVisitor.VisitStatement(context, body);

            return SyntaxFactory.DoStatement(bodySyntax, conditionSyntax);
        }
    }
}
