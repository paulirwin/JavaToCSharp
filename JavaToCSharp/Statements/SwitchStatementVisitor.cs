using System.Collections.Generic;
using System.Linq;
using com.github.javaparser.ast.stmt;
using JavaToCSharp.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements
{
    public class SwitchStatementVisitor : StatementVisitor<SwitchStmt>
    {
        public override StatementSyntax? Visit(ConversionContext context, SwitchStmt switchStmt)
        {
            var selector = switchStmt.getSelector();
            var selectorSyntax = ExpressionVisitor.VisitExpression(context, selector);
            if (selectorSyntax is null)
            {
                return null;
            }

            var cases = switchStmt.getEntries().ToList<SwitchEntryStmt>();

            if (cases == null)
                return SyntaxFactory.SwitchStatement(selectorSyntax, SyntaxFactory.List<SwitchSectionSyntax>());

            var caseSyntaxes = new List<SwitchSectionSyntax>();

            foreach (var cs in cases)
            {
                var label = cs.getLabel();

                var statements = cs.getStmts().ToList<Statement>();
                var syntaxes = VisitStatements(context, statements);

                if (label == null)
                {
                    // default case
                    var hasBreakStmt = false;
                    foreach (var syntax in syntaxes)
                    {
                        if (syntax.Kind() == SyntaxKind.BreakStatement)
                        {
                            hasBreakStmt = true;
                        }
                    }
                    if (!hasBreakStmt)
                    {
                        syntaxes.Add(SyntaxFactory.BreakStatement());
                    }

                    var defaultSyntax = SyntaxFactory.SwitchSection(
                        SyntaxFactory.List(new List<SwitchLabelSyntax> { SyntaxFactory.DefaultSwitchLabel() }),
                        SyntaxFactory.List(syntaxes.AsEnumerable()));
                    caseSyntaxes.Add(defaultSyntax);
                }
                else
                {
                    var labelSyntax = ExpressionVisitor.VisitExpression(context, label);
                    if (labelSyntax is null)
                    {
                        return null;
                    }

                    var caseSyntax = SyntaxFactory.SwitchSection(
                        SyntaxFactory.List(new List<SwitchLabelSyntax> { SyntaxFactory.CaseSwitchLabel(labelSyntax) }),
                        SyntaxFactory.List(syntaxes.AsEnumerable()));
                    caseSyntaxes.Add(caseSyntax);
                }
            }

            return SyntaxFactory.SwitchStatement(selectorSyntax, SyntaxFactory.List(caseSyntaxes));
        }
    }
}
