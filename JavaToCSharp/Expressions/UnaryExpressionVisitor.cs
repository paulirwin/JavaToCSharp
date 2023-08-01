using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions;

public class UnaryExpressionVisitor : ExpressionVisitor<UnaryExpr>
{
    public override ExpressionSyntax? Visit(ConversionContext context, UnaryExpr unaryExpr)
    {
        var expr = unaryExpr.getExpr();
        var exprSyntax = VisitExpression(context, expr);
        if (exprSyntax is null)
        {
            return null;
        }

        var op = unaryExpr.getOperator();
        var kind = SyntaxKind.None;
        bool isPostfix = false;

        if (op == UnaryExpr.Operator.inverse)
            kind = SyntaxKind.BitwiseNotExpression;
        else if (op == UnaryExpr.Operator.negative)
            kind = SyntaxKind.UnaryMinusExpression;
        else if (op == UnaryExpr.Operator.not)
            kind = SyntaxKind.LogicalNotExpression;
        else if (op == UnaryExpr.Operator.posDecrement)
        {
            kind = SyntaxKind.PostDecrementExpression;
            isPostfix = true;
        }
        else if (op == UnaryExpr.Operator.posIncrement)
        {
            kind = SyntaxKind.PostIncrementExpression;
            isPostfix = true;
        }
        else if (op == UnaryExpr.Operator.positive)
            kind = SyntaxKind.UnaryPlusExpression;
        else if (op == UnaryExpr.Operator.preDecrement)
            kind = SyntaxKind.PreDecrementExpression;
        else if (op == UnaryExpr.Operator.preIncrement)
            kind = SyntaxKind.PreIncrementExpression;

        return isPostfix
                   ? SyntaxFactory.PostfixUnaryExpression(kind, exprSyntax)
                   : SyntaxFactory.PrefixUnaryExpression(kind, exprSyntax);
    }
}
