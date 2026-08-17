using System.Collections.Concurrent;
using com.github.javaparser.ast.expr;
using com.github.javaparser.ast.type;
using JavaToCSharp.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Ast = com.github.javaparser.ast;
using Type = com.github.javaparser.ast.type.Type;

namespace JavaToCSharp;

public static class TypeHelper
{
    // Mutated at conversion time via AddOrUpdateTypeNameConversions (e.g. to register interface
    // renames when StartInterfaceNamesWithI is set) while other threads read it in ConvertType,
    // so this must be a concurrent collection to keep parallel conversions safe.
    private static readonly ConcurrentDictionary<string, string> _typeNameConversions = new()
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

    public static string ConvertTypeOf(Ast.nodeTypes.NodeWithType typedNode)
    {
        return ConvertType(typedNode.getType().toString());
    }

    public static string ConvertType(Type type)
    {
        return ConvertType(type.toString());
    }

    public static string ConvertType(string typeName)
        => TypeNameParser.ParseTypeName(typeName, s => _typeNameConversions.TryGetValue(s, out string? converted) ? converted : s);

    public static TypeSyntax ConvertTypeSyntax(Type type, int arrayRank)
    {
        if (type is ArrayType arrayType)
        {
            if (arrayRank > 0 && arrayType.getArrayLevel() != arrayRank)
            {
                throw new ArgumentException("Given array rank does not match the array level of the type", nameof(arrayRank));
            }

            arrayRank = arrayType.getArrayLevel();
            Type elementType;

            while ((elementType = arrayType.getElementType()) is ArrayType nestedArrayType)
            {
                arrayType = nestedArrayType;
            }

            return ConvertTypeSyntax(elementType, arrayRank);
        }

        return arrayRank == 0
            ? SyntaxFactory.ParseTypeName(ConvertType(type))
            : ConvertArrayTypeSyntax(type, arrayRank);
    }

    public static ArrayTypeSyntax ConvertArrayTypeSyntax(Type type, int arrayRank)
    {
        var rankSpecifiers = SyntaxFactory.ArrayRankSpecifier(
            SyntaxFactory.SeparatedList<ExpressionSyntax>(
                Enumerable.Repeat(SyntaxFactory.OmittedArraySizeExpression(), arrayRank)
            ));

        return SyntaxFactory.ArrayType(ConvertTypeSyntax(type, 0))
            .WithRankSpecifiers(SyntaxFactory.SingletonList(rankSpecifiers));
    }

