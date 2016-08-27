using com.github.javaparser.ast.body;
using com.github.javaparser.ast.stmt;
using JavaToCSharp.Expressions;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                .Select(i => Syntax.VariableDeclarator(i.toString()))
                .ToArray();

            var body = foreachStmt.getBody();
            var bodySyntax = StatementVisitor.VisitStatement(context, body);

            if (bodySyntax == null)
                return null;

            return Syntax.ForEachStatement(Syntax.ParseTypeName(type), vars[0].Identifier.ValueText, iterableSyntax, bodySyntax);
        }
    }
}
