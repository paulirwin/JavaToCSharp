using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions
{
	public class BinaryExpressionVisitor : ExpressionVisitor<BinaryExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, BinaryExpr binaryExpr)
        {
            var leftExpr = binaryExpr.getLeft();
            var leftSyntax = ExpressionVisitor.VisitExpression(context, leftExpr);

            var rightExpr = binaryExpr.getRight();
            var rightSyntax = ExpressionVisitor.VisitExpression(context, rightExpr);

            var op = binaryExpr.getOperator();
            SyntaxKind kind = SyntaxKind.None;

            if (op == BinaryExpr.Operator.and)
                kind = SyntaxKind.LogicalAndExpression;
            else if (op == BinaryExpr.Operator.binAnd)
                kind = SyntaxKind.BitwiseAndExpression;
            else if (op == BinaryExpr.Operator.binOr)
                kind = SyntaxKind.BitwiseOrExpression;
            else if (op == BinaryExpr.Operator.divide)
                kind = SyntaxKind.DivideExpression;
            else if (op == BinaryExpr.Operator.equals)
                kind = SyntaxKind.EqualsExpression;
            else if (op == BinaryExpr.Operator.greater)
                kind = SyntaxKind.GreaterThanExpression;
            else if (op == BinaryExpr.Operator.greaterEquals)
                kind = SyntaxKind.GreaterThanOrEqualExpression;
            else if (op == BinaryExpr.Operator.less)
                kind = SyntaxKind.LessThanExpression;
            else if (op == BinaryExpr.Operator.lessEquals)
                kind = SyntaxKind.LessThanOrEqualExpression;
            else if (op == BinaryExpr.Operator.lShift)
                kind = SyntaxKind.LeftShiftExpression;
            else if (op == BinaryExpr.Operator.minus)
                kind = SyntaxKind.SubtractExpression;
            else if (op == BinaryExpr.Operator.notEquals)
                kind = SyntaxKind.NotEqualsExpression;
            else if (op == BinaryExpr.Operator.or)
                kind = SyntaxKind.LogicalOrExpression;
            else if (op == BinaryExpr.Operator.plus)
                kind = SyntaxKind.AddExpression;
            else if (op == BinaryExpr.Operator.remainder)
                kind = SyntaxKind.ModuloExpression;
            else if (op == BinaryExpr.Operator.rSignedShift)
                kind = SyntaxKind.RightShiftExpression;
            else if (op == BinaryExpr.Operator.rUnsignedShift)
            {
                kind = SyntaxKind.RightShiftExpression;
                context.Options.Warning("Use of unsigned right shift in original code; verify correctness.", binaryExpr.getBegin().line);
            }
            else if (op == BinaryExpr.Operator.times)
                kind = SyntaxKind.MultiplyExpression;
            else if (op == BinaryExpr.Operator.xor)
                kind = SyntaxKind.ExclusiveOrExpression;

            return SyntaxFactory.BinaryExpression(kind, leftSyntax, rightSyntax);
        }
    }
}
