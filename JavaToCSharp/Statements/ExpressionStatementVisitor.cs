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
        public override StatementSyntax? Visit(ConversionContext context, ExpressionStmt exprStmt)
        {
            var expression = exprStmt.getExpression();

            // handle special case where AST is different
            if (expression is VariableDeclarationExpr expr)
                return VisitVariableDeclarationStatement(context, expr);

            var expressionSyntax = ExpressionVisitor.VisitExpression(context, expression);
            if (expressionSyntax is null)
            {
                return null;
            }

            return SyntaxFactory.ExpressionStatement(expressionSyntax);
        }

        private static StatementSyntax VisitVariableDeclarationStatement(ConversionContext context, VariableDeclarationExpr varExpr)
        {
            var type = TypeHelper.ConvertType(varExpr.getCommonType());

            var variables = new List<VariableDeclaratorSyntax>();

            var variableDeclarators = varExpr.getVariables()?.ToList<VariableDeclarator>() ?? new List<VariableDeclarator>();
            foreach (var item in variableDeclarators)
            {
                var id = item.getType();
                string name = item.getNameAsString();

                if (id.getArrayLevel() > 0)
                {
                    if (!type.EndsWith("[]"))
                        type += "[]";
                    if (name.EndsWith("[]"))
                        name = name.Substring(0, name.Length - 2);
                }

                var initExpr = item.getInitializer().FromOptional<Expression>();

                if (initExpr != null)
                {
                    var initSyntax = ExpressionVisitor.VisitExpression(context, initExpr);
                    if (initSyntax is not null)
                    {
                        var varDeclarationSyntax = SyntaxFactory.VariableDeclarator(name).WithInitializer(SyntaxFactory.EqualsValueClause(initSyntax));
                        variables.Add(varDeclarationSyntax);
                    }
                }
                else
                    variables.Add(SyntaxFactory.VariableDeclarator(name));
            }

            return SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(type), SyntaxFactory.SeparatedList(variables, Enumerable.Repeat(SyntaxFactory.Token(SyntaxKind.CommaToken), variables.Count - 1))));
        }
    }
}
