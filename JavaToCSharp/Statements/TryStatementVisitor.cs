using com.github.javaparser.ast.stmt;
using com.github.javaparser.ast.type;
using Roslyn.Compilers.CSharp;

namespace JavaToCSharp.Statements
{
	public class TryStatementVisitor : StatementVisitor<TryStmt>
    {
        public override StatementSyntax Visit(ConversionContext context, TryStmt tryStmt)
        {
            var tryBlock = tryStmt.getTryBlock();
            var tryStatements = tryBlock.getStmts().ToList<Statement>();

            var tryConverted = StatementVisitor.VisitStatements(context, tryStatements);

            var catches = tryStmt.getCatchs().ToList<CatchClause>();

            var trySyn = Syntax.TryStatement()
                .AddBlockStatements(tryConverted.ToArray());

            if (catches != null)
            {
                foreach (var ctch in catches)
                {
					//chaws
                    var referenceType = (ReferenceType)ctch.getParam().getType();
                    var block = ctch.getCatchBlock();
                    var catchStatements = block.getStmts().ToList<Statement>();
                    var catchConverted = StatementVisitor.VisitStatements(context, catchStatements);
                    var catchBlockSyntax = Syntax.Block(catchConverted);

                    var type = TypeHelper.ConvertType(referenceType.getType().ToString());

                    trySyn = trySyn.AddCatches(Syntax.CatchClause(Syntax.CatchDeclaration(Syntax.ParseTypeName(type), Syntax.ParseToken(ctch.getParam().getId().toString())), catchBlockSyntax));
                }
            }

            var finallyBlock = tryStmt.getFinallyBlock();

            if (finallyBlock != null)
            {
                var finallyStatements = finallyBlock.getStmts().ToList<Statement>();
                var finallyConverted = StatementVisitor.VisitStatements(context, finallyStatements);
                var finallyBlockSyntax = Syntax.Block(finallyConverted);

                trySyn = trySyn.WithFinally(Syntax.FinallyClause(finallyBlockSyntax));
            }

            return trySyn;
        }
    }
}
