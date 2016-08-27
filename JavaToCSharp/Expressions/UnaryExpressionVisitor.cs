using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions
{
	public class UnaryExpressionVisitor : ExpressionVisitor<UnaryExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, UnaryExpr unaryExpr)
        {
            var expr = unaryExpr.getExpr();
            var exprSyntax = ExpressionVisitor.VisitExpression(context, expr);

            var op = unaryExpr.getOperator();
            SyntaxKind kind = SyntaxKind.None;
            bool isPostfix = false;

            if (op == UnaryExpr.Operator.inverse)
                kind = SyntaxKind.BitwiseNotExpression;
            else if (op == UnaryExpr.Operator.negative)
                kind = SyntaxKind.SubtractExpression;
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
                kind = SyntaxKind.AddExpression;
            else if (op == UnaryExpr.Operator.preDecrement)
                kind = SyntaxKind.PreDecrementExpression;
            else if (op == UnaryExpr.Operator.preIncrement)
                kind = SyntaxKind.PreIncrementExpression;

            if (isPostfix)
                return SyntaxFactory.PostfixUnaryExpression(kind, exprSyntax);
            else
                return SyntaxFactory.PrefixUnaryExpression(kind, exprSyntax);
        }
    }
}
