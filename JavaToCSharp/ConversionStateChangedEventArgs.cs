using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
