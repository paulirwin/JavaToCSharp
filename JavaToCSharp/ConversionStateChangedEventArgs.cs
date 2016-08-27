using System;

namespace JavaToCSharp
{
	public sealed class ConversionStateChangedEventArgs : EventArgs
    {
        public ConversionStateChangedEventArgs(ConversionState newState)
        {
            this.NewState = newState;
        }

        public ConversionState NewState { get; private set; }
    }
}
