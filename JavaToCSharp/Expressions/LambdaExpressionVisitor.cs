using System.Collections.Generic;
using com.github.javaparser.ast.body;
using com.github.javaparser.ast.expr;
using JavaToCSharp.Statements;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions;

public class LambdaExpressionVisitor : ExpressionVisitor<LambdaExpr>
{
    public override ExpressionSyntax? Visit(ConversionContext context, LambdaExpr expr)
    {
        var bodyStatement = expr.getBody();
        var bodyStatementSyntax = StatementVisitor.VisitStatement(context, bodyStatement);
        
        ParenthesizedLambdaExpressionSyntax? lambdaExpressionSyntax = null;

        if (bodyStatementSyntax is ExpressionStatementSyntax ess)
        {
            lambdaExpressionSyntax = SyntaxFactory.ParenthesizedLambdaExpression(ess.Expression);
        }
        else if(bodyStatementSyntax is not null)
        {
            lambdaExpressionSyntax = SyntaxFactory.ParenthesizedLambdaExpression(bodyStatementSyntax);
        }

        var parameters = expr.getParameters().ToList<Parameter>();

        if (parameters is not { Count: > 0 })
            return lambdaExpressionSyntax;

        var paramSyntaxes = new List<ParameterSyntax>();

        foreach (var param in parameters)
        {
            string typeName = TypeHelper.ConvertTypeOf(param);
            string identifier = TypeHelper.EscapeIdentifier(param.getNameAsString());

            if ((param.getType().getArrayLevel() > 0 && !typeName.EndsWith("[]")) || param.isVarArgs())
                typeName += "[]";

            var modifiers = SyntaxFactory.TokenList();

            if (param.isVarArgs())
                modifiers = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ParamsKeyword));

            var paramSyntax = SyntaxFactory.Parameter(
                attributeLists: new SyntaxList<AttributeListSyntax>(),
                modifiers: modifiers,
                type: SyntaxFactory.ParseTypeName(typeName),
                identifier: SyntaxFactory.ParseToken(identifier),
                @default: null);

            paramSyntaxes.Add(paramSyntax);
        }

        lambdaExpressionSyntax = lambdaExpressionSyntax?.AddParameterListParameters(paramSyntaxes.ToArray());
        return lambdaExpressionSyntax;
    }
}