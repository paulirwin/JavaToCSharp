using System.CommandLine;
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

    private static readonly Option<bool> _includeSubdirectoriesOption = new("--include-subdirectories")
    {
        Description = "When the command is dir, converts files in all subdirectories",
        DefaultValueFactory = _ => true,
    };

    private static readonly Option<bool> _includeUsingsOption = new("--include-usings")
    {
        Description = "Include using directives in output",
        DefaultValueFactory = _ => true,
    };

    private static readonly Option<bool> _includeNamespaceOption = new("--include-namespace")
    {
        Description = "Include namespace in output",
        DefaultValueFactory = _ => true,
    };

    private static readonly Option<bool> _includeCommentsOption = new("--include-comments")
    {
        Description = "Include comments in output",
        DefaultValueFactory = _ => true,
    };

    private static readonly Option<bool> _useDebugAssertOption = new("--use-debug-assert")
    {
        Description = "Use Debug.Assert for asserts",
        DefaultValueFactory = _ => false,
    };

    private static readonly Option<bool> _startInterfaceNamesWithIOption = new("--start-interface-names-with-i")
    {
        Description = "Prefix interface names with the letter I",
        DefaultValueFactory = _ => true,
    };

    private static readonly Option<bool> _commentUnrecognizedCodeOption = new("--comment-unrecognized-code")
    {
        Description = "Include unrecognized code in output as commented-out code",
        DefaultValueFactory = _ => true,
    };

    private static readonly Option<bool> _systemOutToConsoleOption = new("--system-out-to-console")
    {
        Description = "Convert System.out calls to Console",
        DefaultValueFactory = _ => false,
    };

    private static readonly Option<bool> _fileScopedNamespacesOption = new("--file-scoped-namespaces")
    {
        Description = "Use file-scoped namespaces in C# output",
        DefaultValueFactory = _ => false,
    };

    private static readonly Option<bool> _clearDefaultUsingsOption = new("--clear-usings")
    {
        Description = "Remove all default usings provided by this app",
        DefaultValueFactory = _ => false,
    };

    private static readonly Option<List<string>> _addUsingsOption = new("--add-using")
    {
        Description = "Adds a using directive to the collection of usings",
        HelpName = "namespace",
    };

    private static readonly Option<string> _mappingsFileNameOption = new("--mappings-file")
    {
        Description = "A yaml file with syntax mappings from imports, methods and annotations",
    };

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
            Description = "A syntactic transformer of source code from Java to C#.",
        };

        rootCommand.Subcommands.Add(CreateFileCommand());
        rootCommand.Subcommands.Add(CreateDirectoryCommand());

        rootCommand.Options.Add(_includeSubdirectoriesOption);
        rootCommand.Options.Add(_includeUsingsOption);
        rootCommand.Options.Add(_includeNamespaceOption);
        rootCommand.Options.Add(_includeCommentsOption);
        rootCommand.Options.Add(_useDebugAssertOption);
        rootCommand.Options.Add(_startInterfaceNamesWithIOption);
        rootCommand.Options.Add(_commentUnrecognizedCodeOption);
        rootCommand.Options.Add(_systemOutToConsoleOption);
        rootCommand.Options.Add(_fileScopedNamespacesOption);
        rootCommand.Options.Add(_clearDefaultUsingsOption);
        rootCommand.Options.Add(_addUsingsOption);
        rootCommand.Options.Add(_mappingsFileNameOption);

        await rootCommand.Parse(args).InvokeAsync();

        // flush logs
        _loggerFactory.Dispose();
    }

    private static Command CreateFileCommand()
    {
        var inputArgument = new Argument<FileInfo>("input")
        {
            Description = "A Java source code file to convert",
        };

        var outputArgument = new Argument<FileInfo?>("output")
        {
            Description = "Path to place the C# output file, or stdout if omitted",
            DefaultValueFactory = _ => null,
        };

        var fileCommand = new Command("file", "Convert a Java file to C#")
        {
            inputArgument,
            outputArgument,
        };

        fileCommand.SetAction(context =>
        {
            var input = context.GetRequiredValue(inputArgument);
            var output = context.GetValue(outputArgument);

            var options = GetJavaConversionOptions(context);

            ConvertToCSharpFile(input, output, options);
        });

        return fileCommand;
    }

    private static JavaConversionOptions GetJavaConversionOptions(ParseResult context)
    {
        var options = new JavaConversionOptions
        {
            IncludeSubdirectories = context.GetValue(_includeSubdirectoriesOption),
            IncludeUsings = context.GetValue(_includeUsingsOption),
            IncludeComments = context.GetValue(_includeCommentsOption),
            IncludeNamespace = context.GetValue(_includeNamespaceOption),
            ConvertSystemOutToConsole = context.GetValue(_systemOutToConsoleOption),
            StartInterfaceNamesWithI = context.GetValue(_startInterfaceNamesWithIOption),
            UseDebugAssertForAsserts = context.GetValue(_useDebugAssertOption),
            UseUnrecognizedCodeToComment = context.GetValue(_commentUnrecognizedCodeOption),
            UseFileScopedNamespaces = context.GetValue(_fileScopedNamespacesOption),
        };

        if (context.GetValue(_clearDefaultUsingsOption))
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

        foreach (string ns in context.GetValue(_addUsingsOption) ?? [])
        {
            options.AddUsing(ns);
        }

        var mappingsFile = context.GetValue(_mappingsFileNameOption);
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
        var inputArgument = new Argument<DirectoryInfo>("input")
        {
            Description = "A directory containing Java source code files to convert",
        };

        var outputArgument = new Argument<DirectoryInfo>("output")
        {
            Description = "Path to place the C# output files",
        };

        var dirCommand = new Command("dir", "Convert a directory containing Java files to C#")
        {
            inputArgument,
            outputArgument,
        };

        dirCommand.SetAction(context =>
        {
            var input = context.GetRequiredValue(inputArgument);
            var output = context.GetRequiredValue(outputArgument);

            var options = GetJavaConversionOptions(context);

            ConvertToCSharpDir(input, output, options);
        });

        return dirCommand;
    }

    private static void ConvertToCSharpDir(DirectoryInfo inputDirectory, DirectoryInfo outputDirectory, JavaConversionOptions options)
    {
        if (inputDirectory.Exists)
        {
            var searchOption = options.IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            foreach (var f in inputDirectory.GetFiles("*.java", searchOption))
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
        {
            _logger.LogError("Java input folder {path} doesn't exist!", inputDirectory);
        }
    }

    private static void ConvertToCSharpFile(FileSystemInfo inputFile, FileSystemInfo? outputFile, JavaConversionOptions options, bool overwrite = true)
    {
        if (!overwrite && outputFile is { Exists: true })
        {
            _logger.LogInformation("{outputFilePath} exists, skip to next.", outputFile);
        }
        else if (inputFile.Exists)
        {
            try
            {
                string javaText = File.ReadAllText(inputFile.FullName);

                void warningEncountered(object? sender, ConversionWarningEventArgs eventArgs)
                {
                    if (outputFile != null)
                    {
                        _logger.LogWarning("Line {JavaLineNumber}: {Message}", eventArgs.JavaLineNumber,
                            eventArgs.Message);
                    }

                    OutputFileOrPrint(outputFile != null ? Path.ChangeExtension(outputFile.FullName, ".warning") : null,
                        eventArgs.Message + Environment.NewLine);
                }

                options.WarningEncountered += warningEncountered;

                try
                {
                    string? parsed = JavaToCSharpConverter.ConvertText(javaText, options);
                    OutputFileOrPrint(outputFile?.FullName, parsed ?? string.Empty);
                }
                finally
                {
                    options.WarningEncountered -= warningEncountered;
                }

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
        {
            _logger.LogError("Java input file {filePath} doesn't exist!", inputFile.FullName);
        }
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
