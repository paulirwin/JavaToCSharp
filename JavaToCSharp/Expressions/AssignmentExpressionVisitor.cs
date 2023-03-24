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

            if (op == AssignExpr.Operator.and)
                kind = SyntaxKind.AndAssignmentExpression;
            else if (op == AssignExpr.Operator.assign)
                kind = SyntaxKind.SimpleAssignmentExpression;
            else if (op == AssignExpr.Operator.lShift)
                kind = SyntaxKind.LeftShiftAssignmentExpression;
            else if (op == AssignExpr.Operator.minus)
                kind = SyntaxKind.SubtractAssignmentExpression;
            else if (op == AssignExpr.Operator.or)
                kind = SyntaxKind.OrAssignmentExpression;
            else if (op == AssignExpr.Operator.plus)
                kind = SyntaxKind.AddAssignmentExpression;
            else if (op == AssignExpr.Operator.rem)
                kind = SyntaxKind.ModuloAssignmentExpression;
            else if (op == AssignExpr.Operator.rSignedShift)
                kind = SyntaxKind.RightShiftAssignmentExpression;
            else if (op == AssignExpr.Operator.rUnsignedShift)
                kind = SyntaxKind.UnsignedRightShiftAssignmentExpression;
            else if (op == AssignExpr.Operator.slash)
                kind = SyntaxKind.DivideAssignmentExpression;
            else if (op == AssignExpr.Operator.star)
                kind = SyntaxKind.MultiplyAssignmentExpression;
            else if (op == AssignExpr.Operator.xor)
                kind = SyntaxKind.ExclusiveOrAssignmentExpression;
            
            return SyntaxFactory.AssignmentExpression(kind, leftSyntax, rightSyntax);
        }
    }
}
