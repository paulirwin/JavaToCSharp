using japa.parser.ast.stmt;
using JavaToCSharp.Expressions;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavaToCSharp.Statements
{
    public class AssertStatementVisitor : StatementVisitor<AssertStmt>
    {
        public override StatementSyntax Visit(ConversionContext context, AssertStmt assertStmt)
        {
            if (!context.Options.UseDebugAssertForAsserts)
                return null;

            var check = assertStmt.getCheck();
            var checkSyntax = ExpressionVisitor.VisitExpression(context, check);

            var message = assertStmt.getMessage();

            if (message == null)
                return Syntax.ExpressionStatement(
                    Syntax.InvocationExpression(
                        Syntax.IdentifierName("Debug.Assert"),
                        Syntax.ArgumentList(Syntax.SeparatedList(Syntax.Argument(checkSyntax)))));

            var messageSyntax = ExpressionVisitor.VisitExpression(context, message);

            return Syntax.ExpressionStatement(
                    Syntax.InvocationExpression(
                        Syntax.IdentifierName("Debug.Assert"),
                        Syntax.ArgumentList(Syntax.SeparatedList(new[] { Syntax.Argument(checkSyntax), Syntax.Argument(messageSyntax) }, new[] { Syntax.Token(SyntaxKind.CommaToken) }))));
        }
    }
}
