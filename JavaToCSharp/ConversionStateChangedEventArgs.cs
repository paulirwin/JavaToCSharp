using System;

namespace JavaToCSharp;

public sealed class ConversionStateChangedEventArgs : EventArgs
{
    public ConversionStateChangedEventArgs(ConversionState newState)
    {
        NewState = newState;
    }

    public ConversionState NewState { get; }
}
