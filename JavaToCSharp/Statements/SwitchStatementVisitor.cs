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
    public class SwitchStatementVisitor : StatementVisitor<SwitchStmt>
    {
        public override StatementSyntax Visit(ConversionContext context, SwitchStmt switchStmt)
        {
            var selector = switchStmt.getSelector();
            var selectorSyntax = ExpressionVisitor.VisitExpression(context, selector);

            var cases = switchStmt.getEntries().ToList<SwitchEntryStmt>();

            if (cases == null)
                return Syntax.SwitchStatement(selectorSyntax, Syntax.List<SwitchSectionSyntax>());

            var caseSyntaxes = new List<SwitchSectionSyntax>();

            foreach (var cs in cases)
            {
                var label = cs.getLabel();

                var statements = cs.getStmts().ToList<Statement>();
                var syntaxes = StatementVisitor.VisitStatements(context, statements);

                if (label == null)
                {
                    // default case
                    var defaultSyntax = Syntax.SwitchSection(Syntax.List(Syntax.SwitchLabel(SyntaxKind.DefaultSwitchLabel)), Syntax.List(syntaxes.AsEnumerable()));
                    caseSyntaxes.Add(defaultSyntax);
                }
                else
                {
                    var labelSyntax = ExpressionVisitor.VisitExpression(context, label);

                    var caseSyntax = Syntax.SwitchSection(Syntax.List(Syntax.SwitchLabel(SyntaxKind.CaseSwitchLabel, labelSyntax)), Syntax.List(syntaxes.AsEnumerable()));
                    caseSyntaxes.Add(caseSyntax);
                }
            }

            return Syntax.SwitchStatement(selectorSyntax, Syntax.List<SwitchSectionSyntax>(caseSyntaxes));
        }
    }
}
