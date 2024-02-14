using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions;

public class TypeExpressionVisitor : ExpressionVisitor<TypeExpr>
{
    protected override ExpressionSyntax Visit(ConversionContext context, TypeExpr expr) => SyntaxFactory.ParseTypeName(TypeHelper.ConvertTypeOf(expr));
}
