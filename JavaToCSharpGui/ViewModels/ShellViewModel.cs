using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using Caliburn.Micro;

using JavaToCSharp;

using JavaToCSharpGui.Views;

using Microsoft.Win32;

namespace JavaToCSharpGui.ViewModels;

[View(typeof(ShellView))]
public class ShellViewModel : Screen, IShell
{
    private string _addUsingInput;
    private string _javaText;
    private string _csharpText;
    private string _openPath;
    private string _copiedText;
    private string _conversionState;
    private bool _includeUsings = true;
    private bool _includeNamespace = true;
    private bool _useDebugAssertForAsserts;
    private bool _useUnrecognizedCodeToComment;
    private bool _isConvertEnabled = true;
    private bool _convertSystemOutToConsole;

    #region UseFolder

    private bool _useFolderConvert;
    private IList<FileInfo> _javaFiles;
    private string _currentJavaFile;

    #endregion

    public ShellViewModel()
    {
        base.DisplayName = "Java to C# Converter";

        _isConvertEnabled = true;
        _useFolderConvert = false;

        _includeUsings = Properties.Settings.Default.UseUsingsPreference;
        _includeNamespace = Properties.Settings.Default.UseNamespacePreference;
        _useDebugAssertForAsserts = Properties.Settings.Default.UseDebugAssertPreference;
        _useUnrecognizedCodeToComment = Properties.Settings.Default.UseUnrecognizedCodeToComment;
        _convertSystemOutToConsole = Properties.Settings.Default.ConvertSystemOutToConsole;
    }

    public ObservableCollection<string> Usings { get; } = new(new JavaConversionOptions().Usings);

    public string AddUsingInput
    {
        get => _addUsingInput;
        set
        {
            _addUsingInput = value;
            NotifyOfPropertyChange(() => AddUsingInput);
        }
    }

    public string JavaText
    {
        get => _javaText;
        set
        {
            _javaText = value;
            NotifyOfPropertyChange(() => JavaText);
        }
    }

    public string CSharpText
    {
        get => _csharpText;
        set
        {
            _csharpText = value;
            NotifyOfPropertyChange(() => CSharpText);
        }
    }

    public string OpenPath
    {
        get => _openPath;
        set
        {
            _openPath = value;
            NotifyOfPropertyChange(() => OpenPath);
        }
    }

    public string CopiedText
    {
        get => _copiedText;
        set
        {
            _copiedText = value;
            NotifyOfPropertyChange(() => CopiedText);
        }
    }

    public string ConversionStateLabel
    {
        get => _conversionState;
        set
        {
            _conversionState = value;
            NotifyOfPropertyChange(() => ConversionStateLabel);
        }
    }

    public bool IncludeUsings
    {
        get => _includeUsings;
        set
        {
            _includeUsings = value;
            NotifyOfPropertyChange(() => IncludeUsings);
            Properties.Settings.Default.UseUsingsPreference = value;
            Properties.Settings.Default.Save();
        }
    }

    public bool IncludeNamespace
    {
        get => _includeNamespace;
        set
        {
            _includeNamespace = value;
            NotifyOfPropertyChange(() => IncludeNamespace);
            Properties.Settings.Default.UseNamespacePreference = value;
            Properties.Settings.Default.Save();
        }
    }

    public bool UseDebugAssertForAsserts
    {
        get => _useDebugAssertForAsserts;
        set
        {
            _useDebugAssertForAsserts = value;
            NotifyOfPropertyChange(() => UseDebugAssertForAsserts);
            Properties.Settings.Default.UseDebugAssertPreference = value;
            Properties.Settings.Default.Save();

            if (value && !Usings.Contains("System.Diagnostics"))
            {
                _addUsingInput = "System.Diagnostics";
                AddUsing();
            }
        }
    }

