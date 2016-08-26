using com.github.javaparser.ast.expr;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavaToCSharp.Expressions
{
    public class AssignmentExpressionVisitor : ExpressionVisitor<AssignExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, AssignExpr assignExpr)
        {
            var left = assignExpr.getTarget();
            var leftSyntax = ExpressionVisitor.VisitExpression(context, left);

            var right = assignExpr.getValue();
            var rightSyntax = ExpressionVisitor.VisitExpression(context, right);

            var op = assignExpr.getOperator();
            var kind = SyntaxKind.None;

            if (op == AssignExpr.Operator.and)
                kind = SyntaxKind.AndAssignExpression;
            else if (op == AssignExpr.Operator.assign)
                kind = SyntaxKind.AssignExpression;
            else if (op == AssignExpr.Operator.lShift)
                kind = SyntaxKind.LeftShiftAssignExpression;
            else if (op == AssignExpr.Operator.minus)
                kind = SyntaxKind.SubtractAssignExpression;
            else if (op == AssignExpr.Operator.or)
                kind = SyntaxKind.OrAssignExpression;
            else if (op == AssignExpr.Operator.plus)
                kind = SyntaxKind.AddAssignExpression;
            else if (op == AssignExpr.Operator.rem)
                kind = SyntaxKind.ModuloAssignExpression;
            else if (op == AssignExpr.Operator.rSignedShift)
                kind = SyntaxKind.RightShiftAssignExpression;
            else if (op == AssignExpr.Operator.rUnsignedShift)
            {
                context.Options.Warning("Use of unsigned right shift assignment. Using signed right shift assignment instead. Check for correctness.", assignExpr.getBeginLine());
                kind = SyntaxKind.RightShiftAssignExpression;
            }
            else if (op == AssignExpr.Operator.slash)
                kind = SyntaxKind.DivideAssignExpression;
            else if (op == AssignExpr.Operator.star)
                kind = SyntaxKind.MultiplyAssignExpression;
            else if (op == AssignExpr.Operator.xor)
                kind = SyntaxKind.ExclusiveOrAssignExpression;

            return Syntax.BinaryExpression(kind, leftSyntax, rightSyntax);
        }
    }
}
