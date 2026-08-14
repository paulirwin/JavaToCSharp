using System.Text;
using System.Text.RegularExpressions;

namespace JavaToCSharp;

/// <summary>
/// Parses and translates Java type names into their C# equivalents.
/// </summary>
/// <remarks>
/// All parse state is held in instance fields, and a fresh instance is created per
/// <see cref="ParseTypeName"/> call, so concurrent conversions cannot corrupt each other.
/// </remarks>
public sealed partial class TypeNameParser
{
    private enum TokenType
    {
        EndOfString = 0,
        Identifier,
        Extends,
        Super,
        LeftSquareBracket,
        RightSquareBracket,
        LeftAngleBracket,
        RightAngleBracket,
        Comma,
        QuestionMark
    }

    [GeneratedRegex(@"\w+|\[|\]|<|>|,|\?", RegexOptions.Compiled)]
    private static partial Regex TokenizePattern { get; }

    private readonly (string, TokenType)[] _tokens;
    private readonly StringBuilder _sb = new();
    private readonly Func<string, string> _translate;
    private (string text, TokenType type) _token;
    private int _currentIndex;

    private TypeNameParser(string typename, Func<string, string> translateIdentifier)
    {
        _translate = translateIdentifier;
        _tokens = Tokenize(typename);
        _currentIndex = -1;
        NextToken();
    }

    internal static string ParseTypeName(string typename, Func<string, string> translateIdentifier)
    {
        // Example: List<Pair<Integer, ? extends Person>>
        //
        // EBNF:
        // TypeName = identifier [ "<" TypeArgument { "," TypeArgument } ">" ] { "[" "]" }.
        // TypeArgument = [ "?" [ "extends" | "super" ] ] TypeName.

        var parser = new TypeNameParser(typename, translateIdentifier);

        if (parser.TypeName() && parser._token.type is TokenType.EndOfString)
        {
            // Otherwise we have extra tokens.
            return parser._sb.ToString();
        }

        return typename;
    }

    private static (string, TokenType)[] Tokenize(string typeName)
    {
        var matches = TokenizePattern.Matches(typeName);
        var tokens = new (string, TokenType)[matches.Count];
        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            string s = match.Value;
            tokens[i] = s switch
            {
                "[" => (s, TokenType.LeftSquareBracket),
                "]" => (s, TokenType.RightSquareBracket),
                "<" => (s, TokenType.LeftAngleBracket),
                ">" => (s, TokenType.RightAngleBracket),
                "," => (s, TokenType.Comma),
                "?" => (s, TokenType.QuestionMark),
                "extends" => (s, TokenType.Extends),
                "super" => (s, TokenType.Super),
                _ => (s, TokenType.Identifier)
            };
        }
        return tokens;
    }

    private void NextToken()
    {
        _currentIndex++;
        _token = _currentIndex < _tokens.Length ? _tokens[_currentIndex] : default;
    }

    private bool TypeName()
    {
        // TypeName = identifier [ "<" TypeArgument { "," TypeArgument } ">" ] { "[" "]" }.
        if (_token.type is not TokenType.Identifier)
        {
            return false;
        }

        _sb.Append(_translate(_token.text));
        NextToken();

        if (_token.type is TokenType.LeftAngleBracket)
        {
            _sb.Append('<');
            NextToken();
            if (!TypeArgument()) return false;

            while (_token.type is TokenType.Comma)
            {
                _sb.Append(", ");
                NextToken();
                if (!TypeArgument()) return false;
            }

            if (_token.type is not TokenType.RightAngleBracket) return false;

            _sb.Append('>');
            NextToken();
        }

        return ArraySuffix();
    }

    private bool ArraySuffix()
    {
        while (_token.type is TokenType.LeftSquareBracket)
        {
            NextToken();
            if (_token.type is not TokenType.RightSquareBracket) return false;

            _sb.Append("[]");
            NextToken();
        }

        return true;
    }

    private bool TypeArgument()
    {
        // TypeArgument = [ "?" [ "extends" | "super" ] ] TypeName.
        if (_token.type is TokenType.QuestionMark)
        {
            _sb.Append("TWildcardTodo");
            NextToken();

            if (_token.type is TokenType.Extends or TokenType.Super)
            {
                NextToken();
            }
            else if (_token.type is TokenType.RightAngleBracket)
            {
                return true;
            }
        }
        return TypeName();
    }
}
