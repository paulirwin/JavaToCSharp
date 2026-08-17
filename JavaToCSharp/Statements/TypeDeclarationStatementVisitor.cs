using com.github.javaparser.ast.stmt;
using JavaToCSharp.Declarations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements;

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

/// <summary>
/// Handles Java 16 local record declarations (<c>void m() { record R(int a) {} }</c>).
/// </summary>
/// <remarks>
/// C# has no local record declaration: the compiler parses <c>record R(int a);</c> in a method body
/// as a local function, so the record cannot stay where Java declared it. It is instead hoisted to
/// the enclosing type, reusing the same queue that lifts anonymous class bodies. This widens the
/// record's scope, which is harmless unless the enclosing type already declares a member of the same
/// name.
/// </remarks>
public class LocalRecordDeclarationStatementVisitor : StatementVisitor<LocalRecordDeclarationStmt>
{
    public override StatementSyntax? Visit(ConversionContext context, LocalRecordDeclarationStmt statement)
    {
        var recordSyntax = RecordDeclarationVisitor.VisitRecordDeclaration(context, statement.getRecordDeclaration(), true);

        context.PendingAnonymousTypes.Enqueue(recordSyntax);

        return null;
    }
}
