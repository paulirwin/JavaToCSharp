using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp;

public class ConversionContext(JavaConversionOptions options)
{
    /// <summary>
    /// Types that must be hoisted to the enclosing type, because C# has no local equivalent of the
    /// Java construct that declared them: anonymous class bodies and local records.
    /// </summary>
    public Queue<MemberDeclarationSyntax> PendingAnonymousTypes { get; } = new();

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
    /// Instance initializer blocks collected while visiting the members of the class currently being
    /// converted. Java runs these at the start of every constructor, so they are prepended to each
    /// constructor body by <see cref="Declarations.ClassOrInterfaceDeclarationVisitor"/>.
    /// </summary>
    internal List<BlockSyntax> PendingInstanceInitializers { get; } = [];

    /// <summary>
    /// The identifier that a Java <c>yield</c> statement should assign to, when a multi-statement
    /// switch expression has been lowered into a switch statement. Null when not inside such a lowering.
    /// </summary>
    internal string? YieldTarget { get; set; }

    /// <summary>
    /// Java labels targeted by a labeled <c>break</c> that was lowered to a <c>goto</c>. The labeled statement
    /// visitor emits a matching target label after the loop for each one.
    /// </summary>
    internal ISet<string> LabelsNeedingBreakTarget { get; } = new HashSet<string>();

    /// <summary>
    /// Java labels targeted by a labeled <c>continue</c> that was lowered to a <c>goto</c>. The labeled
    /// statement visitor emits a matching target label at the end of the loop body for each one.
    /// </summary>
    internal ISet<string> LabelsNeedingContinueTarget { get; } = new HashSet<string>();

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
