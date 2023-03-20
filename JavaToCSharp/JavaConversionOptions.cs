using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace JavaToCSharp
{
    public class JavaConversionOptions
    {
        public JavaConversionOptions()
        {
            IncludeNamespace = true;
            IncludeUsings = true;
            UseUnrecognizedCodeToComment = true;
        }

        public event EventHandler<ConversionWarningEventArgs>? WarningEncountered;

        public event EventHandler<ConversionStateChangedEventArgs>? StateChanged;

        public IList<Replacement> PackageReplacements { get; } = new List<Replacement>();

        public IList<string> Usings { get; } = new List<string>
        {
            "System",
            "System.Collections.Generic",
            "System.Linq",
            "System.Text"
        };

        public bool IncludeUsings { get; set; }

        public bool IncludeNamespace { get; set; }

        public bool UseDebugAssertForAsserts { get; set; }

        /// <summary>
        /// Unrecognized code is translated into comments
        /// </summary>
        public bool UseUnrecognizedCodeToComment { get; set; }

        public ConversionState ConversionState { get; set; }

        public JavaConversionOptions AddPackageReplacement(string pattern, string replacement, RegexOptions options = RegexOptions.None)
        {
            PackageReplacements.Add(new Replacement(pattern, replacement, options));

            return this;
        }

        public JavaConversionOptions ClearUsings()
        {
            Usings.Clear();

            return this;
        }

        public JavaConversionOptions AddUsing(string ns)
        {
            Usings.Add(ns);

            return this;
        }

        internal void Warning(string message, int javaLineNumber) => WarningEncountered?.Invoke(this, new ConversionWarningEventArgs(message, javaLineNumber));

        internal void ConversionStateChanged(ConversionState newState)
        {
            ConversionState = newState;

            StateChanged?.Invoke(this, new ConversionStateChangedEventArgs(newState));
        }
    }
}