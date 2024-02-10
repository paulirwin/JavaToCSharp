using System.Diagnostics.CodeAnalysis;
using java.util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using JavaAst = com.github.javaparser.ast;

namespace JavaToCSharp;

public static class Extensions
{
    /// <summary>
    /// Converts a Java Iterable to a .NET IEnumerable&lt;T&gt; and filters the elements of type T.
    /// </summary>
    /// <typeparam name="T">Type of items to be returned.</typeparam>
    /// <param name="iterable">The java Iterable to be enumerated.</param>
    /// <returns>A filtered enumeration of items of type T</returns>
    public static IEnumerable<T> OfType<T>(this java.lang.Iterable iterable)
    {
        var iterator = iterable.iterator();
        while (iterator.hasNext())
        {
            if (iterator.next() is T item)
            {
                yield return item;
            }
        }
    }

    public static List<T>? ToList<T>(this java.util.List? list)
    {
        if (list == null)
            return null;

        var newList = new List<T>();

        for (int i = 0; i < list.size(); i++)
        {
            newList.Add((T)list.get(i));
        }

        return newList;
    }

    public static bool HasFlag<T>(this java.util.EnumSet values, T flag) => values.contains(flag);

    [return: NotNullIfNotNull(nameof(node))]
    public static TSyntax? WithJavaComments<TSyntax>(this TSyntax? syntax,
        ConversionContext context,
        JavaAst.Node? node)
        where TSyntax : SyntaxNode
        => context.Options.IncludeComments
            ? CommentsHelper.AddCommentsTrivias(syntax, node)
            : syntax;

    public static CompilationUnitSyntax WithPackageFileComments(this CompilationUnitSyntax syntax,
            ConversionContext context,
            JavaAst.CompilationUnit compilationUnit,
            JavaAst.PackageDeclaration? packageDeclaration)
        => context.Options.IncludeComments
            ? CommentsHelper.AddPackageComments(syntax, compilationUnit, packageDeclaration)
            : syntax;

    public static T? FromOptional<T>(this Optional optional)
        where T : class
        => optional.isPresent()
            ? optional.get() as T ??
              throw new InvalidOperationException($"Optional did not convert to {typeof(T)}")
            : null;

    public static T FromRequiredOptional<T>(this Optional optional)
        where T : class
        => optional.isPresent()
            ? optional.get() as T ??
              throw new InvalidOperationException($"Optional did not convert to {typeof(T)}")
            : throw new InvalidOperationException("Required optional did not have a value");

    public static ISet<JavaAst.Modifier.Keyword> ToModifierKeywordSet(this JavaAst.NodeList nodeList)
        => nodeList.ToList<JavaAst.Modifier>()?.Select(i => i.getKeyword()).ToHashSet()
           ?? new HashSet<JavaAst.Modifier.Keyword>();
}
