using System.Collections.Generic;
using System.Linq;
using JavaToCSharp.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ast = com.github.javaparser.ast;

namespace JavaToCSharp
{
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

            // Generic types
            ["ArrayList"] = "List",
            ["List"] = "IList",

            // Exceptions
            ["AlreadyClosedException"] = "ObjectDisposedException",
            ["Error"] = "Exception",
            ["IllegalArgumentException"] = "ArgumentException",
            ["IllegalStateException"] = "InvalidOperationException",
            ["UnsupportedOperationException"] = "NotSupportedException",
            ["RuntimeException"] = "Exception",
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
                if (_typeNameConversions.TryGetValue(s, out string converted))
                {
                    return converted;
                }
                return s;
            });
        }

        public static string Capitalize(string name)
        {
            var parts = name.Split('.');

            var joined = string.Join(".", parts.Select(i =>
            {
                if (i.Length == 1)
                    return i.ToUpper();
                else
                    return i[0].ToString().ToUpper() + i.Substring(1);
            }));

            return joined;
        }

        public static string EscapeIdentifier(string name)
        {
            // @ (C# Reference): https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/verbatim
            switch (name)
            {
                case "string":
                case "ref":
                case "object":
                case "int":
                case "short":
                case "float":
                case "long":
                case "double":
                case "decimal":
                case "in":
                case "out":
                case "byte":
                case "class":
                case "delegate":
                case "params":
                case "is":
                case "as":
                case "base":
                case "namespace":
                case "event":
                case "lock":
                case "operator":
                case "override":
                    return "@" + name;
                default:
                    return name;
            }
        }

        public static string ReplaceCommonMethodNames(string name)
        {
            switch (name.ToLower())
            {
                case "hashcode":
                    return "GetHashCode";
                case "getclass":
                    return "GetType";
                default:
                    return name;
            }
        }

        public static TypeSyntax GetSyntaxFromType(ast.type.ClassOrInterfaceType type)
        {
            string typeName = ConvertType(type.getName());
            var typeArgs = type.getTypeArgs().ToList<com.github.javaparser.ast.type.Type>();

            TypeSyntax typeSyntax;

            if (typeArgs != null && typeArgs.Count > 0)
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
            return GetSeparatedListFromArguments(context, args.OfType<ast.expr.Expression>());
        }

        private static SeparatedSyntaxList<ArgumentSyntax> GetSeparatedListFromArguments(ConversionContext context, IEnumerable<ast.expr.Expression> args)
        {
            var argSyntaxes = new List<ArgumentSyntax>();

            foreach (var arg in args)
            {
                var argSyntax = ExpressionVisitor.VisitExpression(context, arg);
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
        public static bool TryTransformMethodCall(ConversionContext context, ast.expr.MethodCallExpr methodCallExpr,
            out ExpressionSyntax transformedSyntax)
        {
            if (methodCallExpr.getScope() is ast.expr.Expression scope)
            {
                var methodName = methodCallExpr.getName();
                var args = methodCallExpr.getArgs();

                switch (methodName)
                {
                    case "size" when args.size() == 0:
                        var scopeSyntaxSize = ExpressionVisitor.VisitExpression(context, scope);
                        transformedSyntax = ReplaceSizeByCount(scopeSyntaxSize);
                        return true;

                    case "get" when args.size() == 1:
                        var scopeSyntaxGet = ExpressionVisitor.VisitExpression(context, scope);
                        transformedSyntax = ReplaceGetByIndexAccess(context, scopeSyntaxGet, args);
                        return true;

                    case "set" when args.size() == 2:
                        var scopeSyntaxSet = ExpressionVisitor.VisitExpression(context, scope);
                        transformedSyntax = ReplaceSetByIndexAccess(context, scopeSyntaxSet, args);
                        return true;
                }
            }

            transformedSyntax = null;
            return false;


            static MemberAccessExpressionSyntax ReplaceSizeByCount(ExpressionSyntax scopeSyntax)
            {
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

            static ExpressionSyntax ReplaceSetByIndexAccess(ConversionContext context, ExpressionSyntax scopeSyntax,
                java.util.List args)
            {
                // Replace   expr.Set(i,v)   by   expr[i] = v
                var argsList = args.ToList<ast.expr.Expression>();
                return SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.ElementAccessExpression(
                        scopeSyntax,
                        SyntaxFactory.BracketedArgumentList(GetSeparatedListFromArguments(context, argsList.Take(1)))
                    ).WithTrailingTrivia(SyntaxFactory.Whitespace(" ")),
                     ExpressionVisitor.VisitExpression(context, argsList[1]).WithLeadingTrivia(SyntaxFactory.Whitespace(" "))
                );
            }
        }
    }
}
