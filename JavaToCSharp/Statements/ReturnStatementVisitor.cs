using com.github.javaparser;
using com.github.javaparser.ast.body;
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

        if (expr is SwitchExpr switchExpr && SwitchExpressionLowering.RequiresLowering(switchExpr))
        {
            // C# switch expressions cannot contain multiple statements per arm. Lower into a switch
            // statement over a temporary, then return that temporary.
            context.Options.Warning(
                "Multi-statement switch expression converted to a switch statement. Review the generated code carefully.",
                switchExpr.getBegin().FromRequiredOptional<Position>().line);

            var temp = context.CreateUniqueLocalName("switchResult");

            // `var` needs an initializer, so use the enclosing method's declared return type.
            var methodDecl = FindEnclosingMethod(returnStmt)
                             ?? throw new InvalidOperationException(
                                 "Multi-statement switch expression in a return statement outside of a method is not supported");

            var returnTypeNode = methodDecl.getType();
            var tempType = TypeHelper.ConvertTypeSyntax(returnTypeNode, returnTypeNode.getArrayLevel());

            context.PendingStatements.Add(
                SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        tempType,
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(temp)))));

            context.PendingStatements.Add(SwitchExpressionLowering.Lower(context, switchExpr, temp));

            return SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(temp));
        }

        var exprSyntax = ExpressionVisitor.VisitExpression(context, expr);

        return SyntaxFactory.ReturnStatement(exprSyntax);
    }

    private static MethodDeclaration? FindEnclosingMethod(com.github.javaparser.ast.Node node)
    {
        var current = node.getParentNode().FromOptional<com.github.javaparser.ast.Node>();

        while (current is not null)
        {
            if (current is MethodDeclaration methodDecl)
            {
                return methodDecl;
            }

            current = current.getParentNode().FromOptional<com.github.javaparser.ast.Node>();
        }

        return null;
    }
}
