using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions;

public class SuperExpressionVisitor : ExpressionVisitor<SuperExpr>
{
    public override ExpressionSyntax Visit(ConversionContext context, SuperExpr expr) => SyntaxFactory.BaseExpression();
}
