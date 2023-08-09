using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions
{
    public class UnaryExpressionVisitor : ExpressionVisitor<UnaryExpr>
    {
        public override ExpressionSyntax? Visit(ConversionContext context, UnaryExpr unaryExpr)
        {
            var expr = unaryExpr.getExpression();
            var exprSyntax = VisitExpression(context, expr);
            if (exprSyntax is null)
            {
                return null;
            }

            var op = unaryExpr.getOperator();
            var kind = SyntaxKind.None;
            bool isPostfix = false;

            if (op == UnaryExpr.Operator.BITWISE_COMPLEMENT)
                kind = SyntaxKind.BitwiseNotExpression;
            else if (op == UnaryExpr.Operator.MINUS)
                kind = SyntaxKind.UnaryMinusExpression;
            else if (op == UnaryExpr.Operator.LOGICAL_COMPLEMENT)
                kind = SyntaxKind.LogicalNotExpression;
            else if (op == UnaryExpr.Operator.POSTFIX_DECREMENT)
            {
                kind = SyntaxKind.PostDecrementExpression;
                isPostfix = true;
            }
            else if (op == UnaryExpr.Operator.POSTFIX_INCREMENT)
            {
                kind = SyntaxKind.PostIncrementExpression;
                isPostfix = true;
            }
            else if (op == UnaryExpr.Operator.PLUS)
                kind = SyntaxKind.UnaryPlusExpression;
            else if (op == UnaryExpr.Operator.PREFIX_DECREMENT)
                kind = SyntaxKind.PreDecrementExpression;
            else if (op == UnaryExpr.Operator.PREFIX_INCREMENT)
                kind = SyntaxKind.PreIncrementExpression;

            return isPostfix
                       ? SyntaxFactory.PostfixUnaryExpression(kind, exprSyntax)
                       : SyntaxFactory.PrefixUnaryExpression(kind, exprSyntax);
        }
    }
}
