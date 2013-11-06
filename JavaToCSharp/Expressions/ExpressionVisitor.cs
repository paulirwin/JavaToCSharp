using japa.parser.ast.expr;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavaToCSharp.Expressions
{
    public abstract class ExpressionVisitor<T> : ExpressionVisitor
        where T : Expression
    {
        public abstract ExpressionSyntax Visit(ConversionContext context, T expr);

        protected sealed override ExpressionSyntax Visit(ConversionContext context, Expression expr)
        {
            return Visit(context, (T)expr);
        }
    }

    public abstract class ExpressionVisitor
    {
        private static readonly IDictionary<Type, ExpressionVisitor> _visitors;

        static ExpressionVisitor()
        {
            _visitors = new Dictionary<Type, ExpressionVisitor>
            {
                { typeof(ArrayAccessExpr), new ArrayAccessExpressionVisitor() },
                { typeof(ArrayCreationExpr), new ArrayCreationExpressionVisitor() },
                { typeof(AssignExpr), new AssignmentExpressionVisitor() },
                { typeof(BinaryExpr), new BinaryExpressionVisitor() },
                { typeof(BooleanLiteralExpr), new BooleanLiteralExpressionVisitor() },
                { typeof(CastExpr), new CastExpressionVisitor() },
                { typeof(CharLiteralExpr), new CharLiteralExpressionVisitor() },
                { typeof(ClassExpr), new ClassExpressionVisitor() },
                { typeof(ConditionalExpr), new ConditionalExpressionVisitor() },
                { typeof(DoubleLiteralExpr), new DoubleLiteralExpressionVisitor() },
                { typeof(EnclosedExpr), new EnclosedExpressionVisitor() },
                { typeof(FieldAccessExpr), new FieldAccessExpressionVisitor() },
                { typeof(InstanceOfExpr), new InstanceOfExpressionVisitor() },
                { typeof(IntegerLiteralExpr), new IntegerLiteralExpressionVisitor() },
                { typeof(MethodCallExpr), new MethodCallExpressionVisitor() },
                { typeof(NameExpr), new NameExpressionVisitor() },
                { typeof(NullLiteralExpr), new NullLiteralExpressionVisitor() },
                { typeof(ObjectCreationExpr), new ObjectCreationExpressionVisitor() },
                { typeof(StringLiteralExpr), new StringLiteralExpressionVisitor() },
                { typeof(SuperExpr), new SuperExpressionVisitor() },
                { typeof(ThisExpr), new ThisExpressionVisitor() },
                { typeof(UnaryExpr), new UnaryExpressionVisitor() },
            };
        }

        protected abstract ExpressionSyntax Visit(ConversionContext context, Expression expr);

        public static ExpressionSyntax VisitExpression(ConversionContext context, Expression expr)
        {
            if (expr == null)
                return null;

            ExpressionVisitor visitor = null;
            Type t = expr.GetType();

            while (t != null && !_visitors.TryGetValue(t, out visitor))
            {
                t = t.BaseType;
            }

            if (visitor == null)
                throw new NotImplementedException("Expression visitor not implemented for expression type " + expr.GetType().Name);

            return visitor.Visit(context, expr);
        }
    }
}
