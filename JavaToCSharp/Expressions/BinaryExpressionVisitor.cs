using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions
{
    /// <summary>
    /// It's possible to have StackOverflow exception here, especially for the long long strings.
    /// In this case just compile the code in release mode and run it outside of the debugger.
    /// </summary>
    public class BinaryExpressionVisitor : ExpressionVisitor<BinaryExpr>
    {
        public override ExpressionSyntax? Visit(ConversionContext context, BinaryExpr binaryExpr)
        {
            var leftExpr = binaryExpr.getLeft();
            if (leftExpr is null)
            {
                return null;
            }
            
            var leftSyntax = VisitExpression(context, leftExpr);
            if (leftSyntax is null)
            {
                return null;
            }

            var rightExpr = binaryExpr.getRight();
            if (rightExpr is null)
            {
                return null;
            }
            
            var rightSyntax = VisitExpression(context, rightExpr);
            if (rightSyntax is null)
            {
                return null;
            }

            var op = binaryExpr.getOperator();
            var kind = SyntaxKind.None;

            if (op == BinaryExpr.Operator.AND)
                kind = SyntaxKind.LogicalAndExpression;
            else if (op == BinaryExpr.Operator.BINARY_AND)
                kind = SyntaxKind.BitwiseAndExpression;
            else if (op == BinaryExpr.Operator.BINARY_OR)
                kind = SyntaxKind.BitwiseOrExpression;
            else if (op == BinaryExpr.Operator.DIVIDE)
                kind = SyntaxKind.DivideExpression;
            else if (op == BinaryExpr.Operator.EQUALS)
                kind = SyntaxKind.EqualsExpression;
            else if (op == BinaryExpr.Operator.GREATER)
                kind = SyntaxKind.GreaterThanExpression;
            else if (op == BinaryExpr.Operator.GREATER_EQUALS)
                kind = SyntaxKind.GreaterThanOrEqualExpression;
            else if (op == BinaryExpr.Operator.LESS)
                kind = SyntaxKind.LessThanExpression;
            else if (op == BinaryExpr.Operator.LESS_EQUALS)
                kind = SyntaxKind.LessThanOrEqualExpression;
            else if (op == BinaryExpr.Operator.LEFT_SHIFT)
                kind = SyntaxKind.LeftShiftExpression;
            else if (op == BinaryExpr.Operator.MINUS)
                kind = SyntaxKind.SubtractExpression;
            else if (op == BinaryExpr.Operator.NOT_EQUALS)
                kind = SyntaxKind.NotEqualsExpression;
            else if (op == BinaryExpr.Operator.OR)
                kind = SyntaxKind.LogicalOrExpression;
            else if (op == BinaryExpr.Operator.PLUS)
                kind = SyntaxKind.AddExpression;
            else if (op == BinaryExpr.Operator.REMAINDER)
                kind = SyntaxKind.ModuloExpression;
            else if (op == BinaryExpr.Operator.SIGNED_RIGHT_SHIFT)
                kind = SyntaxKind.RightShiftExpression;
            else if (op == BinaryExpr.Operator.UNSIGNED_RIGHT_SHIFT)
                kind = SyntaxKind.UnsignedRightShiftExpression;
            else if (op == BinaryExpr.Operator.MULTIPLY)
                kind = SyntaxKind.MultiplyExpression;
            else if (op == BinaryExpr.Operator.XOR)
                kind = SyntaxKind.ExclusiveOrExpression;

            return SyntaxFactory.BinaryExpression(kind, leftSyntax, rightSyntax);
        }
    }
}
