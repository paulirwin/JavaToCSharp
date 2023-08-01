using com.github.javaparser.ast.stmt;
using JavaToCSharp.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements;

public class AssertStatementVisitor : StatementVisitor<AssertStmt>
{
    public override StatementSyntax? Visit(ConversionContext context, AssertStmt assertStmt)
    {
        if (!context.Options.UseDebugAssertForAsserts)
            return null;

        var check = assertStmt.getCheck();
        var checkSyntax = ExpressionVisitor.VisitExpression(context, check);
        if (checkSyntax is null)
        {
            return null;
        }

        var message = assertStmt.getMessage();

        if (message == null)
            return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName("Debug.Assert"),
                    SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Argument(checkSyntax)
                    }))));

        var messageSyntax = ExpressionVisitor.VisitExpression(context, message);
        if (messageSyntax is null)
        {
            return null;
        }

        return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName("Debug.Assert"),
                    SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[] { SyntaxFactory.Argument(checkSyntax), SyntaxFactory.Argument(messageSyntax) }, new[] { SyntaxFactory.Token(SyntaxKind.CommaToken) }))));
    }
}
