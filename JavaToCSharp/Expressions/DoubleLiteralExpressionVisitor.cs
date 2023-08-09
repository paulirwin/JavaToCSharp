using System;
using System.Globalization;
using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions;

public class DoubleLiteralExpressionVisitor : ExpressionVisitor<DoubleLiteralExpr>
{
    private readonly CultureInfo _cultureInfo;

    public DoubleLiteralExpressionVisitor()
    {
        _cultureInfo = (CultureInfo)CultureInfo.CurrentCulture.Clone();
        _cultureInfo.NumberFormat.CurrencyDecimalSeparator = ".";
    }

    public override ExpressionSyntax Visit(ConversionContext context, DoubleLiteralExpr expr)
    {
        var value = expr.getValue().Replace("_", String.Empty);
        
        var literalSyntax = value.EndsWith("f", StringComparison.OrdinalIgnoreCase) 
            ? SyntaxFactory.Literal(Single.Parse(value.TrimEnd('f', 'F'), NumberStyles.Any, _cultureInfo)) 
            : SyntaxFactory.Literal(Double.Parse(value.TrimEnd('d', 'D'), NumberStyles.Any, _cultureInfo));

        return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, literalSyntax);
    }
}
