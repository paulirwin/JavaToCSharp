using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using JavaAst = com.github.javaparser.ast;
using JavaComments = com.github.javaparser.ast.comments;
using JavaParser = com.github.javaparser;
using SysRegex = System.Text.RegularExpressions;

namespace JavaToCSharp;

public static class CommentsHelper
{
    private enum CommentPosition
    {
        Leading,
        Trailing
    }

    // Regex: Optional *, capture optional @par, capture optional text (keep leading whitespaces, trim end).
    private static readonly SysRegex.Regex _analyzeDocString =
        new(@"^(\s*\*)?(\s*(?<param>@[a-z]+))?\s?(?<text>.*?)\s*$", SysRegex.RegexOptions.Compiled);

    private static readonly Dictionary<string, string> _knownTagsDict = new()
    {
        ["@param"] = "param",
        ["@return"] = "returns",
        ["@exception"] = "exception",
        ["@throws"] = "exception"
    };

    public static CompilationUnitSyntax AddPackageComments(CompilationUnitSyntax syntax,
        JavaAst.CompilationUnit compilationUnit,
        JavaAst.PackageDeclaration? packageDeclaration)
    {
        var leadingTriviaList = new List<SyntaxTrivia>();

        if (compilationUnit.getComment().FromOptional<JavaComments.Comment>() is { } compilationUnitComment)
        {
            var (kind, pre, post) = GetCommentInfo(compilationUnitComment);
            var commentTrivia = SyntaxFactory.SyntaxTrivia(kind, pre + compilationUnitComment.getContent() + post + Environment.NewLine);
            leadingTriviaList.Add(commentTrivia);
        }

        if (packageDeclaration is not null)
        {
            var packageComments = GatherComments(packageDeclaration);

            if (packageComments.Count > 0)
            {
                foreach (var (comment, _) in packageComments)
                {
                    var (kind, pre, post) = GetCommentInfo(comment);
                    var commentTrivia = SyntaxFactory.SyntaxTrivia(kind, pre + comment.getContent() + post + Environment.NewLine);
                    leadingTriviaList.Add(commentTrivia);
                }

                syntax = syntax.WithLeadingTrivia(leadingTriviaList);
            }
        }

        return leadingTriviaList.Count > 0 ? syntax.WithLeadingTrivia(leadingTriviaList) : syntax;
    }

    public static TSyntax? AddCommentsTrivias<TSyntax>(TSyntax? syntax, JavaAst.Node? node) where TSyntax : SyntaxNode
    {
        if (syntax is null)
        {
            return null;
        }

        var comments = GatherComments(node);

        if (comments.Count > 0)
        {
            var leadingTriviaList = new List<SyntaxTrivia>();
            var trailingTriviaList = new List<SyntaxTrivia>();

            foreach (var (comment, pos) in comments)
            {
                var (kind, pre, post) = GetCommentInfo(comment);

                if (kind == SyntaxKind.XmlComment)
                {
                    leadingTriviaList.AddRange(ConvertDocComment(comment, post));
                }
                else
                {
                    var commentTrivia = SyntaxFactory.SyntaxTrivia(kind, pre + comment.getContent() + post);
                    if (pos == CommentPosition.Leading)
                    {
                        leadingTriviaList.Add(commentTrivia);
                    }
                    else
                    {
                        trailingTriviaList.Add(commentTrivia);
                    }
                }
            }

            syntax = syntax
                .WithLeadingTrivia(leadingTriviaList)
                .WithTrailingTrivia(trailingTriviaList);
        }

        return syntax;
    }

    private static (SyntaxKind kind, string? pre, string? post) GetCommentInfo(
        JavaComments.Comment comment)
    {
        return comment switch
        {
            JavaComments.BlockComment => (SyntaxKind.MultiLineCommentTrivia, "/*", "*/"),
            JavaComments.JavadocComment => (SyntaxKind.XmlComment, null, null),
            _ => (SyntaxKind.SingleLineCommentTrivia, "//", null),
        };
    }

