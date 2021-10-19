using System;
using System.IO;
using JavaToCSharp;
using log4net;
using log4net.Config;

namespace JavaToCSharpCli
{
    public class Program
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(Program));
        public static void Main(string[] args)
        {
            GlobalContext.Properties["appname"] = "JavaToCSharpCli";
            XmlConfigurator.ConfigureAndWatch(new FileInfo("log4net.config"));
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
            catch(Exception ex)
            {
                _logger.Error(ex.Message, ex);
            }
        }

        private static void ConvertToCSharpDir(string folderPath, string outputFolderPath)
        {
            if (Directory.Exists(folderPath))
            {
                var input = new DirectoryInfo(folderPath);
                var output = new DirectoryInfo(outputFolderPath);
                foreach (var f in input.GetFiles("*.java", SearchOption.AllDirectories))
                    ConvertToCSharpFile(
                        f.FullName,
                        Path.Combine(output.FullName, f.Name.Replace(f.Extension, ".cs")), 
                        false);
            }
            else
                _logger.Info("Java input folder doesn't exist!");
        }

        private static void ConvertToCSharpFile(string filePath, string outputFilePath, bool overwrite = true)
        {
            if (!overwrite && File.Exists(outputFilePath))
                _logger.Info($"{outputFilePath} exists, skip to next.");
            else if (File.Exists(filePath))
            {
                try
                {
                    var javaText = File.ReadAllText(filePath);
                    var options = new JavaConversionOptions();

                    options.WarningEncountered += (sender, eventArgs)
                        => _logger.Warn($"[WARNING] Line {eventArgs.JavaLineNumber}: {eventArgs.Message}");

                    var parsed = JavaToCSharpConverter.ConvertText(javaText, options);

                    File.WriteAllText(outputFilePath, parsed);
                    _logger.Info($"{filePath} was done!");
                }
                catch (Exception ex)
                {
                    _logger.Error($"{filePath} was failed! err = " + ex.Message, ex);
                }
            }
            else
                _logger.Info("Java input file doesn't exist!");
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Usage:\r\n\tJavaToCSharpCli.exe -f [pathToJavaFile] [pathToCsOutputFile]");
            Console.WriteLine("Usage:\tJavaToCSharpCli.exe -d [pathToJavaFolder] [pathToCsOutputFolder]");
        }
    }
}