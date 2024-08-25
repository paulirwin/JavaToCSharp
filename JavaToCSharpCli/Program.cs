using System.CommandLine;
using System.CommandLine.Invocation;
using JavaToCSharp;
using Microsoft.Extensions.Logging;

namespace JavaToCSharpCli;

/// <summary>
/// The main JavaToCSharpCli program class.
/// </summary>
public class Program
{
    private static readonly ILoggerFactory _loggerFactory;
    private static readonly ILogger _logger;

    private static readonly Option<bool> _includeUsingsOption = new(
        name: "--include-usings",
        description: "Include using directives in output",
        getDefaultValue: () => true);

    private static readonly Option<bool> _includeNamespaceOption = new(
        name: "--include-namespace",
        description: "Include namespace in output",
        getDefaultValue: () => true);

    private static readonly Option<bool> _includeCommentsOption = new(
        name: "--include-comments",
        description: "Include comments in output",
        getDefaultValue: () => true);

    private static readonly Option<bool> _useDebugAssertOption = new(
        name: "--use-debug-assert",
        description: "Use Debug.Assert for asserts",
        getDefaultValue: () => false);

    private static readonly Option<bool> _startInterfaceNamesWithIOption = new(
        name: "--start-interface-names-with-i",
        description: "Prefix interface names with the letter I",
        getDefaultValue: () => true);

    private static readonly Option<bool> _commentUnrecognizedCodeOption = new(
        name: "--comment-unrecognized-code",
        description: "Include unrecognized code in output as commented-out code",
        getDefaultValue: () => true);

    private static readonly Option<bool> _systemOutToConsoleOption = new(
        name: "--system-out-to-console",
        description: "Convert System.out calls to Console",
        getDefaultValue: () => false);

    private static readonly Option<bool> _fileScopedNamespacesOption = new(
        name: "--file-scoped-namespaces",
        description: "Use file-scoped namespaces in C# output",
        getDefaultValue: () => false);

    private static readonly Option<bool> _clearDefaultUsingsOption = new(
        name: "--clear-usings",
        description: "Remove all default usings provided by this app",
        getDefaultValue: () => false);

    private static readonly Option<List<string>> _addUsingsOption = new(
        name: "--add-using",
        description: "Adds a using directive to the collection of usings")
    {
        ArgumentHelpName = "namespace"
    };

    private static readonly Option<string> _mappingsFileNameOption = new(
        name: "--mappings-file",
        description: "A yaml file with syntax mappings from imports, methods and annotations");

    static Program()
    {
        _loggerFactory = LoggerFactory.Create(builder =>
            builder.AddSimpleConsole().SetMinimumLevel(LogLevel.Information));
        _logger = _loggerFactory.CreateLogger<Program>();
    }

    public static async Task Main(string[] args)
    {
        var rootCommand = new RootCommand("Java to C# Converter")
        {
            Description = "A syntactic transformer of source code from Java to C#."
        };

        rootCommand.AddCommand(CreateFileCommand());
        rootCommand.AddCommand(CreateDirectoryCommand());

        rootCommand.AddGlobalOption(_includeUsingsOption);
        rootCommand.AddGlobalOption(_includeNamespaceOption);
        rootCommand.AddGlobalOption(_includeCommentsOption);
        rootCommand.AddGlobalOption(_useDebugAssertOption);
        rootCommand.AddGlobalOption(_startInterfaceNamesWithIOption);
        rootCommand.AddGlobalOption(_commentUnrecognizedCodeOption);
        rootCommand.AddGlobalOption(_systemOutToConsoleOption);
        rootCommand.AddGlobalOption(_fileScopedNamespacesOption);
        rootCommand.AddGlobalOption(_clearDefaultUsingsOption);
        rootCommand.AddGlobalOption(_addUsingsOption);
        rootCommand.AddGlobalOption(_mappingsFileNameOption);

        await rootCommand.InvokeAsync(args);

        // flush logs
        _loggerFactory.Dispose();
    }

    private static Command CreateFileCommand()
    {
        var inputArgument = new Argument<FileInfo>(
            name: "input",
            description: "A Java source code file to convert");

        var outputArgument = new Argument<FileInfo?>(
            name: "output",
            description: "Path to place the C# output file, or stdout if omitted",
            getDefaultValue: () => null);

        var fileCommand = new Command("file", "Convert a Java file to C#");
        fileCommand.AddArgument(inputArgument);
        fileCommand.AddArgument(outputArgument);

        fileCommand.SetHandler(context =>
        {
            var input = context.ParseResult.GetValueForArgument(inputArgument);
            var output = context.ParseResult.GetValueForArgument(outputArgument);

            var options = GetJavaConversionOptions(context);

            ConvertToCSharpFile(input, output, options);
        });

        return fileCommand;
    }

