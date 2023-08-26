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

    static Program()
    {
        _loggerFactory = LoggerFactory.Create(builder =>
            builder.AddSimpleConsole().SetMinimumLevel(LogLevel.Information));
        _logger = _loggerFactory.CreateLogger<Program>();
    }

    public static async Task Main(string[] args)
    {
        var inputOption = new Option<FileSystemInfo?>(
            name: "--input",
            description: "A Java source code file or directory to convert");

        var outputOption = new Option<FileSystemInfo?>(
            name: "--output",
            description: "Path to a file or directory for the C# output");

        var rootCommand = new RootCommand("Java to C# Converter");
        rootCommand.AddOption(inputOption);
        rootCommand.AddOption(outputOption);

        rootCommand.SetHandler((input, output) =>
            {
                if (input is FileInfo inputFile)
                {
                    if (output is not FileInfo outputFile)
                    {
                        _logger.LogError("Expected a file, not a directory, as output");
                        return;
                    }

                    ConvertToCSharpFile(inputFile, outputFile);
                }
                else if (input is DirectoryInfo inputDirectory)
                {
                    if (output is not DirectoryInfo outputDirectory)
                    {
                        _logger.LogError("Expected a directory, not a file, as output");
                        return;
                    }

                    ConvertToCSharpDir(inputDirectory, outputDirectory);
                }
            },
            inputOption, outputOption);

        await rootCommand.InvokeAsync(args);

        // flush logs
        _loggerFactory.Dispose();
    }

    private static void ConvertToCSharpDir(DirectoryInfo inputDirectory, DirectoryInfo outputDirectory)
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
                    false);
            }
        }
        else
            _logger.LogError("Java input folder {path} doesn't exist!", inputDirectory);
    }

    private static void ConvertToCSharpFile(FileSystemInfo inputFile, FileSystemInfo outputFile, bool overwrite = true)
    {
        if (!overwrite && outputFile.Exists)
            _logger.LogInformation("{outputFilePath} exists, skip to next.", outputFile);
        else if (inputFile.Exists)
        {
            try
            {
                string javaText = File.ReadAllText(inputFile.FullName);
                var options = new JavaConversionOptions();

                options.WarningEncountered += (_, eventArgs) =>
                {
                    _logger.LogWarning("Line {JavaLineNumber}: {Message}", eventArgs.JavaLineNumber, eventArgs.Message);
                    File.AppendAllText(Path.ChangeExtension(outputFile.FullName, ".warning"),
                        eventArgs.Message + Environment.NewLine);
                };

                string? parsed = JavaToCSharpConverter.ConvertText(javaText, options);
                File.WriteAllText(outputFile.FullName, parsed);
                _logger.LogInformation("{filePath} converted!", inputFile.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError("{filePath} failed! {type}: {message}", inputFile.Name, ex.GetType().Name, ex);
                File.WriteAllText(Path.ChangeExtension(outputFile.FullName, ".error"), ex.ToString());
            }
        }
        else
            _logger.LogError("Java input file {filePath} doesn't exist!", inputFile.FullName);
    }
}