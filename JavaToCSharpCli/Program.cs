using System;
using System.IO;
using JavaToCSharp;

namespace JavaToCSharpCli
{
    public class Program
    {
        public static void Main(string[] args)
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

        private static void ConvertToCSharpDir(string folderPath, string outputFolderPath)
        {
            if (Directory.Exists(folderPath))
            {
                var input = new DirectoryInfo(folderPath);
                var output = new DirectoryInfo(outputFolderPath);
                foreach (var f in input.GetFiles("*.java", SearchOption.AllDirectories))
                    ConvertToCSharpFile(
                        f.FullName,
                        Path.Combine(output.FullName, f.Name.Replace(f.Extension, ".cs")));
            }
            else
                Console.WriteLine("Java input folder doesn't exist!");
        }

        private static void ConvertToCSharpFile(string filePath, string outputFilePath)
        {
            if (File.Exists(filePath))
            {
                var javaText = File.ReadAllText(filePath);
                var options = new JavaConversionOptions();

                options.WarningEncountered += (sender, eventArgs)
                    => Console.WriteLine($"[WARNING] Line {eventArgs.JavaLineNumber}: {eventArgs.Message}");

                var parsed = JavaToCSharpConverter.ConvertText(javaText, options);

                File.WriteAllText(outputFilePath, parsed);
                Console.WriteLine("Done!");
            }
            else
                Console.WriteLine("Java input file doesn't exist!");
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Usage:\r\n\tJavaToCSharpCli.exe -f [pathToJavaFile] [pathToCsOutputFile]");
            Console.WriteLine("Usage:\tJavaToCSharpCli.exe -d [pathToJavaFolder] [pathToCsOutputFolder]");
        }
    }
}