    private static List<(JavaComments.Comment c, CommentPosition pos)> GatherComments(JavaAst.Node? node)
    {
        var result = new List<(JavaComments.Comment c, CommentPosition pos)>();
        if (node == null) return result;

        var parentNode = node.getParentNode().FromOptional<JavaAst.Node>();
        if (parentNode is null)
        {
            if (node.getComment().FromOptional<JavaComments.Comment>() is { } comment)
            {
                result.Add((comment, CommentPosition.Leading));
            }
        }
        else
        {
            var unsortedComments = parentNode.getAllContainedComments();
            if (unsortedComments.size() != 0)
            {
                var comments = unsortedComments.OfType<JavaComments.Comment>()
                    .OrderBy(c => c.getBegin().FromOptional<JavaParser.Position>()?.line)
                    .ThenBy(c => c.getBegin().FromOptional<JavaParser.Position>()?.column)
                    .ToList();

                // Find leading comments
                var nodeBegin = node.getBegin().FromOptional<JavaParser.Position>() 
                                ?? throw new InvalidOperationException("Node did not have a begin position");
                var previousSibling = GetPreviousSibling(parentNode, nodeBegin);
                int previousPos = previousSibling?.getEnd().FromOptional<JavaParser.Position>()?.line ?? 0;
                var leadingComments = GetLeadingComments(comments, nodeBegin, previousPos);
                result.AddRange(leadingComments);

                // Find trailing comments.
                // We consider only comments either appearing on the same line or, if no sibling nodes follow,
                // then also comments on the succeeding lines (because otherwise they belong to the next sibling).
                var nodeEnd = node.getEnd().FromOptional<JavaParser.Position>() 
                              ?? throw new InvalidOperationException("Node did not have an end position");

                var trailingComments = HasNextSibling(parentNode, nodeEnd)
                    ? GetLineEndComment(comments, nodeEnd)
                    : GetTrailingComments(comments, nodeEnd);

                result.AddRange(trailingComments);
            }
        }

        return result;
    }

    private static IEnumerable<(JavaComments.Comment c, CommentPosition Trailing)> GetTrailingComments(IEnumerable<JavaComments.Comment> comments, JavaParser.Position nodeEnd) =>
        comments.Where(c =>
            {
                var commentBegin = c.getBegin().FromOptional<JavaParser.Position>();
                return commentBegin != null && (commentBegin.line == nodeEnd.line && commentBegin.column > nodeEnd.column || commentBegin.line > nodeEnd.line);
            })
            .Select(c => (c, CommentPosition.Trailing));

    private static IEnumerable<(JavaComments.Comment c, CommentPosition Trailing)> GetLineEndComment(IEnumerable<JavaComments.Comment> comments, JavaParser.Position nodeEnd) =>
        comments
            .Where(c =>
            {
                var commentBegin = c.getBegin().FromOptional<JavaParser.Position>();
                return commentBegin != null && commentBegin.line == nodeEnd.line && commentBegin.column > nodeEnd.column;
            })
            .Select(c => (c, CommentPosition.Trailing));

    private static bool HasNextSibling(JavaAst.Node parentNode, JavaParser.Position nodeEnd)
    {
        return parentNode.getChildNodes()
            .OfType<JavaAst.Node>()
            .Where(sibling => sibling is not JavaComments.Comment)
            .Any(sibling =>
            {
                var siblingBegin = sibling.getBegin().FromOptional<JavaParser.Position>();
                return siblingBegin != null && (siblingBegin.line > nodeEnd.line || siblingBegin.line == nodeEnd.line && siblingBegin.column > nodeEnd.column);
            });
    }

    private static IEnumerable<(JavaComments.Comment c, CommentPosition Leading)> GetLeadingComments(IEnumerable<JavaComments.Comment> comments, JavaParser.Position nodeBegin, int previousPos)
    {
        return comments.Where(c =>
            {
                var commentBegin = c.getBegin().FromOptional<JavaParser.Position>();
                var commentEnd = c.getEnd().FromOptional<JavaParser.Position>();
                return commentBegin != null && commentEnd != null && commentBegin.line > previousPos && (commentEnd.line < nodeBegin.line || commentEnd.line == nodeBegin.line && commentEnd.column < nodeBegin.column);
            })
            .Select(c => (c, CommentPosition.Leading));
    }

