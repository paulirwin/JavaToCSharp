using System;

namespace JavaToCSharp;

public class ConversionWarningEventArgs : EventArgs
{
    public ConversionWarningEventArgs(string message, int javaLineNumber)
    {
        Message = message;
        JavaLineNumber = javaLineNumber;
    }

    public string Message { get; }

    public int JavaLineNumber { get; }
}
