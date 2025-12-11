using System.Text.RegularExpressions;

namespace JavaToCSharp;

public class Replacement(string pattern, string replacement, RegexOptions options = RegexOptions.None)
{
    public Regex Regex { get; } = new(pattern, options);

    public string? ReplacementValue { get; } = replacement;

    public string Replace(string input)
        => string.IsNullOrWhiteSpace(ReplacementValue)
        ? string.Empty
        : Regex.Replace(input, ReplacementValue);

    protected bool Equals(Replacement other)
        => Equals(Regex, other.Regex) && ReplacementValue == other.ReplacementValue;

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Replacement) obj);
    }

    public override int GetHashCode()
        => HashCode.Combine(Regex, ReplacementValue);
}
