using com.github.javaparser.ast.stmt;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements;

public abstract class StatementVisitor<T> : StatementVisitor
    where T : Statement
{
    public abstract StatementSyntax? Visit(ConversionContext context, T statement);

    protected sealed override StatementSyntax? Visit(ConversionContext context, Statement statement)
    {
        return Visit(context, (T)statement);
    }
}

public abstract class StatementVisitor
{
    private static readonly IDictionary<Type, StatementVisitor> _visitors;

    static StatementVisitor()
    {
        _visitors = new Dictionary<Type, StatementVisitor>
        {
            { typeof(AssertStmt), new AssertStatementVisitor() },
            { typeof(BlockStmt), new BlockStatementVisitor() },
            { typeof(BreakStmt), new BreakStatementVisitor() },
            { typeof(ContinueStmt), new ContinueStatementVisitor() },
            { typeof(DoStmt), new DoStatementVisitor() },
            { typeof(ExpressionStmt), new ExpressionStatementVisitor() },
            { typeof(ForEachStmt), new ForEachStatementVisitor() },
            { typeof(ForStmt), new ForStatementVisitor() },
            { typeof(IfStmt), new IfStatementVisitor() },
            { typeof(LabeledStmt), new LabeledStatementVisitor() },
            { typeof(ReturnStmt), new ReturnStatementVisitor() },
            { typeof(SwitchStmt), new SwitchStatementVisitor() },
            { typeof(SynchronizedStmt), new SynchronizedStatementVisitor() },
            { typeof(ThrowStmt), new ThrowStatementVisitor() },
            { typeof(TryStmt), new TryStatementVisitor() },
            { typeof(WhileStmt), new WhileStatementVisitor() },
            { typeof(YieldStmt), new YieldStatementVisitor() },
            { typeof(EmptyStmt), new EmptyStatementVisitor() },
            { typeof(LocalClassDeclarationStmt), new TypeDeclarationStatementVisitor() }
        };
    }

    protected abstract StatementSyntax? Visit(ConversionContext context, Statement statement);

    public static List<StatementSyntax> VisitStatements(ConversionContext context, IEnumerable<Statement>? statements)
    {
        if (statements is null)
        {
            return [];
        }

        var results = new List<StatementSyntax>();

        // Statements pending from an outer statement list must not be drained here; this list is
        // only responsible for the statements its own children produce.
        var outerPending = context.PendingStatements.Count;

        foreach (var statement in statements)
        {
            var syntax = VisitStatement(context, statement);

            // A visitor may have lowered part of this statement into statements that must precede it,
            // for example a multi-statement switch expression becoming a switch statement.
            if (context.PendingStatements.Count > outerPending)
            {
                results.AddRange(context.PendingStatements.Skip(outerPending));
                context.PendingStatements.RemoveRange(outerPending, context.PendingStatements.Count - outerPending);
            }

            if (syntax is not null)
            {
                results.Add(syntax);
            }
        }

        return results;
    }

    public static StatementSyntax? VisitStatement(ConversionContext context, Statement statement)
    {
        if (!_visitors.TryGetValue(statement.GetType(), out var visitor))
        {
            var message = $"Statement visitor not implemented for statement `{statement}`, `{statement.getBegin()}` type `{statement.GetType()}`.";
            throw new InvalidOperationException(message);
        }

        return visitor.Visit(context, statement).WithJavaComments(context, statement);
    }
}
