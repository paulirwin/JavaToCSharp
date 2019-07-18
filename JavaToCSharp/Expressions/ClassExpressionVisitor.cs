﻿using com.github.javaparser.ast.expr;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Expressions
{
    public class ClassExpressionVisitor : ExpressionVisitor<ClassExpr>
    {
        public override ExpressionSyntax Visit(ConversionContext context, ClassExpr expr)
        {
            var type = TypeHelper.ConvertType(expr.getType().toString());

            return SyntaxFactory.TypeOfExpression(SyntaxFactory.ParseTypeName(type));
        }
    }
}