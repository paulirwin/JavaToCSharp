using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp;

public class ConversionContext(JavaConversionOptions options)
{
    public Queue<ClassDeclarationSyntax> PendingAnonymousTypes { get; } = new();

    public ISet<string> UsedAnonymousTypeNames { get; } = new HashSet<string>();

    public ISet<string> StaticUsingEnumNames { get; } = new HashSet<string>();

    public JavaConversionOptions Options { get; } = options;

    public ConversionState ConversionState { get; private set; }

    public string? RootTypeName { get; set; }

    public string? LastTypeName { get; set; }

    /// <summary>
    /// Statements that must be emitted immediately before the statement currently being visited.
    /// Used to lower constructs that cannot be expressed as a single statement, such as multi-statement
    /// switch expressions. Drained by <see cref="Statements.StatementVisitor.VisitStatements"/>.
    /// </summary>
    internal List<StatementSyntax> PendingStatements { get; } = [];

    /// <summary>
    /// The identifier that a Java <c>yield</c> statement should assign to, when a multi-statement
    /// switch expression has been lowered into a switch statement. Null when not inside such a lowering.
    /// </summary>
    internal string? YieldTarget { get; set; }

    private int _uniqueLocalCounter;

    /// <summary>
    /// Creates a local variable name that will not collide with other generated locals.
    /// </summary>
    internal string CreateUniqueLocalName(string prefix) => $"__{prefix}{_uniqueLocalCounter++}";

    /// <summary>
    /// Records the new conversion state and raises <see cref="JavaConversionOptions.StateChanged"/>.
    /// </summary>
    internal void ConversionStateChanged(ConversionState newState)
    {
        ConversionState = newState;

        Options.ConversionStateChanged(newState);
    }
}
