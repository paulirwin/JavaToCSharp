using japa.parser.ast.stmt;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavaToCSharp.Statements
{
    public class ContinueStatementVisitor : StatementVisitor<ContinueStmt>
    {
        public override StatementSyntax Visit(ConversionContext context, ContinueStmt cnt)
        {
            if (!string.IsNullOrEmpty(cnt.getId()))
                context.Options.Warning("Continue with label detected, using plain continue instead. Check for correctness.", cnt.getBeginLine());

            return Syntax.ContinueStatement();
        }
    }
}
