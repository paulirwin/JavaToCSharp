using com.github.javaparser.ast.expr;
using com.github.javaparser.ast.stmt;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements;

public class ContinueStatementVisitor : StatementVisitor<ContinueStmt>
{
    public override StatementSyntax Visit(ConversionContext context, ContinueStmt cnt)
    {
        var label = cnt.getLabel().FromOptional<SimpleName>();

        if (label is not null)
            return LabeledJumpHelper.CreateJump(context, label.asString(), isBreak: false);

        return SyntaxFactory.ContinueStatement();
    }
}
