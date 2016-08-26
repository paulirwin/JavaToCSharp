using com.github.javaparser.ast.body;
using com.github.javaparser.ast.expr;
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
    public class ForStatementVisitor : StatementVisitor<ForStmt>
    {
        public override StatementSyntax Visit(ConversionContext context, ForStmt forStmt)
        {
            var inits = forStmt.getInit().ToList<Expression>();

            var initSyntaxes = new List<ExpressionSyntax>();
            VariableDeclarationSyntax varSyntax = null;

            if (inits != null)
            {
                foreach (var init in inits)
                {
                    if (init is VariableDeclarationExpr)
                    {
                        var varExpr = init as VariableDeclarationExpr;

                        var type = TypeHelper.ConvertType(varExpr.getType().toString());

                        var vars = varExpr.getVars()
                            .ToList<VariableDeclarator>()
                            .Select(i => Syntax.VariableDeclarator(i.toString()))
                            .ToArray();

                        varSyntax = Syntax.VariableDeclaration(Syntax.ParseTypeName(type), Syntax.SeparatedList(vars, Enumerable.Repeat(Syntax.Token(SyntaxKind.CommaToken), vars.Length - 1)));
                    }
                    else
                    {
                        var initSyntax = ExpressionVisitor.VisitExpression(context, init);
                        initSyntaxes.Add(initSyntax);
                    }
                }
            }

            var condition = forStmt.getCompare();
            var conditionSyntax = ExpressionVisitor.VisitExpression(context, condition);

            var increments = forStmt.getUpdate().ToList<Expression>();
            var incrementSyntaxes = new List<ExpressionSyntax>();

            if (increments != null)
            {
                foreach (var increment in increments)
                {
                    var incrementSyntax = ExpressionVisitor.VisitExpression(context, increment);
                    incrementSyntaxes.Add(incrementSyntax);
                }
            }

            var body = forStmt.getBody();
            var bodySyntax = StatementVisitor.VisitStatement(context, body);

            if (bodySyntax == null)
                return null;

            return Syntax.ForStatement(bodySyntax)
                .WithDeclaration(varSyntax)
                .AddInitializers(initSyntaxes.ToArray())
                .WithCondition(conditionSyntax)
                .AddIncrementors(incrementSyntaxes.ToArray());
        }
    }
}
