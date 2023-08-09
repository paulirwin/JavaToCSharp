using System.Collections.Generic;
using System.Linq;
using com.github.javaparser.ast.expr;
using JavaToCSharp.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ast = com.github.javaparser.ast;

namespace JavaToCSharp;

public static class TypeHelper
{
    private static readonly Dictionary<string, string> _typeNameConversions = new()
    {
        // Simple types
        ["boolean"] = "bool",
        ["Boolean"] = "bool",
        ["ICloseable"] = "IDisposable",
        ["Integer"] = "int",
        ["Long"] = "long",
        ["Float"] = "float",
        ["String"] = "string",
        ["Object"] = "object",
        ["AutoCloseable"] = "IDisposable",

        // Generic types
        ["ArrayList"] = "List",
        ["List"] = "IList",
        ["Map"] = "Dictionary",
        ["Set"] = "HashSet",
        ["Iterator"] = "IEnumerator",

        // Exceptions
        ["AlreadyClosedException"] = "ObjectDisposedException",
        ["Error"] = "Exception",
        ["IllegalArgumentException"] = "ArgumentException",
        ["IllegalStateException"] = "InvalidOperationException",
        ["UnsupportedOperationException"] = "NotSupportedException",
        ["RuntimeException"] = "Exception",
        ["AccessDeniedException"] = "UnauthorizedAccessException",
        ["AssertionError"] = "InvalidOperationException",
        ["NullPointerException"] = "NullReferenceException",
        ["UncheckedIOException"] = "IOException",
        ["EOFException"] = "EndOfStreamException",
        ["NoSuchFileException"] = "FileNotFoundException",
    };

    public static void AddOrUpdateTypeNameConversions(string key, string value)
    {
        _typeNameConversions[key] = value;
    }

    public static string ConvertTypeOf(ast.nodeTypes.NodeWithType typedNode)
    {
        return ConvertType(typedNode.getType().toString());
    }
    
    public static string ConvertType(ast.type.Type type)
    {
        return ConvertType(type.toString());
    }

    public static string ConvertType(string typeName)
    {
        return TypeNameParser.ParseTypeName(typeName, s =>
        {
            if (_typeNameConversions.TryGetValue(s, out string? converted))
            {
                return converted;
            }
            return s;
        });
    }

    public static string Capitalize(string name)
    {
        var parts = name.Split('.');

        var joined = System.String.Join(".", parts.Select(i =>
        {
            if (i.Length == 1)
                return i.ToUpper();
            else
                return i[0].ToString().ToUpper() + i[1..];
        }));

        return joined;
    }

    public static string EscapeIdentifier(string name)
    {
        // @ (C# Reference): https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/verbatim
        return name switch {
            "string" or "ref" or "object" or "int" or "short" or "float" or "long" or "double" or "decimal" or "in" or 
            "out" or "byte" or "class" or "delegate" or "params" or "is" or "as" or "base" or "namespace" or "event" or 
            "lock" or "operator" or "override" => "@" + name,
            _ => name,
        };
    }

    public static string ReplaceCommonMethodNames(string name)
    {
        return name.ToLower() switch {
            "hashcode" => "GetHashCode",
            "getclass" => "GetType",
            _ => name,
        };
    }

    public static TypeSyntax GetSyntaxFromType(ast.type.ClassOrInterfaceType type)
    {
        string typeName = ConvertType(type.getNameAsString());
        var typeArgs = type.getTypeArguments().FromOptional<ast.NodeList>()?.ToList<ast.type.Type>();

        TypeSyntax typeSyntax;

        if (typeArgs is { Count: > 0 })
        {
            typeSyntax = SyntaxFactory.GenericName(typeName)
                .AddTypeArgumentListArguments(typeArgs.Select(t => SyntaxFactory.ParseTypeName(ConvertType(t))).ToArray());
        }
        else
        {
            typeSyntax = SyntaxFactory.ParseTypeName(typeName);
        }

        return typeSyntax;
    }

    public static ArgumentListSyntax GetSyntaxFromArguments(ConversionContext context, java.util.List args)
    {
        return SyntaxFactory.ArgumentList(GetSeparatedListFromArguments(context, args));
    }

