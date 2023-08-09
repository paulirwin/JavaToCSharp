using System.Collections.Generic;
using System.Linq;
using com.github.javaparser.ast.expr;
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

            var cases = switchStmt.getEntries().ToList<SwitchEntry>();

            if (cases == null)
                return SyntaxFactory.SwitchStatement(selectorSyntax, SyntaxFactory.List<SwitchSectionSyntax>());

            var caseSyntaxes = new List<SwitchSectionSyntax>();

            foreach (var cs in cases)
            {
                var labels = cs.getLabels().ToList<Expression>();

                var statements = cs.getStatements().ToList<Statement>();
                var syntaxes = VisitStatements(context, statements);

                if (labels is not { Count: > 0 })
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
                    var labelSyntaxes = labels
                        .Select(i => ExpressionVisitor.VisitExpression(context, i))
                        .Where(i => i != null)
                        .Select(i => (SwitchLabelSyntax)SyntaxFactory.CaseSwitchLabel(i!));
                    
                    var caseSyntax = SyntaxFactory.SwitchSection(
                        SyntaxFactory.List(labelSyntaxes.ToList()),
                        SyntaxFactory.List(syntaxes.AsEnumerable()));
                    caseSyntaxes.Add(caseSyntax);
                }
            }

            return SyntaxFactory.SwitchStatement(selectorSyntax, SyntaxFactory.List(caseSyntaxes));
        }
    }
}
