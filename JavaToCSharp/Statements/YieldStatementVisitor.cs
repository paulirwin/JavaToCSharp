using com.github.javaparser.ast.expr;
using com.github.javaparser.ast.stmt;
using JavaToCSharp.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements;

/// <summary>
/// Converts a Java <c>yield</c> statement. Java's <c>yield</c> produces a value from a switch
/// expression arm; the C# equivalent depends on how the enclosing switch expression was lowered,
/// which is communicated via <see cref="ConversionContext.YieldTarget"/>.
/// </summary>
public class YieldStatementVisitor : StatementVisitor<YieldStmt>
{
    public override StatementSyntax Visit(ConversionContext context, YieldStmt yieldStmt)
    {
        var expr = yieldStmt.getExpression();
        var exprSyntax = ExpressionVisitor.VisitExpression(context, expr)
                         ?? throw new InvalidOperationException("Yield statement must contain an expression");

        var target = context.YieldTarget;

        if (target is null)
        {
            // Not inside a lowered switch expression; nothing sensible to translate to.
            throw new InvalidOperationException("Yield statement encountered outside of a switch expression");
        }

        // Assign to the temporary that receives the switch expression's value, then leave the section.
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(target),
                exprSyntax));
    }
}
