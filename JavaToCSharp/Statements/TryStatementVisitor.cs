using System.Collections.Generic;
using com.github.javaparser.ast.stmt;
using com.github.javaparser.ast.type;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements
{
    public class TryStatementVisitor : StatementVisitor<TryStmt>
    {
        public override StatementSyntax Visit(ConversionContext context, TryStmt tryStmt)
        {
            var tryBlock = tryStmt.getTryBlock();
            var tryStatements = tryBlock.getStatements().ToList<Statement>();

            var tryConverted = VisitStatements(context, tryStatements);

            var catches = tryStmt.getCatchClauses().ToList<CatchClause>();

            var trySyn = SyntaxFactory.TryStatement()
                .AddBlockStatements(tryConverted.ToArray());

            if (catches != null)
            {
                foreach (var catchClause in catches)
                {
                    var paramType = catchClause.getParameter().getType();
                    if (paramType is UnionType)
                    {
                        var nodes = paramType.getChildNodes()?.ToList<ReferenceType>() ?? new List<ReferenceType>();
                        foreach (var node in nodes)
                        {
                            var referenceTypeName = node.getElementType().ToString();
                            trySyn = AddCatches(context, catchClause, referenceTypeName, trySyn);
                        }
                    }
                    else
                    {
                        var referenceType = (ReferenceType)catchClause.getParameter().getType();
                        trySyn = AddCatches(context, catchClause, referenceType.getElementType().ToString(), trySyn);
                    }
                }
            }

            var finallyBlock = tryStmt.getFinallyBlock().FromOptional<BlockStmt>();

            if (finallyBlock == null)
                return trySyn;

            var finallyStatements = finallyBlock.getStatements().ToList<Statement>();
            var finallyConverted = VisitStatements(context, finallyStatements);
            var finallyBlockSyntax = SyntaxFactory.Block(finallyConverted);

            trySyn = trySyn.WithFinally(SyntaxFactory.FinallyClause(finallyBlockSyntax));

            return trySyn;
        }

        private static TryStatementSyntax AddCatches(ConversionContext context, CatchClause ctch,
            string typeName, TryStatementSyntax trySyn)
        {
            var block = ctch.getBody();
            var catchStatements = block.getStatements().ToList<Statement>();
            var catchConverted = VisitStatements(context, catchStatements);
            var catchBlockSyntax = SyntaxFactory.Block(catchConverted);

            var type = TypeHelper.ConvertType(typeName);

            trySyn = trySyn.AddCatches(
                SyntaxFactory.CatchClause(
                    SyntaxFactory.CatchDeclaration(
                        SyntaxFactory.ParseTypeName(type),
                        SyntaxFactory.ParseToken(ctch.getParameter().getType().asString())
                    ),
                    filter: null,
                    block: catchBlockSyntax
                )
            );

            return trySyn;
        }
    }
}
