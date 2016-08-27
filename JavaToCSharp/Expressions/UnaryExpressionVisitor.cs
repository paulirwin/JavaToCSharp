using com.github.javaparser.ast.expr;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                kind = SyntaxKind.NegateExpression;
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
                kind = SyntaxKind.PlusExpression;
            else if (op == UnaryExpr.Operator.preDecrement)
                kind = SyntaxKind.PreDecrementExpression;
            else if (op == UnaryExpr.Operator.preIncrement)
                kind = SyntaxKind.PreIncrementExpression;

            if (isPostfix)
                return Syntax.PostfixUnaryExpression(kind, exprSyntax);
            else
                return Syntax.PrefixUnaryExpression(kind, exprSyntax);
        }
    }
}
