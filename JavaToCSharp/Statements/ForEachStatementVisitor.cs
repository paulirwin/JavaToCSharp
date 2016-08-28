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
        public override StatementSyntax Visit(ConversionContext context, ForeachStmt foreachStmt)
        {
            var iterableExpr = foreachStmt.getIterable();
            var iterableSyntax = ExpressionVisitor.VisitExpression(context, iterableExpr);

            var varExpr = foreachStmt.getVariable();
            var type = TypeHelper.ConvertType(varExpr.getType().toString());

            var vars = varExpr.getVars()
                .ToList<VariableDeclarator>()
                .Select(i => SyntaxFactory.VariableDeclarator(i.toString()))
                .ToArray();

            var body = foreachStmt.getBody();
            var bodySyntax = StatementVisitor.VisitStatement(context, body);

            if (bodySyntax == null)
                return null;

            return SyntaxFactory.ForEachStatement(SyntaxFactory.ParseTypeName(type), vars[0].Identifier.ValueText, iterableSyntax, bodySyntax);
        }
    }
}