    private static JavaAst.Node? GetPreviousSibling(JavaAst.Node parentNode, JavaParser.Position nodeBegin)
    {
        return parentNode.getChildNodes()
            .OfType<JavaAst.Node>()
            .Where(sibling => sibling is not JavaComments.Comment)
            .LastOrDefault(sibling =>
            {
                var siblingEnd = sibling.getEnd().FromOptional<JavaParser.Position>();
                return siblingEnd != null && (siblingEnd.line < nodeBegin.line || siblingEnd.line == nodeBegin.line && siblingEnd.column < nodeBegin.column);
            });
    }

    /// <summary>
    /// Convert `JavaAst.Node` code to Comments
    /// </summary>
    /// <param name="codes"></param>
    /// <param name="tag"></param>
    /// <param name="hasBlockMark"></param>
    /// <returns></returns>
    public static IEnumerable<SyntaxTrivia> ConvertToComment(IEnumerable<JavaAst.Node> codes, string tag, bool hasBlockMark = true)
    {
        var outputs = new List<string>();
        foreach (var code in codes)
        {
            string[] input = code.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            outputs.AddRange(input);
        }

        if (outputs.Count > 0)
        {
            if (hasBlockMark)
            {
                yield return SyntaxFactory.Comment(Environment.NewLine);
                yield return SyntaxFactory.Comment("// --------------------");
                yield return SyntaxFactory.Comment($"// TODO {tag}");
            }

            foreach (var t in outputs)
            {
                yield return SyntaxFactory.Comment($"// {t}");
            }

            if (hasBlockMark)
                yield return SyntaxFactory.Comment("// --------------------");
        }
    }

    private static IEnumerable<SyntaxTrivia> ConvertDocComment(JavaComments.Comment comment, string? post)
    {
        string[] input = comment.getContent().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        var output = new List<string>();
        var remarks = new List<string>(); // For Java tags unknown in C#
        var currentOutput = output;
        string? tag = null;
        foreach (string inputLine in input)
        {
            var match = _analyzeDocString.Match(inputLine);
            if (match.Success)
            {
                string paramName = match.Groups["param"].Value;
                string text = match.Groups["text"].Value;
                if (_knownTagsDict.TryGetValue(paramName, out var newTag))
                {
                    CloseSection(output, tag);
                    tag = newTag;
                    currentOutput = output;
                    OpenSection(output, tag, text);
                }
                else if (paramName.Length > 0)
                {
                    // Add other parameters to remarks section.
                    CloseSection(output, tag);
                    currentOutput = remarks;
                    remarks.Add(paramName + text);
                    tag = "remarks";
                }
                else if (tag == null)
                {
                    tag = "summary";
                    OpenSection(output, tag, text);
                }
                else
                {
                    currentOutput.Add(text); // Add additional text lines to the same section.
                }
            }
        }

        CloseSection(output, tag);

        AppendRemarks(output, remarks);
        if (output.Count > 0)
        {
            output[^1] += post;
            foreach (var t in output)
            {
                yield return SyntaxFactory.Comment("/// " + t);
            }
        }
    }

    private static void AppendRemarks(List<string> output, IList<string> remarks)
    {
        TrimTrailingEmptyLines(remarks);
        if (remarks.Count == 1)
        {
            remarks[0] = $"<remarks>{remarks[0]}</remarks>";
        }
        else if (remarks.Count > 1)
        {
            remarks.Insert(0, "<remarks>");
            remarks.Add("</remarks>");
        }

        output.AddRange(remarks);
    }

    private static void CloseSection(IList<string> output, string? tag)
    {
        if (output.Count > 0 && tag != "remarks")
        {
            TrimTrailingEmptyLines(output);
            string xmlEndTag = $"</{tag}>";
            if (tag == "summary")
            {
                // Summary tags are always on separate lines.
                output.Add(xmlEndTag);
            }
            else
            {
                output[output.Count - 1] += xmlEndTag;
            }
        }
    }

