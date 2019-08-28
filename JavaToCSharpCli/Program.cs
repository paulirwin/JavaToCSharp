using System;
using System.IO;
using JavaToCSharp;
namespace JavaToCSharpCli
{
    internal class Program
    {
        private static void Main(string [] args)
        {
            if ( args == null || args.Length < 2 )
            {
                Console.WriteLine ("Usage: JavaToCSharpCli.exe [pathToJavaFile] [pathToCsOutputFile]");
                return;
            }
            if ( args [0] == "--all" )
            {
                string parsed;
                JavaConversionOptions opts;
                foreach ( string f in Directory.GetFiles (args [1], "*.*", SearchOption.AllDirectories) )
                {
                    if ( File.Exists (f) )
                    {
                        string jT = File.ReadAllText (f);
                        opts = new JavaConversionOptions ( );
                        parsed = JavaToCSharpConverter.ConvertText (jT, opts);
                        File.WriteAllText (f.Substring (0, f.LastIndexOf (".")) + ".cs", parsed);
                        Console.WriteLine (f.Substring (0, f.LastIndexOf (".")) + ".cs Done!");
                    }
                }
            }
            else
            {
                if ( !File.Exists (args [0]) )
                {
                    Console.WriteLine ("Java input file doesn't exist!");
                    return;
                }
                string javaText = File.ReadAllText (args [0]);
                // HACK for testing
                JavaConversionOptions options = new JavaConversionOptions ( )
                .AddPackageReplacement ("org\\.apache\\.lucene", "Lucene.Net")
                .AddUsing ("Lucene.Net")
                .AddUsing ("Lucene.Net.Support")
                .AddUsing ("Lucene.Net.Util");
                string parsed = JavaToCSharpConverter.ConvertText (javaText, options);
                File.WriteAllText (args [1], parsed);
                Console.WriteLine ("Done!");
            }
        }
    }
}
