using com.github.javaparser.ast.body;
using com.github.javaparser.ast.expr;
using JavaToCSharp.Declarations;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavaToCSharp.Expressions
{
    public class ObjectCreationExpressionVisitor : ExpressionVisitor<ObjectCreationExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, ObjectCreationExpr newExpr)
        {
            var anonBody = newExpr.getAnonymousClassBody().ToList<BodyDeclaration>();

            if (anonBody != null && anonBody.Count > 0)
            {
                return VisitAnonymousClassCreationExpression(context, newExpr, anonBody);
            }

            var scope = newExpr.getScope();
            ExpressionSyntax scopeSyntax = null;

            if (scope != null)
            {
                scopeSyntax = ExpressionVisitor.VisitExpression(context, scope);
            }

            // TODO: what to do with scope?

            var type = newExpr.getType();

            var typeSyntax = TypeHelper.GetSyntaxFromType(type);

            var args = newExpr.getArgs().ToList<Expression>();

            if (args == null || args.Count == 0)
                return Syntax.ObjectCreationExpression(typeSyntax).WithArgumentList(Syntax.ArgumentList());

            var argSyntaxes = new List<ArgumentSyntax>();

            foreach (var arg in args)
            {
                var argSyntax = ExpressionVisitor.VisitExpression(context, arg);
                argSyntaxes.Add(Syntax.Argument(argSyntax));
            }

            return Syntax.ObjectCreationExpression(typeSyntax, Syntax.ArgumentList(Syntax.SeparatedList(argSyntaxes, Enumerable.Repeat(Syntax.Token(SyntaxKind.CommaToken), argSyntaxes.Count - 1))), null);
        }

        private static ExpressionSyntax VisitAnonymousClassCreationExpression(ConversionContext context, ObjectCreationExpr newExpr, List<BodyDeclaration> anonBody)
        {
            string baseTypeName = TypeHelper.ConvertType(newExpr.getType().getName());
            string anonTypeName = string.Empty;

            for (int i = 0; i <= 100; i++)
            {
                if (i == 100)
                    throw new InvalidOperationException("Too many anonymous types");

                anonTypeName = string.Format("Anonymous{0}{1}", baseTypeName, i == 0 ? string.Empty : i.ToString());

                if (!context.UsedAnonymousTypeNames.Contains(anonTypeName))
                {
                    context.UsedAnonymousTypeNames.Add(anonTypeName);
                    break; // go with this one
                }
            }

            var classSyntax = Syntax.ClassDeclaration(anonTypeName)
                .AddModifiers(
                    Syntax.Token(SyntaxKind.PrivateKeyword),
                    Syntax.Token(SyntaxKind.SealedKeyword))
                .WithBaseList(Syntax.BaseList(Syntax.SeparatedList(Syntax.ParseTypeName(baseTypeName))));

            var parentField = Syntax.FieldDeclaration(
                Syntax.VariableDeclaration(Syntax.ParseTypeName(context.LastTypeName)).AddVariables(Syntax.VariableDeclarator("parent")))
                .AddModifiers(Syntax.Token(SyntaxKind.PrivateKeyword), Syntax.Token(SyntaxKind.ReadOnlyKeyword));

            var ctorSyntax = Syntax.ConstructorDeclaration(anonTypeName)
                .AddModifiers(Syntax.Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(Syntax.Parameter(Syntax.ParseToken("parent")).WithType(Syntax.ParseTypeName(context.LastTypeName)))
                .AddBodyStatements(Syntax.ExpressionStatement(Syntax.BinaryExpression(SyntaxKind.AssignExpression, Syntax.MemberAccessExpression(SyntaxKind.MemberAccessExpression, Syntax.ThisExpression(), Syntax.IdentifierName("parent")), Syntax.IdentifierName("parent"))));

            classSyntax = classSyntax.AddMembers(ctorSyntax, parentField);

            foreach (var member in anonBody)
            {
                var memberSyntax = BodyDeclarationVisitor.VisitBodyDeclarationForClass(context, classSyntax, member);
                classSyntax = classSyntax.AddMembers(memberSyntax);
            }

            context.PendingAnonymousTypes.Enqueue(classSyntax);

            var args = newExpr.getArgs().ToList<Expression>();

            if (args == null || args.Count == 0)
                return Syntax.ObjectCreationExpression(Syntax.ParseTypeName(anonTypeName))
                    .AddArgumentListArguments(Syntax.Argument(Syntax.ThisExpression()));

            var argSyntaxes = new List<ArgumentSyntax>();

            argSyntaxes.Add(Syntax.Argument(Syntax.ThisExpression()));

            foreach (var arg in args)
            {
                var argSyntax = ExpressionVisitor.VisitExpression(context, arg);
                argSyntaxes.Add(Syntax.Argument(argSyntax));
            }

            return Syntax.ObjectCreationExpression(Syntax.ParseTypeName(anonTypeName), Syntax.ArgumentList(Syntax.SeparatedList(argSyntaxes, Enumerable.Repeat(Syntax.Token(SyntaxKind.CommaToken), argSyntaxes.Count - 1))), null);
        }
    }
}
