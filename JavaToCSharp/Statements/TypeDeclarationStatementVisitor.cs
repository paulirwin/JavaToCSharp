using com.github.javaparser.ast.stmt;
using JavaToCSharp.Declarations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements
{
    public class TypeDeclarationStatementVisitor : StatementVisitor<LocalClassDeclarationStmt>
    {
        public override StatementSyntax? Visit(ConversionContext context, LocalClassDeclarationStmt statement)
        {
            var typeDeclaration = statement.getClassDeclaration();
            var classSyntax = ClassOrInterfaceDeclarationVisitor.VisitClassDeclaration(context, typeDeclaration);
            var text = classSyntax?.NormalizeWhitespace().GetText().ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }
            
            return SyntaxFactory.ParseStatement(text);
        }
    }
}
