using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions
{
    public class NameExpressionVisitor : ExpressionVisitor<NameExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, NameExpr nameExpr)
        {
            return SyntaxFactory.IdentifierName(TypeHelper.ConvertIdentifierName(nameExpr.getName()));
        }
    }
}
