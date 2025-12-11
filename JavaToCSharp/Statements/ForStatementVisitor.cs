using com.github.javaparser.ast.body;
using com.github.javaparser.ast.expr;
using com.github.javaparser.ast.stmt;
using JavaToCSharp.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements;

public class ForStatementVisitor : StatementVisitor<ForStmt>
{
    public override StatementSyntax? Visit(ConversionContext context, ForStmt forStmt)
    {
        var inits = forStmt.getInitialization().ToList<Expression>();

        var initSyntaxes = new List<ExpressionSyntax>();
        VariableDeclarationSyntax? varSyntax = null;

        if (inits is not null)
        {
            foreach (var init in inits)
            {
                if (init is VariableDeclarationExpr varExpr)
                {
                    var type = TypeHelper.ConvertType(varExpr.getCommonType());

                    var variableDeclarators = varExpr.getVariables()?.ToList<VariableDeclarator>() ?? [];
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

        var condition = forStmt.getCompare().FromOptional<Expression>();
        var conditionSyntax = ExpressionVisitor.VisitExpression(context, condition);

        var increments = forStmt.getUpdate().ToList<Expression>();
        var incrementSyntaxes = new List<ExpressionSyntax>();

        if (increments is not null)
        {
            var expressionSyntaxes = increments.Select(increment => ExpressionVisitor.VisitExpression(context, increment));
            incrementSyntaxes.AddRange(expressionSyntaxes.OfType<ExpressionSyntax>());
        }

        var body = forStmt.getBody();
        var bodySyntax = VisitStatement(context, body);

        if (bodySyntax is null)
        {
            return null;
        }

        return SyntaxFactory.ForStatement(bodySyntax)
            .WithDeclaration(varSyntax)
            .AddInitializers(initSyntaxes.ToArray())
            .WithCondition(conditionSyntax)
            .AddIncrementors(incrementSyntaxes.ToArray());
    }
}
