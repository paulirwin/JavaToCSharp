using System;
using System.Collections.Generic;
using com.github.javaparser.ast.stmt;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements
{
    public abstract class StatementVisitor<T> : StatementVisitor
        where T : Statement
    {
        public abstract StatementSyntax Visit(ConversionContext context, T statement);

        protected sealed override StatementSyntax Visit(ConversionContext context, Statement statement)
        {
            return Visit(context, (T)statement);
        }
    }

    public abstract class StatementVisitor
    {
        private static readonly IDictionary<Type, StatementVisitor> _visitors;

        static StatementVisitor()
        {
            // TODO: replace with MEF
            _visitors = new Dictionary<Type, StatementVisitor>
            {
                { typeof(AssertStmt), new AssertStatementVisitor() },
                { typeof(BlockStmt), new BlockStatementVisitor() },
                { typeof(BreakStmt), new BreakStatementVisitor() },
                { typeof(ContinueStmt), new ContinueStatementVisitor() },
                { typeof(DoStmt), new DoStatementVisitor() },
                { typeof(ExpressionStmt), new ExpressionStatementVisitor() },
                { typeof(ForeachStmt), new ForEachStatementVisitor() },
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
                { typeof(TypeDeclarationStmt), new TypeDeclarationStatementVisitor() }
            };
        }

        protected abstract StatementSyntax Visit(ConversionContext context, Statement statement);

        public static List<StatementSyntax> VisitStatements(ConversionContext context, IEnumerable<Statement> statements)
        {
            if (statements == null)
                return new List<StatementSyntax>();

            var syntaxes = new List<StatementSyntax>();

            foreach (var statement in statements)
            {
                StatementSyntax syntax = VisitStatement(context, statement);

                if (syntax != null)
                    syntaxes.Add(syntax);
            }

            return syntaxes;
        }

        public static StatementSyntax VisitStatement(ConversionContext context, Statement statement)
        {
            StatementVisitor visitor;

            if (!_visitors.TryGetValue(statement.GetType(), out visitor))
            {
                var message = $"Statement visitor not implemented for statement `{statement}`, `{statement.getBegin()}` type `{statement.GetType()}`.";
                throw new InvalidOperationException(message);
            }

            return visitor.Visit(context, statement);
        }
    }
}
