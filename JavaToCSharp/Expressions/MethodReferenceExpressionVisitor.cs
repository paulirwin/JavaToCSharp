using com.github.javaparser.ast;
using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions;

public class MethodReferenceExpressionVisitor : ExpressionVisitor<MethodReferenceExpr>
{
    protected override ExpressionSyntax Visit(ConversionContext context, MethodReferenceExpr expr)
    {
        var scope = expr.getScope();
        ExpressionSyntax? scopeSyntax = null;

        if (scope is not null)
        {
            scopeSyntax = VisitExpression(context, scope);
        }

        // A constructor reference (`Foo::new`) has no C# method-group equivalent, so it becomes a
        // lambda that news up the type instead. The scope is a type name here rather than a value,
        // so it is converted as a type to keep any generic arguments (`ArrayList<String>::new`).
        if (expr.getIdentifier() == "new")
        {
            var typeName = scope is TypeExpr typeExpr
                ? TypeHelper.ConvertType(typeExpr.getType().toString())
                : scopeSyntax?.ToString() ?? "object";

            return SyntaxFactory.ParenthesizedLambdaExpression(
                SyntaxFactory.ObjectCreationExpression(
                    SyntaxFactory.ParseTypeName(typeName),
                    SyntaxFactory.ArgumentList(),
                    initializer: null));
        }

        var methodName = TypeHelper.Capitalize(expr.getIdentifier());
        methodName = TypeHelper.ReplaceCommonMethodNames(methodName);

        var args = expr.getTypeArguments().FromOptional<NodeList>();

        SimpleNameSyntax nameSyntax = args is null || args.size() == 0
            ? SyntaxFactory.IdentifierName(methodName)
            : SyntaxFactory.GenericName(SyntaxFactory.Identifier(methodName))
                .WithTypeArgumentList(
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SeparatedList(
                            (args.ToList<com.github.javaparser.ast.type.Type>() ?? [])
                                .Select(x => SyntaxFactory.ParseTypeName(TypeHelper.ConvertType(x.toString()))))));

        // A method reference is a method group, not an invocation: `String::length` is `Length`,
        // not `Length()`. Wrapping it in an InvocationExpression would call the method here.
        if (scopeSyntax is null)
        {
            return nameSyntax;
        }

        return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, scopeSyntax, nameSyntax);
    }
}
