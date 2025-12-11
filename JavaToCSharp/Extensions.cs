using System.Diagnostics.CodeAnalysis;
using java.util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using JavaAst = com.github.javaparser.ast;

namespace JavaToCSharp;

public static class Extensions
{
    /// <param name="iterable">The java Iterable to be enumerated.</param>
    extension(java.lang.Iterable iterable)
    {
        /// <summary>
        /// Converts a Java Iterable to a .NET IEnumerable&lt;T&gt; and filters the elements of type T.
        /// </summary>
        /// <typeparam name="T">Type of items to be returned.</typeparam>
        /// <returns>A filtered enumeration of items of type T</returns>
        public IEnumerable<T> OfType<T>()
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
    }

    extension(java.util.List? list)
    {
        public List<T>? ToList<T>()
        {
            if (list is null)
            {
                return null;
            }

            var newList = new List<T>();

            for (int i = 0; i < list.size(); i++)
            {
                newList.Add((T)list.get(i));
            }

            return newList;
        }
    }

    extension(java.util.EnumSet values)
    {
        public bool HasFlag<T>(T flag) => values.contains(flag);
    }

    extension(CompilationUnitSyntax syntax)
    {
        public CompilationUnitSyntax WithPackageFileComments(ConversionContext context,
            JavaAst.CompilationUnit compilationUnit,
            JavaAst.PackageDeclaration? packageDeclaration)
            => context.Options.IncludeComments
                ? CommentsHelper.AddPackageComments(syntax, compilationUnit, packageDeclaration)
                : syntax;
    }

    extension(Optional optional)
    {
        public T? FromOptional<T>()
            where T : class
            => optional.isPresent()
                ? optional.get() as T ?? throw new InvalidOperationException($"Optional did not convert to {typeof(T)}")
                : null;

        public T FromRequiredOptional<T>()
            where T : class
            => optional.isPresent()
                ? optional.get() as T ?? throw new InvalidOperationException($"Optional did not convert to {typeof(T)}")
                : throw new InvalidOperationException("Required optional did not have a value");
    }

    extension(JavaAst.NodeList nodeList)
    {
        public ISet<JavaAst.Modifier.Keyword> ToModifierKeywordSet()
            => nodeList.ToList<JavaAst.Modifier>()?.Select(i => i.getKeyword()).ToHashSet() ?? [];
    }

    extension<TSyntax>(TSyntax syntax) where TSyntax : SyntaxNode
    {
        public TSyntax WithLeadingNewLines(int count = 1)
            => syntax.WithLeadingTrivia(Enumerable.Repeat(Whitespace.NewLine, count));

        public TSyntax WithTrailingNewLines(int count = 1)
            => syntax.WithTrailingTrivia(Enumerable.Repeat(Whitespace.NewLine, count));
    }

    extension<TSyntax>(TSyntax? syntax) where TSyntax : SyntaxNode
    {
        [return: NotNullIfNotNull(nameof(node))]
        public TSyntax? WithJavaComments(ConversionContext context, JavaAst.Node? node)
            => context.Options.IncludeComments
                ? CommentsHelper.AddCommentsTrivias(syntax, node)
                : syntax;
    }
}
