using System.Collections.Generic;
using System.Linq;
using com.github.javaparser.ast.body;
using com.github.javaparser.ast.expr;
using com.github.javaparser.ast.stmt;
using JavaToCSharp.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements
{
    public class ExpressionStatementVisitor : StatementVisitor<ExpressionStmt>
    {
        public override StatementSyntax Visit(ConversionContext context, ExpressionStmt exprStmt)
        {
            var expression = exprStmt.getExpression();

            // handle special case where AST is different
            if (expression is VariableDeclarationExpr)
                return VisitVariableDeclarationStatement(context, (VariableDeclarationExpr)expression);

            var expressionSyntax = ExpressionVisitor.VisitExpression(context, expression);

            return SyntaxFactory.ExpressionStatement(expressionSyntax);
        }

        private static StatementSyntax VisitVariableDeclarationStatement(ConversionContext context, VariableDeclarationExpr varExpr)
        {
            var type = TypeHelper.ConvertType(varExpr.getType().toString());

            var variables = new List<VariableDeclaratorSyntax>();

            foreach (var item in varExpr.getVars().ToList<VariableDeclarator>())
            {
                var id = item.getId();
                string name = id.getName();

                if (id.getArrayCount() > 0)
                {
                    if (!type.EndsWith("[]"))
                        type += "[]";
                    if (name.EndsWith("[]"))
                        name = name.Substring(0, name.Length - 2);
                }

                var initexpr = item.getInit();

                if (initexpr != null)
                {
                    var initsyn = ExpressionVisitor.VisitExpression(context, initexpr);
                    var vardeclsyn = SyntaxFactory.VariableDeclarator(name).WithInitializer(SyntaxFactory.EqualsValueClause(initsyn));
                    variables.Add(vardeclsyn);
                }
                else
                    variables.Add(SyntaxFactory.VariableDeclarator(name));
            }

            return SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(type), SyntaxFactory.SeparatedList(variables, Enumerable.Repeat(SyntaxFactory.Token(SyntaxKind.CommaToken), variables.Count - 1))));
        }
    }
}
