using System;
using System.Collections.Generic;
using System.Linq;
using com.github.javaparser;
using com.github.javaparser.ast.expr;
using com.github.javaparser.ast.stmt;
using com.github.javaparser.ast.type;
using JavaToCSharp.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements;

public class TryStatementVisitor : StatementVisitor<TryStmt>
{
    public override StatementSyntax Visit(ConversionContext context, TryStmt tryStmt)
    {
        var resources = tryStmt.getResources().ToList<Expression>() ?? new List<Expression>();

        var tryBlock = tryStmt.getTryBlock();
        var tryStatements = tryBlock.getStatements().ToList<Statement>();

        var tryConverted = VisitStatements(context, tryStatements);
        var tryBlockStatements = tryConverted.ToArray();

        if (resources.Count == 0)
        {
            // regular try statement
            return TransformTryBlock(context, tryStmt, tryBlockStatements);
        }

        // try-with-resources statement

        // for inner-most using block, use the statements from the Java try block
        StatementSyntax result = SyntaxFactory.Block(tryBlockStatements);

        // go inner-most to outer-most
        resources.Reverse();

        foreach (var resource in resources)
        {
            if (resource.isNameExpr())
            {
                result = SyntaxFactory.UsingStatement(result)
                    .WithExpression(
                        SyntaxFactory.IdentifierName(resource.asNameExpr().getNameAsString()));
            }
            else if (resource.isVariableDeclarationExpr())
            {
                var varDecl = resource.asVariableDeclarationExpr();

                if (varDecl.getVariables().size() > 1)
                {
                    context.Options.Warning("Unexpected multiple variables in try-with-resources declaration",
                        resource.getBegin().FromRequiredOptional<Position>().line);
                }

                var variable = varDecl.getVariable(0);
                var variableInit = variable.getInitializer().FromRequiredOptional<Expression>();
                
                var initSyntax = ExpressionVisitor.VisitExpression(context, variableInit)
                                 ?? throw new InvalidOperationException("Unable to parse try-with-resources variable initializer");
                
                result = SyntaxFactory.UsingStatement(result)
                    .WithDeclaration(
                        SyntaxFactory.VariableDeclaration(
                            SyntaxFactory.ParseTypeName(TypeHelper.ConvertType(variable.getType())),
                            SyntaxFactory.SeparatedList(new[]
                            {
                                SyntaxFactory.VariableDeclarator(variable.getNameAsString())
                                    .WithInitializer(SyntaxFactory.EqualsValueClause(initSyntax))
                            })
                        ));
            }
            else
            {
                context.Options.Warning("Unexpected try-with-resources resource",
                    resource.getBegin().FromRequiredOptional<Position>().line);
            }
        }

        if (tryStmt.getFinallyBlock().isPresent() || tryStmt.getCatchClauses().ToList<CatchClause>()?.Count > 0)
        {
            result = TransformTryBlock(context, tryStmt, new[] { result });
        }

        return result;
    }

    private static StatementSyntax TransformTryBlock(ConversionContext context, TryStmt tryStmt,
        IEnumerable<StatementSyntax> tryBlockStatements)
    {
        var catches = tryStmt.getCatchClauses().ToList<CatchClause>();

        var trySyn = SyntaxFactory.TryStatement()
            .AddBlockStatements(tryBlockStatements.ToArray());

        if (catches != null)
        {
            foreach (var catchClause in catches)
            {
                var paramType = catchClause.getParameter().getType();
                if (paramType is UnionType)
                {
                    var nodes = paramType.getChildNodes()?.ToList<ReferenceType>() ?? new List<ReferenceType>();
                    foreach (var node in nodes)
                    {
                        var referenceTypeName = node.getElementType().ToString();
                        trySyn = AddCatches(context, catchClause, referenceTypeName, trySyn);
                    }
                }
                else
                {
                    var referenceType = (ReferenceType)catchClause.getParameter().getType();
                    trySyn = AddCatches(context, catchClause, referenceType.getElementType().ToString(), trySyn);
                }
            }
        }

        var finallyBlock = tryStmt.getFinallyBlock().FromOptional<BlockStmt>();

        if (finallyBlock == null)
            return trySyn;

        var finallyStatements = finallyBlock.getStatements().ToList<Statement>();
        var finallyConverted = VisitStatements(context, finallyStatements);
        var finallyBlockSyntax = SyntaxFactory.Block(finallyConverted);

        trySyn = trySyn.WithFinally(SyntaxFactory.FinallyClause(finallyBlockSyntax));

        return trySyn;
    }

    private static TryStatementSyntax AddCatches(ConversionContext context, CatchClause ctch,
        string typeName, TryStatementSyntax trySyn)
    {
        var block = ctch.getBody();
        var catchStatements = block.getStatements().ToList<Statement>();
        var catchConverted = VisitStatements(context, catchStatements);
        var catchBlockSyntax = SyntaxFactory.Block(catchConverted);

        var type = TypeHelper.ConvertType(typeName);

        trySyn = trySyn.AddCatches(
            SyntaxFactory.CatchClause(
                SyntaxFactory.CatchDeclaration(
                    SyntaxFactory.ParseTypeName(type),
                    SyntaxFactory.ParseToken(ctch.getParameter().getNameAsString())
                ),
                filter: null,
                block: catchBlockSyntax
            )
        );

        return trySyn;
    }
}