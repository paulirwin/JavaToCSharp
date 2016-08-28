using System.Text.RegularExpressions;

namespace JavaToCSharp
{
    public class Replacement
    {
        private readonly Regex _regex;
        private readonly string _replacement;

        public Replacement(string pattern, string replacement, RegexOptions options = RegexOptions.None)
        {
            _regex = new Regex(pattern, options);
            _replacement = replacement;
        }

        public Regex Regex
        {
            get { return _regex; }
        }

        public string ReplacementValue
        {
            get { return _replacement; }
        }

        public string Replace(string input)
        {
            return _regex.Replace(input, _replacement);
        }

        public override bool Equals(object obj)
        {
            Replacement r = obj as Replacement;

            if (obj == null) return false;

            return r.Regex.Equals(_regex) && string.Equals(r.ReplacementValue, _replacement);
        }

        public override int GetHashCode()
        {
            const int prime = 23;

            int i = 17;

            i += prime * _regex.GetHashCode();
            i += prime * _replacement.GetHashCode();

            return i;
        }
    }
}