    public static string Capitalize(string name)
    {
        var parts = name.Split('.');

        var joined = string.Join(".", parts.Select(i =>
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
        // C# Keywords: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/
        return name switch {
            "abstract" or "as" or "base" or "bool" or "break" or
            "byte" or "case" or "catch" or "char" or "checked" or
            "class" or "const" or "continue" or "decimal" or "default" or
            "delegate" or "do" or "double" or "else" or "enum" or
            "event" or "explicit" or "extern" or "false" or "finally" or
            "fixed" or "float" or "for" or "foreach" or "goto" or
            "if" or "implicit" or "in" or "int" or "interface" or
            "internal" or "is" or "lock" or "long" or "namespace" or
            "new" or "null" or "object" or "operator" or "out" or
            "override" or "params" or "private" or "protected" or "public" or
            "readonly" or "ref" or "return" or "sbyte" or "sealed" or
            "short" or "sizeof" or "stackalloc" or "static" or "string" or
            "struct" or "switch" or "this" or "throw" or "true" or
            "try" or "typeof" or "uint" or "ulong" or "unchecked" or
            "unsafe" or "ushort" or "using" or "virtual" or "void" or
            "volatile" or "while" => "@" + name,
            _ => name,
        };
    }

    public static string ReplaceCommonMethodNames(string name)
    {
        return name.ToLower() switch {
            "hashcode" => "GetHashCode",
            "getclass" => "GetType",
            "close" => "Dispose",
            _ => name,
        };
    }

    public static TypeSyntax GetSyntaxFromType(ClassOrInterfaceType type)
    {
        string typeName = ConvertType(type.getNameAsString());
        var typeArgs = type.getTypeArguments().FromOptional<Ast.NodeList>()?.ToList<Type>();

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

        // Nested types (e.g. `Outer.Inner`) carry their declaring type as a scope, which
        // getNameAsString() above does not include. Prepend it so the declaring type isn't lost.
        if (type.getScope().FromOptional<ClassOrInterfaceType>() is { } scope
            && typeSyntax is SimpleNameSyntax simpleName
            && GetSyntaxFromType(scope) is NameSyntax scopeName)
        {
            return SyntaxFactory.QualifiedName(scopeName, simpleName);
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
    /// Returns the list of C# type parameter constraints that must be added to a class syntax
    /// to convert the java bounded type parameters of a class declaration.
    /// e.g. to convert <![CDATA[ <T extends Clazz<T>> ]]> into <![CDATA[ <T> where T : Clazz<T> ]]>
    /// </summary>
    public static IEnumerable<TypeParameterConstraintClauseSyntax> GetTypeParameterListConstraints(List<TypeParameter> typeParams)
    {
        var typeParameterConstraints = new List<TypeParameterConstraintClauseSyntax>();

        foreach (TypeParameter typeParam in typeParams)
        {
            if (typeParam.getTypeBound().size() > 0)
            {
                var typeConstraintsSyntax = new SeparatedSyntaxList<TypeParameterConstraintSyntax>();

                foreach (ClassOrInterfaceType bound in typeParam.getTypeBound())
                {
                    typeConstraintsSyntax = typeConstraintsSyntax.Add(SyntaxFactory.TypeConstraint(SyntaxFactory.ParseTypeName(bound.asString())));
                }

                var typeIdentifier = SyntaxFactory.IdentifierName(typeParam.getName().asString());
                var parameterConstraintClauseSyntax = SyntaxFactory.TypeParameterConstraintClause(typeIdentifier, typeConstraintsSyntax);

                typeParameterConstraints.Add(parameterConstraintClauseSyntax);
            }
        }

        return typeParameterConstraints;
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
                case "length" when args.size() == 0:
                {
                    var scopeSyntaxLength = ExpressionVisitor.VisitExpression(context, scope);
                    transformedSyntax = ReplaceMethodByProperty(scopeSyntaxLength, "Length");
                    return true;
                }

                case "size" when args.size() == 0:
                {
                    var scopeSyntaxSize = ExpressionVisitor.VisitExpression(context, scope);
                    transformedSyntax = ReplaceMethodByProperty(scopeSyntaxSize, "Count");
                    return true;
                }

                case "getMessage" when args.size() == 0:
                {
                    var scopeSyntaxMessage = ExpressionVisitor.VisitExpression(context, scope);
                    transformedSyntax = ReplaceMethodByProperty(scopeSyntaxMessage, "Message");
                    return true;
                }

                case "get" when args.size() == 1:
                {
                    var scopeSyntaxGet = ExpressionVisitor.VisitExpression(context, scope);
                    if (scopeSyntaxGet is null)
                    {
                        transformedSyntax = null;
                        return false;
                    }

                    transformedSyntax = ReplaceGetByIndexAccess(context, scopeSyntaxGet, args);
                    return true;
                }

                case "set" when args.size() == 2:
                {
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
        }

        transformedSyntax = null;
        return false;

        static MemberAccessExpressionSyntax? ReplaceMethodByProperty(ExpressionSyntax? scopeSyntax, string identifier)
        {
            if (scopeSyntax is null)
            {
                return null;
            }

            // Replace   expr.Size()   by   expr.Count
            return SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                scopeSyntax,
                SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(identifier)));
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
            var argsList = args.ToList<Expression>() ?? [];
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
