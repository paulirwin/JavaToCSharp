using com.github.javaparser.ast.body;
using com.github.javaparser.ast.expr;
using com.github.javaparser.ast.stmt;
using JavaToCSharp.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements;

public class ExpressionStatementVisitor : StatementVisitor<ExpressionStmt>
{
    public override StatementSyntax? Visit(ConversionContext context, ExpressionStmt exprStmt)
    {
        var expression = exprStmt.getExpression();

        // handle special case where AST is different
        if (expression is VariableDeclarationExpr expr)
            return VisitVariableDeclarationStatement(context, expr);

        var expressionSyntax = ExpressionVisitor.VisitExpression(context, expression);

        return expressionSyntax is null ? null : SyntaxFactory.ExpressionStatement(expressionSyntax);
    }

    private static StatementSyntax VisitVariableDeclarationStatement(ConversionContext context, VariableDeclarationExpr varExpr)
    {
        var commonType = varExpr.getCommonType();
        int? arrayRank = null;

        var variables = new List<VariableDeclaratorSyntax>();

        var variableDeclarators = varExpr.getVariables()?.ToList<VariableDeclarator>() ?? [];

        foreach (var item in variableDeclarators)
        {
            var type = item.getType();

            if (arrayRank is not null && type.getArrayLevel() != arrayRank)
            {
                throw new InvalidOperationException("Different array levels in the same field declaration are not yet supported");
            }

            arrayRank ??= type.getArrayLevel();

            var id = item.getType();
            string name = item.getNameAsString();

            if (type.getArrayLevel() > 0)
            {
                while (name.EndsWith("[]"))
                {
                    name = name[..^2];
                }
            }

            var initExpr = item.getInitializer().FromOptional<Expression>();

            if (initExpr != null)
            {
                var initSyntax = ExpressionVisitor.VisitExpression(context, initExpr);
                if (initSyntax is not null)
                {
                    var varDeclarationSyntax = SyntaxFactory.VariableDeclarator(name).WithInitializer(SyntaxFactory.EqualsValueClause(initSyntax));
                    variables.Add(varDeclarationSyntax);
                }
            }
            else
                variables.Add(SyntaxFactory.VariableDeclarator(name));
        }

        var typeSyntax = TypeHelper.ConvertTypeSyntax(commonType, arrayRank ?? 0);

        return SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(typeSyntax, SyntaxFactory.SeparatedList(variables, Enumerable.Repeat(SyntaxFactory.Token(SyntaxKind.CommaToken), variables.Count - 1))));
    }
}
