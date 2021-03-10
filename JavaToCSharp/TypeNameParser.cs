using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace JavaToCSharp
{
    public static class TypeNameParser
    {
        private enum TokenType
        {
            EndOfString = 0,
            Identifier,
            Extends,
            Super,
            LeftBracket,
            RightBracket,
            Comma,
            QuestionMark
        }

        private static readonly Regex _tokenizePattern = new Regex(@"\w+|<|>|,|\?", RegexOptions.Compiled);

        private static (string, TokenType)[] _tokens;
        private static (string text, TokenType type) _token;
        private static int _currentIndex;
        private static StringBuilder _sb = new();
        private static Func<string, string> _translate;

        internal static string ParseTypeName(string typename, Func<string, string> translateIdentifier)
        {
            // Example: List<Pair<Integer, ? extends Person>>
            //
            // EBNF:
            // TypeName = identifier [ "<" TypeArgument { "," TypeArgument } ">" ].
            // TypeArgument = [ "?" [ "extends" | "super" ] ] TypeName.

            _translate = translateIdentifier;
            _tokens = Tokenize(typename);
            _currentIndex = -1;
            NextToken();
            _sb.Clear();
            if (TypeName() && _token.type is TokenType.EndOfString) { // Otherwise we have extra tokens.
                return _sb.ToString();
            }
            return typename;
        }

        private static (string, TokenType)[] Tokenize(string typeName)
        {
            var matches = _tokenizePattern.Matches(typeName);
            var tokens = new (string, TokenType)[matches.Count];
            for (int i = 0; i < matches.Count; i++) {
                Match match = matches[i];
                string s = match.Value;
                tokens[i] = s switch {
                    "<" => (s, TokenType.LeftBracket),
                    ">" => (s, TokenType.RightBracket),
                    "," => (s, TokenType.Comma),
                    "?" => (s, TokenType.QuestionMark),
                    "extends" => (s, TokenType.Extends),
                    "super" => (s, TokenType.Super),
                    _ => (s, TokenType.Identifier)
                };
            }
            return tokens;
        }

        private static void NextToken()
        {
            _currentIndex++;
            if (_currentIndex < _tokens.Length) {
                _token = _tokens[_currentIndex];
            } else {
                _token = default;
            }
        }

        static bool TypeName()
        {
            // TypeName = identifier [ "<" TypeArgument { "," TypeArgument } ">" ].
            if (_token.type is TokenType.Identifier) {
                _sb.Append(_translate(_token.text));
                NextToken();
                if (_token.type is TokenType.LeftBracket) {
                    _sb.Append("<");
                    NextToken();
                    if (TypeArgument()) {
                        while (_token.type is TokenType.Comma) {
                            _sb.Append(", ");
                            NextToken();
                            if (!TypeArgument()) return false;
                        }
                        if (_token.type is TokenType.RightBracket) {
                            _sb.Append(">");
                            NextToken();
                            return true;
                        }
                    }
                } else {
                    return true;
                }
            }
            return false;
        }

        static bool TypeArgument()
        {
            // TypeArgument = [ "?" [ "extends" | "super" ] ] TypeName.
            if (_token.type is TokenType.QuestionMark) {
                // C# does not have this. Ignore. We could fix it by replacing IList<T> by IList for example.
                NextToken();
                if (_token.type is TokenType.Extends or TokenType.Super) {
                    NextToken();
                }
            }
            return TypeName();
        }
    }
}
