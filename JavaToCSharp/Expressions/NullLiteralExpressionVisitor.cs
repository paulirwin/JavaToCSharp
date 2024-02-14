﻿using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions;

public class NullLiteralExpressionVisitor : ExpressionVisitor<NullLiteralExpr>
{
    protected override ExpressionSyntax Visit(ConversionContext context, NullLiteralExpr expr) =>
        SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
}
