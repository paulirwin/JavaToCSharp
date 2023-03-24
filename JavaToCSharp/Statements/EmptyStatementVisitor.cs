using com.github.javaparser.ast.stmt;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements
{
    public class EmptyStatementVisitor : StatementVisitor<EmptyStmt>
    {
        public override StatementSyntax? Visit(ConversionContext context, EmptyStmt statement) => null;
    }
}