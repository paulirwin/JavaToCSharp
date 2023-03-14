using System;
using System.IO;
using System.Threading;
using JavaToCSharp;
using Microsoft.Extensions.Logging;

namespace JavaToCSharpCli
{
    public class Program
    {
        private static readonly ILogger _logger;

        static Program()
        {
            var loggerFactory =
                LoggerFactory.Create(builder => builder.AddSimpleConsole().SetMinimumLevel(LogLevel.Information));
            _logger = loggerFactory.CreateLogger<Program>();
        }

        public static void Main(string[] args)
        {
            try
            {
                if (args == null || args.Length < 3)
                    ShowHelp();
                else
                    switch (args[0])
                    {
                        case "-f":
                            ConvertToCSharpFile(args[1], args[2]);
                            break;
                        case "-d":
                            ConvertToCSharpDir(args[1], args[2]);
                            break;
                        default:
                            ShowHelp();
                            break;
                    }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
            }

            // allow logger background thread to flush
            Thread.Sleep(TimeSpan.FromSeconds(1));
        }

        private static void ConvertToCSharpDir(string folderPath, string outputFolderPath)
        {
            if (Directory.Exists(folderPath))
            {
                var input = new DirectoryInfo(folderPath);
                foreach (var f in input.GetFiles("*.java", SearchOption.AllDirectories))
                {
                    var newFolderPath =
                        f.DirectoryName.Replace(folderPath, outputFolderPath, StringComparison.OrdinalIgnoreCase);
                    if (!Directory.Exists(newFolderPath))
                    {
                        Directory.CreateDirectory(newFolderPath);
                    }

                    ConvertToCSharpFile(f.FullName,
                                        Path.Combine(newFolderPath, Path.ChangeExtension(f.Name, ".cs")),
                                        false);
                }
            }
            else
                _logger.LogError("Java input folder {path} doesn't exist!", folderPath);
        }

        private static void ConvertToCSharpFile(string filePath, string outputFilePath, bool overwrite = true)
        {
            if (!overwrite && File.Exists(outputFilePath))
                _logger.LogInformation("{outputFilePath} exists, skip to next.", outputFilePath);
            else if (File.Exists(filePath))
            {
                try
                {
                    var javaText = File.ReadAllText(filePath);
                    var options = new JavaConversionOptions();

                    options.WarningEncountered += (sender, eventArgs) =>
                                                  {
                                                      var message = $"Line {eventArgs.JavaLineNumber}: {eventArgs.Message}";
                                                      _logger.LogWarning(message);
                                                      File.AppendAllText(Path.ChangeExtension(outputFilePath, ".warning"), message + Environment.NewLine);
                                                  };

                    var parsed = JavaToCSharpConverter.ConvertText(javaText, options);
                    File.WriteAllText(outputFilePath, parsed);
                    _logger.LogInformation("{filePath} converted!", filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError("{filePath} failed! {type}: {message}", filePath, ex.GetType().Name, ex);
                    File.WriteAllText(Path.ChangeExtension(outputFilePath, ".error"), ex.ToString());
                }
            }
            else
                _logger.LogError("Java input file {filePath} doesn't exist!", filePath);
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Usage:\r\n\tJavaToCSharpCli.exe -f [pathToJavaFile] [pathToCsOutputFile]");
            Console.WriteLine("\tJavaToCSharpCli.exe -d [pathToJavaFolder] [pathToCsOutputFolder]");
        }
    }
}