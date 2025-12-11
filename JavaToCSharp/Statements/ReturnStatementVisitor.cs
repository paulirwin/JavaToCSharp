using com.github.javaparser.ast.expr;
using com.github.javaparser.ast.stmt;
using JavaToCSharp.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements;

public class ReturnStatementVisitor : StatementVisitor<ReturnStmt>
{
    public override StatementSyntax Visit(ConversionContext context, ReturnStmt returnStmt)
    {
        var expr = returnStmt.getExpression().FromOptional<Expression>();

        if (expr is null)
        {
            return SyntaxFactory.ReturnStatement(); // i.e. "return" in a void method
        }

        var exprSyntax = ExpressionVisitor.VisitExpression(context, expr);

        return SyntaxFactory.ReturnStatement(exprSyntax);
    }
}
