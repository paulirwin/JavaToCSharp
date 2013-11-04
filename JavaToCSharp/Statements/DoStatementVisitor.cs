using japa.parser.ast.stmt;
using JavaToCSharp.Expressions;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            return Syntax.DoStatement(bodySyntax, conditionSyntax);
        }
    }
}
