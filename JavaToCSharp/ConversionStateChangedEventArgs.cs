namespace JavaToCSharp;

public sealed class ConversionStateChangedEventArgs(ConversionState newState) : EventArgs
{
    public ConversionState NewState { get; } = newState;
}
