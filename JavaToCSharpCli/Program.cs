using System;
using System.IO;
using JavaToCSharp;

namespace JavaToCSharpCli
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args == null || args.Length < 2)
            {
                Console.WriteLine("Usage: JavaToCSharpCli.exe [pathToJavaFile] [pathToCsOutputFile]");
                return;
            }

            if (!File.Exists(args[0]))
            {
                Console.WriteLine("Java input file doesn't exist!");
                return;
            }

            var javaText = File.ReadAllText(args[0]);

            // HACK for testing
            var options = new JavaConversionOptions();

            var parsed = JavaToCSharpConverter.ConvertText(javaText, options);

            File.WriteAllText(args[1], parsed);
            Console.WriteLine("Done!");
        }
    }
}
