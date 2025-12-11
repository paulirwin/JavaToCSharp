namespace JavaToCSharp;

public class ConversionWarningEventArgs(string message, int javaLineNumber) : EventArgs
{
    public string Message { get; } = message;

    public int JavaLineNumber { get; } = javaLineNumber;
}
