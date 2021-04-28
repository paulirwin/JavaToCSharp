using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using JavaAst = com.github.javaparser.ast;

namespace JavaToCSharp
{
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

        public static List<T> ToList<T>(this java.util.List list)
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

        public static TSyntax WithJavaComments<TSyntax>(this TSyntax syntax, JavaAst.Node node, string singleLineCommentEnd = null) 
            where TSyntax : SyntaxNode =>
            CommentsHelper.AddCommentsTrivias(syntax, node, singleLineCommentEnd);
    }
}
