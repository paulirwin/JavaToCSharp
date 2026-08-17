using com.github.javaparser.ast.expr;
using com.github.javaparser.ast.stmt;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements;

public class BreakStatementVisitor : StatementVisitor<BreakStmt>
{
    public override StatementSyntax Visit(ConversionContext context, BreakStmt brk)
    {
        var label = brk.getLabel().FromOptional<SimpleName>();

        if (label is not null)
            return LabeledJumpHelper.CreateJump(context, label.asString(), isBreak: true);

        return SyntaxFactory.BreakStatement();
    }
}
