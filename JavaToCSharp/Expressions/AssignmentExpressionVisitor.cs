using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions
{
    public class AssignmentExpressionVisitor : ExpressionVisitor<AssignExpr>
    {
        public override ExpressionSyntax? Visit(ConversionContext context, AssignExpr assignExpr)
        {
            var left = assignExpr.getTarget();
            var leftSyntax = VisitExpression(context, left);
            if (leftSyntax is null)
            {
                return null;
            }

            var right = assignExpr.getValue();
            var rightSyntax = VisitExpression(context, right);
            if (rightSyntax is null)
            {
                return null;
            }

            var op = assignExpr.getOperator();
            var kind = SyntaxKind.None;

            if (op == AssignExpr.Operator.BINARY_AND)
                kind = SyntaxKind.AndAssignmentExpression;
            else if (op == AssignExpr.Operator.ASSIGN)
                kind = SyntaxKind.SimpleAssignmentExpression;
            else if (op == AssignExpr.Operator.LEFT_SHIFT)
                kind = SyntaxKind.LeftShiftAssignmentExpression;
            else if (op == AssignExpr.Operator.MINUS)
                kind = SyntaxKind.SubtractAssignmentExpression;
            else if (op == AssignExpr.Operator.BINARY_OR)
                kind = SyntaxKind.OrAssignmentExpression;
            else if (op == AssignExpr.Operator.PLUS)
                kind = SyntaxKind.AddAssignmentExpression;
            else if (op == AssignExpr.Operator.REMAINDER)
                kind = SyntaxKind.ModuloAssignmentExpression;
            else if (op == AssignExpr.Operator.SIGNED_RIGHT_SHIFT)
                kind = SyntaxKind.RightShiftAssignmentExpression;
            else if (op == AssignExpr.Operator.UNSIGNED_RIGHT_SHIFT)
                kind = SyntaxKind.UnsignedRightShiftAssignmentExpression;
            else if (op == AssignExpr.Operator.DIVIDE)
                kind = SyntaxKind.DivideAssignmentExpression;
            else if (op == AssignExpr.Operator.MULTIPLY)
                kind = SyntaxKind.MultiplyAssignmentExpression;
            else if (op == AssignExpr.Operator.XOR)
                kind = SyntaxKind.ExclusiveOrAssignmentExpression;
            
            return SyntaxFactory.AssignmentExpression(kind, leftSyntax, rightSyntax);
        }
    }
}
