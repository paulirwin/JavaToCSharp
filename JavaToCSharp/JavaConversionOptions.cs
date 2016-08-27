using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace JavaToCSharp
{
	public class JavaConversionOptions
    {
        public JavaConversionOptions()
        {
            this.IncludeNamespace = true;
            this.IncludeUsings = true;
        }

        private readonly IList<Replacement> _packageReplacements = new List<Replacement>();

        public event EventHandler<ConversionWarningEventArgs> WarningEncountered;

        public event EventHandler<ConversionStateChangedEventArgs> StateChanged;
        
        private readonly IList<string> _usings = new List<string>
        {
            "System",
            "System.Collections.Generic",
            "System.Linq",
            "System.Text"
        };

        public IList<Replacement> PackageReplacements
        {
            get { return _packageReplacements; }
        }

        public IList<string> Usings
        {
            get { return _usings; }
        }

        public bool IncludeUsings { get; set; }

        public bool IncludeNamespace { get; set; }

        public bool UseDebugAssertForAsserts { get; set; }

        public ConversionState ConversionState { get; set; }

        public JavaConversionOptions AddPackageReplacement(string pattern, string replacement, RegexOptions options = RegexOptions.None)
        {
            _packageReplacements.Add(new Replacement(pattern, replacement, options));

            return this;
        }

        public JavaConversionOptions ClearUsings()
        {
            _usings.Clear();
            
            return this;
        }

        public JavaConversionOptions AddUsing(string ns)
        {
            _usings.Add(ns);

            return this;
        }

        internal void Warning(string message, int javaLineNumber)
        {
            var e = WarningEncountered;

            if (e != null)
            {
                e(this, new ConversionWarningEventArgs(message, javaLineNumber));
            }
        }

        internal void ConversionStateChanged(ConversionState newState)
        {
            this.ConversionState = newState;

            var e = StateChanged;

            if (e != null)
            {
                e(this, new ConversionStateChangedEventArgs(newState));
            }
        }
    }
}
