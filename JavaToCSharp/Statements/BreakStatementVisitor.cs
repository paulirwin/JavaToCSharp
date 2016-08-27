using com.github.javaparser.ast.stmt;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavaToCSharp.Statements
{
    public class BreakStatementVisitor : StatementVisitor<BreakStmt>
    {
        public override StatementSyntax Visit(ConversionContext context, BreakStmt brk)
        {
            if (!string.IsNullOrEmpty(brk.getId()))
                context.Options.Warning("Break with label detected, using plain break instead. Check for correctness.", brk.getBegin().line);

            return Syntax.BreakStatement();
        }
    }
}
