using System.Collections.Generic;
using System.Linq;
using com.github.javaparser.ast.body;
using com.github.javaparser.ast.expr;
using com.github.javaparser.ast.stmt;
using JavaToCSharp.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements
{
    public class ForStatementVisitor : StatementVisitor<ForStmt>
    {
        public override StatementSyntax? Visit(ConversionContext context, ForStmt forStmt)
        {
            var inits = forStmt.getInit().ToList<Expression>();

            var initSyntaxes = new List<ExpressionSyntax>();
            VariableDeclarationSyntax? varSyntax = null;

            if (inits != null)
            {
                foreach (var init in inits)
                {
                    if (init is VariableDeclarationExpr varExpr)
                    {
                        var type = TypeHelper.ConvertTypeOf(varExpr);

                        var variableDeclarators = varExpr.getVars()?.ToList<VariableDeclarator>() ?? new List<VariableDeclarator>();
                        var vars = variableDeclarators
                                   .Select(i => SyntaxFactory.VariableDeclarator(i.toString()))
                                   .ToArray();

                        varSyntax = SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(type), SyntaxFactory.SeparatedList(vars, Enumerable.Repeat(SyntaxFactory.Token(SyntaxKind.CommaToken), vars.Length - 1)));
                    }
                    else
                    {
                        var initSyntax = ExpressionVisitor.VisitExpression(context, init);
                        if (initSyntax is not null)
                        {
                            initSyntaxes.Add(initSyntax);
                        }
                    }
                }
            }

            var condition = forStmt.getCompare();
            var conditionSyntax = ExpressionVisitor.VisitExpression(context, condition);

            var increments = forStmt.getUpdate().ToList<Expression>();
            var incrementSyntaxes = new List<ExpressionSyntax>();

            if (increments != null)
            {
                var expressionSyntaxes = increments.Select(increment => ExpressionVisitor.VisitExpression(context, increment));
                incrementSyntaxes.AddRange(expressionSyntaxes.Where(expressionSyntax => expressionSyntax != null)!);
            }

            var body = forStmt.getBody();
            var bodySyntax = VisitStatement(context, body);

            if (bodySyntax == null)
                return null;

            return SyntaxFactory.ForStatement(bodySyntax)
                .WithDeclaration(varSyntax)
                .AddInitializers(initSyntaxes.ToArray())
                .WithCondition(conditionSyntax)
                .AddIncrementors(incrementSyntaxes.ToArray());
        }
    }
}
