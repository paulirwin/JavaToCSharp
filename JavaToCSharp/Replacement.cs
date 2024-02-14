using System.Text.RegularExpressions;

namespace JavaToCSharp;

public class Replacement
{
    public Replacement(string pattern, string replacement, RegexOptions options = RegexOptions.None)
    {
        Regex = new Regex(pattern, options);
        ReplacementValue = replacement;
    }

    public Regex? Regex { get; }

    public string? ReplacementValue { get; }

    public string? Replace(string input) => string.IsNullOrWhiteSpace(ReplacementValue) ? null : Regex?.Replace(input, ReplacementValue);

    protected bool Equals(Replacement other)
    {
        return Equals(Regex, other.Regex) && ReplacementValue == other.ReplacementValue;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Replacement) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((Regex != null ? Regex.GetHashCode() : 0) * 397) ^ (ReplacementValue != null ? ReplacementValue.GetHashCode() : 0);
        }
    }
}
