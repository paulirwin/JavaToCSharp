using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace JavaToCSharp;

public class JavaConversionOptions
{
    public event EventHandler<ConversionWarningEventArgs>? WarningEncountered;

    public event EventHandler<ConversionStateChangedEventArgs>? StateChanged;

    public IList<Replacement> PackageReplacements { get; } = new List<Replacement>();

    public IList<string> Usings { get; } = new List<string>
    {
        "System",
        "System.Collections.Generic",
        "System.Collections.ObjectModel",
        "System.Linq",
        "System.Text"
    };

    public bool IncludeUsings { get; set; } = true;

    public bool IncludeNamespace { get; set; } = true;

    public bool UseDebugAssertForAsserts { get; set; }
    
    public bool StartInterfaceNamesWithI { get; set; }

    /// <summary>
    /// Unrecognized code is translated into comments
    /// </summary>
    public bool UseUnrecognizedCodeToComment { get; set; } = true;
    
    public bool ConvertSystemOutToConsole { get; set; }

    public bool IncludeComments { get; set; } = true;

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