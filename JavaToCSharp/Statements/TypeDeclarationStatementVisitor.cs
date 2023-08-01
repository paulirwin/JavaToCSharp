using com.github.javaparser.ast.body;
using com.github.javaparser.ast.stmt;
using JavaToCSharp.Declarations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements;

public class TypeDeclarationStatementVisitor : StatementVisitor<TypeDeclarationStmt>
{
    public override StatementSyntax? Visit(ConversionContext context, TypeDeclarationStmt statement)
    {
        var typeDeclaration = (ClassOrInterfaceDeclaration)statement.getTypeDeclaration();
        var classSyntax = ClassOrInterfaceDeclarationVisitor.VisitClassDeclaration(context, typeDeclaration);
        var text = classSyntax?.NormalizeWhitespace().GetText().ToString();
        if (System.String.IsNullOrWhiteSpace(text))
        {
            return null;
        }
        
        return SyntaxFactory.ParseStatement(text);
    }
}
