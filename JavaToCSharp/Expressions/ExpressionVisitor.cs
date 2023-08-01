using System;
using System.Collections.Generic;
using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions;

public abstract class ExpressionVisitor<T> : ExpressionVisitor
    where T : Expression
{
    public abstract ExpressionSyntax? Visit(ConversionContext context, T expr);

    protected sealed override ExpressionSyntax? Visit(ConversionContext context, Expression expr) => Visit(context, (T)expr);
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
            { typeof(ArrayInitializerExpr), new ArrayInitializerExpressionVisitor() },
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
            { typeof(LongLiteralExpr), new LongLiteralExpressionVisitor() },
            { typeof(LambdaExpr), new LambdaExpressionVisitor()  },
            { typeof(MethodReferenceExpr), new MethodReferenceExpressionVisitor()  },
            { typeof(TypeExpr), new TypeExpressionVisitor()   }
        };
    }

    protected abstract ExpressionSyntax? Visit(ConversionContext context, Expression expr);

    public static ExpressionSyntax? VisitExpression(ConversionContext context, Expression? expr)
    {
        if (expr == null)
            return null;

        ExpressionVisitor? visitor = null;
        var t = expr.GetType();

        while (t != null && !_visitors.TryGetValue(t, out visitor))
        {
            t = t.BaseType;
        }

        if (visitor != null) 
            return visitor.Visit(context, expr);

        var message = $"Expression visitor not implemented for expression type `{expr.GetType()}`{Environment.NewLine}{expr}";
        throw new NotImplementedException(message);
    }
}
