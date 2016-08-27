using System;

namespace JavaToCSharp
{
	public class ConversionWarningEventArgs : EventArgs
    {
        public ConversionWarningEventArgs(string message, int javaLineNumber)
        {
            this.Message = message;
            this.JavaLineNumber = javaLineNumber;
        }

        public string Message { get; private set; }

        public int JavaLineNumber { get; private set; }
    }
}
