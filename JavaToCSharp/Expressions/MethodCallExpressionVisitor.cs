using com.github.javaparser.ast.expr;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavaToCSharp.Expressions
{
    public class MethodCallExpressionVisitor : ExpressionVisitor<MethodCallExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, MethodCallExpr methodCallExpr)
        {
            var scope = methodCallExpr.getScope();
            ExpressionSyntax scopeSyntax = null;

            if (scope != null)
            {
                scopeSyntax = ExpressionVisitor.VisitExpression(context, scope);
            }

            var methodName = TypeHelper.Capitalize(methodCallExpr.getName());
            methodName = TypeHelper.ReplaceCommonMethodNames(methodName);

            ExpressionSyntax methodExpression;

            if (scopeSyntax == null)
            {
                methodExpression = Syntax.IdentifierName(methodName);
            }
            else
            {
                methodExpression = Syntax.MemberAccessExpression(SyntaxKind.MemberAccessExpression, scopeSyntax, Syntax.IdentifierName(methodName));
            }

            var args = methodCallExpr.getArgs().ToList<Expression>();

            if (args == null || args.Count == 0)
                return Syntax.InvocationExpression(methodExpression);

            var argSyntaxes = new List<ArgumentSyntax>();

            foreach (var arg in args)
            {
                var argSyntax = ExpressionVisitor.VisitExpression(context, arg);
                argSyntaxes.Add(Syntax.Argument(argSyntax));
            }

            return Syntax.InvocationExpression(methodExpression, Syntax.ArgumentList(Syntax.SeparatedList(argSyntaxes, Enumerable.Repeat(Syntax.Token(SyntaxKind.CommaToken), argSyntaxes.Count - 1))));
        }
    }
}
