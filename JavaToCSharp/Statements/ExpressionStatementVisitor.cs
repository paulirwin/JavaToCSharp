using japa.parser.ast.body;
using japa.parser.ast.expr;
using japa.parser.ast.stmt;
using JavaToCSharp.Expressions;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            return Syntax.ExpressionStatement(expressionSyntax);
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
                    var vardeclsyn = Syntax.VariableDeclarator(name).WithInitializer(Syntax.EqualsValueClause(initsyn));
                    variables.Add(vardeclsyn);
                }
                else
                    variables.Add(Syntax.VariableDeclarator(name));
            }

            return Syntax.LocalDeclarationStatement(
                Syntax.VariableDeclaration(Syntax.ParseTypeName(type), Syntax.SeparatedList(variables, Enumerable.Repeat(Syntax.Token(SyntaxKind.CommaToken), variables.Count - 1))));
        }
    }
}
