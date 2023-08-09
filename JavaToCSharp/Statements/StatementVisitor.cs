using System;
using System.Collections.Generic;
using System.Linq;
using com.github.javaparser.ast.stmt;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements
{
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
                { typeof(EmptyStmt), new EmptyStatementVisitor() },
                { typeof(LocalClassDeclarationStmt), new TypeDeclarationStatementVisitor() }
            };
        }

        protected abstract StatementSyntax? Visit(ConversionContext context, Statement statement);

        public static List<StatementSyntax> VisitStatements(ConversionContext context, IEnumerable<Statement>? statements) =>
            statements == null
                ? new List<StatementSyntax>()
                : statements.Select(statement => VisitStatement(context, statement))
                            .Where(syntax => syntax != null)!.ToList<StatementSyntax>();

        public static StatementSyntax? VisitStatement(ConversionContext context, Statement statement)
        {
            if (!_visitors.TryGetValue(statement.GetType(), out var visitor))
            {
                var message = $"Statement visitor not implemented for statement `{statement}`, `{statement.getBegin()}` type `{statement.GetType()}`.";
                throw new InvalidOperationException(message);
            }

            return visitor.Visit(context, statement).WithJavaComments(statement);
        }
    }
}