    public bool UseUnrecognizedCodeToComment
    {
        get => _useUnrecognizedCodeToComment;
        set
        {
            _useUnrecognizedCodeToComment = value;
            NotifyOfPropertyChange(() => UseUnrecognizedCodeToComment);
            Properties.Settings.Default.UseUnrecognizedCodeToComment = value;
            Properties.Settings.Default.Save();
        }
    }

    public bool IsConvertEnabled
    {
        get => _isConvertEnabled;
        set
        {
            _isConvertEnabled = value;
            NotifyOfPropertyChange(() => IsConvertEnabled);
        }
    }

    public bool UseFolderConvert
    {
        get => _useFolderConvert;
        set
        {
            _useFolderConvert = value;
            NotifyOfPropertyChange(() => UseFolderConvert);
        }
    }

    public bool ConvertSystemOutToConsole
    {
        get => _convertSystemOutToConsole;
        set
        {
            _convertSystemOutToConsole = value;
            NotifyOfPropertyChange(() => ConvertSystemOutToConsole);
        }
    }

    public void AddUsing()
    {
        Usings.Add(_addUsingInput);
        AddUsingInput = String.Empty;
    }

    public void RemoveUsing(string value)
    {
        Usings.Remove(value);
    }

    public void Convert()
    {
        var options = new JavaConversionOptions();
        options.ClearUsings();

        foreach (string ns in Usings)
        {
            options.AddUsing(ns);
        }

        options.IncludeUsings = _includeUsings;
        options.IncludeNamespace = _includeNamespace;
        options.UseDebugAssertForAsserts = _useDebugAssertForAsserts;
        options.UseUnrecognizedCodeToComment = _useUnrecognizedCodeToComment;
        options.ConvertSystemOutToConsole = _convertSystemOutToConsole;

        options.WarningEncountered += Options_WarningEncountered;
        options.StateChanged += Options_StateChanged;

        IsConvertEnabled = false;
        Task.Run(() =>
        {
            try
            {
                if (_useFolderConvert)
                {
                    FolderConvert(options);
                }
                else
                {
                    var csharp = JavaToCSharpConverter.ConvertText(JavaText, options);
                    DispatcherInvoke(() => CSharpText = csharp);
                }
            }
            catch (Exception ex)
            {
                DispatcherInvoke(() => MessageBox.Show("There was an error converting the text to C#: " + ex.Message));
            }
            finally
            {
                DispatcherInvoke(() => IsConvertEnabled = true);
            }
        });
    }

    private void Options_StateChanged(object sender, ConversionStateChangedEventArgs e)
    {
        switch (e.NewState)
        {
            case ConversionState.Starting:
                ConversionStateLabel = "Starting...";
                break;

            case ConversionState.ParsingJavaAst:
                ConversionStateLabel = "Parsing Java code...";
                break;

            case ConversionState.BuildingCSharpAst:
                ConversionStateLabel = "Building C# AST...";
                break;

            case ConversionState.Done:
                ConversionStateLabel = "Done!";
                break;

            default:
                break;
        }
    }

    private void Options_WarningEncountered(object sender, ConversionWarningEventArgs e)
    {
        if (_useFolderConvert)
        {
            DispatcherInvoke(() =>
            {
                CSharpText = $"{ CSharpText } \r\n==================\r\n[WARN]out.path: { _currentJavaFile },\r\n\t\tConversionWarning-JavaLine:[{ e.JavaLineNumber}]-Message:[{ e.Message}]\r\n";
            });
        }
        else
        {
            MessageBox.Show($"Java Line {e.JavaLineNumber}: {e.Message}", "Warning Encountered");
        }
    }

    public void OpenFileDialog()
    {
        if (_useFolderConvert)
        {
            OpenFolderDialog();
        }
        else
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Java Files (*.java)|*.java",
                Title = "Open Java File"
            };

