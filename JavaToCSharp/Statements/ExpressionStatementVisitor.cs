using com.github.javaparser;
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
        {
            return VisitVariableDeclarationStatement(context, expr);
        }

        // `target = switch (...) { ... yield ... }` lowers to a switch statement assigning to the target.
        if (expression is AssignExpr { } assignExpr
            && assignExpr.getValue() is SwitchExpr assignedSwitch
            && assignExpr.getOperator() == AssignExpr.Operator.ASSIGN
            && SwitchExpressionLowering.RequiresLowering(assignedSwitch))
        {
            var targetSyntax = ExpressionVisitor.VisitExpression(context, assignExpr.getTarget());

            if (targetSyntax is IdentifierNameSyntax identifier)
            {
                context.Options.Warning(
                    "Multi-statement switch expression converted to a switch statement. Review the generated code carefully.",
                    assignedSwitch.getBegin().FromRequiredOptional<Position>().line);

                return SwitchExpressionLowering.Lower(context, assignedSwitch, identifier.Identifier.Text);
            }
        }

        var expressionSyntax = ExpressionVisitor.VisitExpression(context, expression);

        return expressionSyntax is null ? null : SyntaxFactory.ExpressionStatement(expressionSyntax);
    }

    private static StatementSyntax? VisitVariableDeclarationStatement(ConversionContext context, VariableDeclarationExpr varExpr)
    {
        var variableDeclarators = varExpr.getVariables()?.ToList<VariableDeclarator>() ?? [];

        // Java allows C-style array brackets on individual declarators, so a single declaration can mix
        // ranks (`int multi[][] = ..., single[] = ...;`). C# has no equivalent, and asking JavaParser for
        // a common type throws in that case, so emit one C# declaration per distinct array rank. The
        // groups stay flat siblings rather than a nested block so the variables remain in the same scope.
        var declaratorGroups = variableDeclarators
            .GroupBy(item => item.getType().getArrayLevel())
            .ToList();

        if (declaratorGroups.Count > 1)
        {
            StatementSyntax? last = null;

            foreach (var group in declaratorGroups)
            {
                if (last is not null)
                {
                    context.PendingStatements.Add(last);
                }

                last = VisitVariableDeclarationGroup(context, group.First().getType(), group.ToList());
            }

            return last;
        }

        return VisitVariableDeclarationGroup(context, varExpr.getCommonType(), variableDeclarators);
    }

    private static StatementSyntax? VisitVariableDeclarationGroup(
        ConversionContext context,
        com.github.javaparser.ast.type.Type commonType,
        List<VariableDeclarator> variableDeclarators)
    {
        int? arrayRank = null;

        var variables = new List<VariableDeclaratorSyntax>();
        var loweredSwitches = new List<StatementSyntax>();

        foreach (var item in variableDeclarators)
        {
            var type = item.getType();

            arrayRank ??= type.getArrayLevel();

            string name = item.getNameAsString();

            if (type.getArrayLevel() > 0)
            {
                while (name.EndsWith("[]"))
                {
                    name = name[..^2];
                }
            }

            var initExpr = item.getInitializer().FromOptional<Expression>();

            if (initExpr is SwitchExpr switchExpr && SwitchExpressionLowering.RequiresLowering(switchExpr))
            {
                // C# switch expressions cannot contain multiple statements per arm, so lower this
                // into a switch statement that assigns to the declared variable. The declaration is
                // emitted without an initializer and the switch statement precedes the current statement.
                context.Options.Warning(
                    "Multi-statement switch expression converted to a switch statement. Review the generated code carefully.",
                    switchExpr.getBegin().FromRequiredOptional<Position>().line);

                variables.Add(SyntaxFactory.VariableDeclarator(TypeHelper.EscapeIdentifier(name)));

                loweredSwitches.Add(
                    SwitchExpressionLowering.Lower(context, switchExpr, TypeHelper.EscapeIdentifier(name)));

                continue;
            }

            if (initExpr is not null)
            {
                var initSyntax = ExpressionVisitor.VisitExpression(context, initExpr);
                if (initSyntax is not null)
                {
                    var varDeclarationSyntax = SyntaxFactory.VariableDeclarator(TypeHelper.EscapeIdentifier(name)).WithInitializer(SyntaxFactory.EqualsValueClause(initSyntax));
                    variables.Add(varDeclarationSyntax);
                }
            }
            else
            {
                variables.Add(SyntaxFactory.VariableDeclarator(TypeHelper.EscapeIdentifier(name)));
            }
        }

        var typeSyntax = TypeHelper.ConvertTypeSyntax(commonType, arrayRank ?? 0);

        var declaration = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(typeSyntax, SyntaxFactory.SeparatedList(variables, Enumerable.Repeat(SyntaxFactory.Token(SyntaxKind.CommaToken), variables.Count - 1))));

        if (loweredSwitches.Count == 0)
        {
            return declaration;
        }

        // The declaration must precede the switch statements that assign to it, so emit it first
        // and return the final switch as this statement's syntax.
        context.PendingStatements.Add(declaration);
        context.PendingStatements.AddRange(loweredSwitches[..^1]);

        return loweredSwitches[^1];
    }
}
