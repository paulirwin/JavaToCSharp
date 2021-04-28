using System;

namespace WebPageLauncher
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                Console.WriteLine("Usage: WebPageLauncher.exe [url]");
                return;
            }

            var validuri = Uri.IsWellFormedUriString(args[0], UriKind.Absolute);

            if (!validuri)
            {
                Console.WriteLine("URL given is not a valid URL!");
                return;
            }

            Uri uri = new Uri(args[0], UriKind.Absolute);

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                Console.WriteLine("URL is not HTTP or HTTPS!");
                return;
            }

            try
            {
                string keyValue = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\Classes\http\shell\open\command", "", null) as string;
                if (string.IsNullOrEmpty(keyValue) == false)
                {
                    string browserPath = keyValue.Replace("%1", args[0]);
                    System.Diagnostics.Process.Start(browserPath);
                    return;
                }
            }
            catch { }

            try
            {
                System.Diagnostics.Process.Start(args[0]); //browserPath, argUrl);
                return;
            }
            catch { }

            try
            {
                string browserPath = GetWindowsPath("explorer.exe");
                string argUrl = "\"" + args[0] + "\"";

                System.Diagnostics.Process.Start(browserPath, argUrl);
                return;
            }
            catch { }
        }

        internal static string GetWindowsPath(string p_fileName)
        {
            string path = null;
            string sysdir;

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    if (i == 0)
                    {
                        path = Environment.GetEnvironmentVariable("SystemRoot");
                    }
                    else if (i == 1)
                    {
                        path = Environment.GetEnvironmentVariable("windir");
                    }
                    else if (i == 2)
                    {
                        sysdir = Environment.GetFolderPath(Environment.SpecialFolder.System);
                        path = System.IO.Directory.GetParent(sysdir).FullName;
                    }

                    if (path != null)
                    {
                        path = System.IO.Path.Combine(path, p_fileName);
                        if (System.IO.File.Exists(path) == true)
                        {
                            return path;
                        }
                    }
                }
                catch { }
            }

            // not found
            return null;
        }
    }
}