    private static SeparatedSyntaxList<ArgumentSyntax> GetSeparatedListFromArguments(ConversionContext context, java.util.List args)
    {
        return GetSeparatedListFromArguments(context, args.OfType<Expression>());
    }

    private static SeparatedSyntaxList<ArgumentSyntax> GetSeparatedListFromArguments(ConversionContext context, IEnumerable<Expression> args)
    {
        var argSyntaxes = new List<ArgumentSyntax>();

        foreach (var arg in args)
        {
            var argSyntax = ExpressionVisitor.VisitExpression(context, arg);
            if (argSyntax is null)
            {
                continue;
            }
            
            argSyntaxes.Add(SyntaxFactory.Argument(argSyntax));
        }

        var separators = Enumerable.Repeat(SyntaxFactory.Token(SyntaxKind.CommaToken), argSyntaxes.Count - 1);
        return SyntaxFactory.SeparatedList(argSyntaxes, separators);
    }

    /// <summary>
    /// Transforms method calls into property and indexer accesses where appropriate.
    /// </summary>
    /// <param name="context">The conversion context.</param>
    /// <param name="methodCallExpr">The <c>MethodCallExpr</c> to be transformed.</param>
    /// <param name="transformedSyntax">The resulting transformed syntax.</param>
    /// <returns><c>true</c> if the syntax was transformed, <c>false</c> otherwise</returns>
    public static bool TryTransformMethodCall(ConversionContext context, MethodCallExpr methodCallExpr,
        out ExpressionSyntax? transformedSyntax)
    {
        if (methodCallExpr.getScope().FromOptional<Expression>() is { } scope)
        {
            var methodName = methodCallExpr.getName();
            var args = methodCallExpr.getArguments();

            switch (methodName.getIdentifier())
            {
                case "size" when args.size() == 0:
                    var scopeSyntaxSize = ExpressionVisitor.VisitExpression(context, scope);
                    transformedSyntax = ReplaceSizeByCount(scopeSyntaxSize);
                    return true;

                case "get" when args.size() == 1:
                    var scopeSyntaxGet = ExpressionVisitor.VisitExpression(context, scope);
                    if (scopeSyntaxGet is null) 
                    {
                        transformedSyntax = null;
                        return false;
                    }
                    
                    transformedSyntax = ReplaceGetByIndexAccess(context, scopeSyntaxGet, args);
                    return true;

                case "set" when args.size() == 2:
                    var scopeSyntaxSet = ExpressionVisitor.VisitExpression(context, scope);
                    if (scopeSyntaxSet is null) 
                    {
                        transformedSyntax = null;
                        return false;
                    }
                    
                    transformedSyntax = ReplaceSetByIndexAccess(context, scopeSyntaxSet, args);
                    return true;
            }
        }

        transformedSyntax = null;
        return false;


        static MemberAccessExpressionSyntax? ReplaceSizeByCount(ExpressionSyntax? scopeSyntax)
        {
            if (scopeSyntax is null)
            {
                return null;
            }
            
            // Replace   expr.Size()   by   expr.Count
            return SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                scopeSyntax,
                SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("Count")));
        }

        static ExpressionSyntax ReplaceGetByIndexAccess(ConversionContext context, ExpressionSyntax scopeSyntax,
            java.util.List args)
        {
            // Replace   expr.Get(i)   by   expr[i]
            return SyntaxFactory.ElementAccessExpression(
                scopeSyntax,
                SyntaxFactory.BracketedArgumentList(GetSeparatedListFromArguments(context, args))
            );
        }

        static ExpressionSyntax? ReplaceSetByIndexAccess(
            ConversionContext context, 
            ExpressionSyntax scopeSyntax,
            java.util.List args)
        {
            // Replace   expr.Set(i,v)   by   expr[i] = v
            var argsList = args.ToList<Expression>() ?? new List<Expression>();
            var left = SyntaxFactory.ElementAccessExpression(
                 scopeSyntax,
                 SyntaxFactory.BracketedArgumentList(GetSeparatedListFromArguments(context, argsList.Take(1)))
                ).WithTrailingTrivia(SyntaxFactory.Whitespace(" "));
            var right = ExpressionVisitor.VisitExpression(context, argsList[1])?.WithLeadingTrivia(SyntaxFactory.Whitespace(" "));
            if (right is null)
            {
                return null;
            }
            
            return SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, left, right);
        }
    }
}
