using System.Collections.Generic;
using System.Linq;
using com.github.javaparser.ast.body;
using com.github.javaparser.ast.stmt;
using JavaToCSharp.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements
{
    public class ForEachStatementVisitor : StatementVisitor<ForeachStmt>
    {
        public override StatementSyntax? Visit(ConversionContext context, ForeachStmt foreachStmt)
        {
            var iterableExpr = foreachStmt.getIterable();
            var iterableSyntax = ExpressionVisitor.VisitExpression(context, iterableExpr);
            if (iterableSyntax is null)
            {
                return null;
            }

            var varExpr = foreachStmt.getVariable();
            var type = TypeHelper.ConvertTypeOf(varExpr);

            var variableDeclarators = varExpr.getVars()?.ToList<VariableDeclarator>()?? new List<VariableDeclarator>();
            var vars = variableDeclarators
                       .Select(i => SyntaxFactory.VariableDeclarator(i.toString()))
                       .ToArray();

            var body = foreachStmt.getBody();
            var bodySyntax = VisitStatement(context, body);

            return bodySyntax == null ? null : SyntaxFactory.ForEachStatement(SyntaxFactory.ParseTypeName(type), vars[0].Identifier.ValueText, iterableSyntax, bodySyntax);
        }
    }
}
