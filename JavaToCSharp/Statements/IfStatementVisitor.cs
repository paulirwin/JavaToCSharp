using com.github.javaparser.ast.stmt;
using JavaToCSharp.Expressions;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavaToCSharp.Statements
{
    public class IfStatementVisitor : StatementVisitor<IfStmt>
    {
        public override StatementSyntax Visit(ConversionContext context, IfStmt ifStmt)
        {
            var condition = ifStmt.getCondition();
            var conditionSyntax = ExpressionVisitor.VisitExpression(context, condition);

            var thenStmt = ifStmt.getThenStmt();
            var thenSyntax = StatementVisitor.VisitStatement(context, thenStmt);

            if (thenSyntax == null)
                return null;

            var elseStmt = ifStmt.getElseStmt();

            if (elseStmt == null)
                return Syntax.IfStatement(conditionSyntax, thenSyntax);

            var elseStatementSyntax = StatementVisitor.VisitStatement(context, elseStmt);
            var elseSyntax = Syntax.ElseClause(elseStatementSyntax);

            if (elseSyntax == null)
                return Syntax.IfStatement(conditionSyntax, thenSyntax);

            return Syntax.IfStatement(conditionSyntax, thenSyntax, elseSyntax);
        }
    }
}