    private static void TrimTrailingEmptyLines(IList<string> lines)
    {
        while (lines.Count > 0 && lines[lines.Count - 1].Trim() == "")
        {
            lines.RemoveAt(lines.Count - 1);
        }
    }

    private static void OpenSection(ICollection<string> output, string tag, string text)
    {
        string id, label;
        switch (tag)
        {
            case "summary":
                output.Add("<summary>");
                if (text.Trim() != "")
                {
                    // Do not include the first empty line.
                    output.Add(text);
                }

                break;

            case "param": // <param name="id">label</param>
                (id, label) = ParseByFirstWord(text);
                output.Add($"<param name=\"{id}\">{label}");
                break;

            case "exception": // <exception cref="id">label</exception>
                (id, label) = ParseByFirstWord(text);
                output.Add($"<exception cref=\"{id}\">{label}");
                break;

            default:
                output.Add($"<{tag}>{text}");
                break;
        }

        static (string id, string label) ParseByFirstWord(string text)
        {
            string id = text.Split()[0];
            string label = text[id.Length..].TrimStart();
            return (id, label);
        }
    }

    public static SyntaxNode FixCommentsWhitespaces(SyntaxNode node)
    {
        // Comments are inserted before whitespace normalization. The following fixes must be executed after
        // whitespace normalization to be effective.

        if (node.HasLeadingTrivia)
        {
            node = InsertEmptyLineBeforeComment(node);
            node = AdjustBlockCommentIndentation(node);
        }

        return node;

        static SyntaxNode InsertEmptyLineBeforeComment(SyntaxNode node)
        {
            /* For increased readability we change this
             *
             *    DoSomething();
             *    // Comment
             *    DoSomethingElse();
             *
             * to this
             *
             *    DoSomething();
             *
             *    // Comment
             *    DoSomethingElse();
             */
            if (node is StatementSyntax statement)
            {
                var leading = node.GetLeadingTrivia();
                var index = leading.IndexOf(SyntaxKind.SingleLineCommentTrivia);
                if (index >= 0)
                {
                    if (index > 0 && leading[index - 1].IsKind(SyntaxKind.WhitespaceTrivia))
                    {
                        index--;
                    }

                    node = statement.InsertTriviaBefore(leading[index],
                        Enumerable.Repeat(SyntaxFactory.CarriageReturnLineFeed, 1));
                }
            }

            return node;
        }
    }

    private static SyntaxNode AdjustBlockCommentIndentation(SyntaxNode node)
    {
        // The first line of multiline comments is adjusted by the whitespace normalization but not the
        // following lines.
        var leading = node.GetLeadingTrivia();
        for (int i = 0; i < leading.Count; i++)
        {
            var t = leading[i];
            if (t.IsKind(SyntaxKind.MultiLineCommentTrivia))
            {
                int indentation = GetIndentation(leading, i) + 1; // Add one to align stars.
                string[] lines = t.ToFullString().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                string indentString = new(' ', indentation);
                for (int l = 1; l < lines.Length; l++)
                {
                    // Skip first line with "/*"
                    lines[l] = indentString + lines[l].TrimStart();
                }

                node = node.ReplaceTrivia(t, SyntaxFactory.Comment(String.Join(Environment.NewLine, lines).TrimEnd(' ')));
            }
        }

        return node;
    }

    private static int GetIndentation(SyntaxTriviaList leading, int commentIndex)
    {
        SyntaxTrivia whiteSpaceTrivia;
        if (commentIndex > 0 && leading[commentIndex - 1].IsKind(SyntaxKind.WhitespaceTrivia))
        {
            // Try to get the indentation from the whitespace leading the comment.
            whiteSpaceTrivia = leading[commentIndex - 1];
        }
        else if (leading.Last().IsKind(SyntaxKind.WhitespaceTrivia))
        {
            // Try to get the indentation of the node from the last leading trivia.
            whiteSpaceTrivia = leading.Last();
        }
        else
        {
            return 0;
        }

        string s = whiteSpaceTrivia.ToFullString().Replace("\t", "    ");

        return s.All(c => c == ' ') ? s.Length : 0;
    }
}