    private static JavaConversionOptions GetJavaConversionOptions(InvocationContext context)
    {
        var options = new JavaConversionOptions
        {
            IncludeUsings = context.ParseResult.GetValueForOption(_includeUsingsOption),
            IncludeComments = context.ParseResult.GetValueForOption(_includeCommentsOption),
            IncludeNamespace = context.ParseResult.GetValueForOption(_includeNamespaceOption),
            ConvertSystemOutToConsole = context.ParseResult.GetValueForOption(_systemOutToConsoleOption),
            StartInterfaceNamesWithI = context.ParseResult.GetValueForOption(_startInterfaceNamesWithIOption),
            UseDebugAssertForAsserts = context.ParseResult.GetValueForOption(_useDebugAssertOption),
            UseUnrecognizedCodeToComment = context.ParseResult.GetValueForOption(_commentUnrecognizedCodeOption),
            UseFileScopedNamespaces = context.ParseResult.GetValueForOption(_fileScopedNamespacesOption),
        };

        if (context.ParseResult.GetValueForOption(_clearDefaultUsingsOption))
        {
            options.ClearUsings();
        }
        else
        {
            options.AddUsing("System");
            options.AddUsing("System.Collections.Generic");
            options.AddUsing("System.Collections.ObjectModel");
            options.AddUsing("System.Linq");
            options.AddUsing("System.Text");
        }

        foreach (string ns in context.ParseResult.GetValueForOption(_addUsingsOption) ?? new List<string>())
        {
            options.AddUsing(ns);
        }

        var mappingsFile = context.ParseResult.GetValueForOption(_mappingsFileNameOption);
        if (!string.IsNullOrEmpty(mappingsFile))
        {
            options.SyntaxMappings = ReadMappingsFile(mappingsFile);
        }

        return options;
    }

    private static SyntaxMapping ReadMappingsFile(string mappingsFile)
    {
        // Let fail if cannot be read or deserialized to display the exception message in the CLI
        var mappingsStr = File.ReadAllText(mappingsFile);
        return SyntaxMapping.Deserialize(mappingsStr);
    }

    private static Command CreateDirectoryCommand()
    {
        var inputArgument = new Argument<DirectoryInfo>(
            name: "input",
            description: "A directory containing Java source code files to convert");

        var outputArgument = new Argument<DirectoryInfo>(
            name: "output",
            description: "Path to place the C# output files");

        var dirCommand = new Command("dir", "Convert a directory containing Java files to C#");
        dirCommand.AddArgument(inputArgument);
        dirCommand.AddArgument(outputArgument);

        dirCommand.SetHandler(context =>
        {
            var input = context.ParseResult.GetValueForArgument(inputArgument);
            var output = context.ParseResult.GetValueForArgument(outputArgument);

            var options = GetJavaConversionOptions(context);

            ConvertToCSharpDir(input, output, options);
        });

        return dirCommand;
    }

    private static void ConvertToCSharpDir(DirectoryInfo inputDirectory, DirectoryInfo outputDirectory, JavaConversionOptions options)
    {
        if (inputDirectory.Exists)
        {
            foreach (var f in inputDirectory.GetFiles("*.java", SearchOption.AllDirectories))
            {
                string? directoryName = f.DirectoryName;
                if (string.IsNullOrWhiteSpace(directoryName))
                {
                    continue;
                }

                if (!outputDirectory.Exists)
                {
                    outputDirectory.Create();
                }

                ConvertToCSharpFile(f,
                    new FileInfo(Path.Combine(outputDirectory.FullName, Path.ChangeExtension(f.Name, ".cs"))),
                    options,
                    false);
            }
        }
        else
            _logger.LogError("Java input folder {path} doesn't exist!", inputDirectory);
    }

    private static void ConvertToCSharpFile(FileSystemInfo inputFile, FileSystemInfo? outputFile, JavaConversionOptions options, bool overwrite = true)
    {
        if (!overwrite && outputFile is { Exists: true })
            _logger.LogInformation("{outputFilePath} exists, skip to next.", outputFile);
        else if (inputFile.Exists)
        {
            try
            {
                string javaText = File.ReadAllText(inputFile.FullName);

                options.WarningEncountered += (_, eventArgs) =>
                {
                    if (outputFile != null)
                    {
                        _logger.LogWarning("Line {JavaLineNumber}: {Message}", eventArgs.JavaLineNumber,
                            eventArgs.Message);
                    }

                    OutputFileOrPrint(outputFile != null ? Path.ChangeExtension(outputFile.FullName, ".warning") : null,
                        eventArgs.Message + Environment.NewLine);
                };

                string? parsed = JavaToCSharpConverter.ConvertText(javaText, options);
                OutputFileOrPrint(outputFile?.FullName, parsed ?? string.Empty);

                if (outputFile != null)
                {
                    _logger.LogInformation("{filePath} converted!", inputFile.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{filePath} failed! {type}: {message}", inputFile.Name, ex.GetType().Name, ex);

                if (outputFile != null)
                {
                    File.WriteAllText(Path.ChangeExtension(outputFile.FullName, ".error"), ex.ToString());
                }
            }
        }
        else
            _logger.LogError("Java input file {filePath} doesn't exist!", inputFile.FullName);
    }

    private static void OutputFileOrPrint(string? fileName, string contents)
    {
        if (fileName == null)
        {
            Console.Out.WriteLine(contents);
        }
        else
        {
            File.WriteAllText(fileName, contents);
        }
    }
}