            var result = ofd.ShowDialog();
            if (result.GetValueOrDefault())
            {
                OpenPath = ofd.FileName;
                JavaText = File.ReadAllText(ofd.FileName);
            }
        }
    }

    #region FolderCodeConvert

    /// <summary>
    /// Folder Browser OpenFolderDialog
    /// </summary>
    private void OpenFolderDialog()
    {
        var dlg = new FolderBrowserForWPF.Dialog()
        {
            Title = "Folder Browser",
        };

        if (!(dlg.ShowDialog() ?? false))
            return;

        var path = dlg.FileName;
        var dir = new DirectoryInfo(path);
        if (!dir.Exists)
        {
            OpenPath = String.Empty;
            JavaText = String.Empty;
            _javaFiles = null;

            return;
        }

        var pDir = dir.Parent;
        if (pDir == null)
        {
            MessageBox.Show("Fail: Root Directory !!!");
            return;
        }

        OpenPath = path;

        Task.Run(() =>
        {
            //list all subdir *.java
            var files = dir.GetFiles("*.java", SearchOption.AllDirectories);
            _javaFiles = files;

            //out java path
            var subStartIndex = path.Length;
            var javaTexts = String.Join("\r\n", files.Select(x => x.FullName[subStartIndex..]));

            DispatcherInvoke(() => JavaText = javaTexts);
        });
    }

    /// <summary>
    /// Folder Code Convert
    /// </summary>
    /// <param name="options"></param>
    private void FolderConvert(JavaConversionOptions options)
    {
        if (_javaFiles == null || options == null)
            return;

        var dir = new DirectoryInfo(_openPath);
        var pDir = dir.Parent ?? throw new FileNotFoundException($"dir {_openPath} parent");
        var dirName = dir.Name;
        var outDirName = $"{dirName}_net_{DateTime.Now.Millisecond}";
        var outDir = pDir.CreateSubdirectory(outDirName);
        if (outDir == null || !outDir.Exists)
            throw new FileNotFoundException($"outDir {outDirName}");

        var outDirFullName = outDir.FullName;
        var subStartIndex = dir.FullName.Length;
        foreach (var jFile in _javaFiles)
        {
            var jPath = jFile.Directory.FullName;
            var jOutPath = outDirFullName + jPath[subStartIndex..];
            var jOutFileName = Path.GetFileNameWithoutExtension(jFile.Name) + ".cs";
            var jOutFileFullName = Path.Combine(jOutPath, jOutFileName);

            _currentJavaFile = jFile.FullName;
            if (!Directory.Exists(jOutPath))
                Directory.CreateDirectory(jOutPath);

            var jText = File.ReadAllText(_currentJavaFile);
            if (String.IsNullOrEmpty(jText))
                continue;

            try
            {
                var csText = JavaToCSharpConverter.ConvertText(jText, options);
                File.WriteAllText(jOutFileFullName, csText);

                DispatcherInvoke(() =>
                {
                    CSharpText = $"{ CSharpText } \r\n==================\r\nout.path: { jOutPath },\r\n\t\tfile: {jOutFileName}";
                });
            }
            catch (Exception ex)
            {
                DispatcherInvoke(() =>
                {
                    CSharpText = $"{ CSharpText } \r\n==================\r\n[ERROR]out.path: { jOutPath },\r\nex: { ex } \r\n";
                });
            }
        }
    }

    #endregion

    public void CopyOutput()
    {
        Clipboard.SetText(CSharpText);

        CopiedText = "Copied!";

        Task.Run(async () =>
        {
            await Task.Delay(5000);
            CopiedText = null;
        });
    }

    public static void ForkMeOnGitHub()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "http://www.github.com/paulirwin/javatocsharp",
            UseShellExecute = true
        });
    }

    /// <summary>
    /// Dispatcher.CurrentDispatcher.Invoke
    /// </summary>
    /// <param name="callback"></param>
    private static void DispatcherInvoke(System.Action callback)
    {
        Dispatcher.CurrentDispatcher.Invoke(callback);
    }
}