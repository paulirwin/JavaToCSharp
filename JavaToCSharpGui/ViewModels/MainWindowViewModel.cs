using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using JavaToCSharp;

namespace JavaToCSharpGui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _addUsingInput = "";
    private bool _includeUsings = true;
    private bool _includeNamespace = true;
    private bool _useDebugAssertForAsserts;
    private bool _useUnrecognizedCodeToComment;

    private readonly IClassicDesktopStyleApplicationLifetime? _desktop;

    #region UseFolder

    private IList<FileInfo> _javaFiles = new List<FileInfo>();
    private string _currentJavaFile = "";

    #endregion

    [ObservableProperty]
    private AvaloniaList<string> _usings = new();

    [ObservableProperty]
    private string _javaText = "";

    [ObservableProperty]
    private string _cSharpText = "";

    [ObservableProperty]
    private string _openPath = "";

    [ObservableProperty]
    private string _copiedText = "";

    [ObservableProperty]
    private string _conversionStateLabel = "";

    public bool IncludeUsings
    {
        get => _includeUsings;
        set
        {
            SetProperty(ref _includeUsings, value);
            Properties.Settings.Default.UseUsingsPreference = value;
            Properties.Settings.Default.Save();
        }
    }

    public bool IncludeNamespace
    {
        get => _includeNamespace;
        set
        {
            SetProperty(ref _includeNamespace, value);
            Properties.Settings.Default.UseNamespacePreference = value;
            Properties.Settings.Default.Save();
        }
    }

    public bool UseDebugAssertForAsserts
    {
        get => _useDebugAssertForAsserts;
        set
        {
            SetProperty(ref _useDebugAssertForAsserts, value);
            Properties.Settings.Default.UseDebugAssertPreference = value;
            Properties.Settings.Default.Save();

            if (value && !Usings.Contains("System.Diagnostics"))
            {
                AddUsingInput = "System.Diagnostics";
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
            SetProperty(ref _useUnrecognizedCodeToComment, value);
            Properties.Settings.Default.UseUnrecognizedCodeToComment = value;
            Properties.Settings.Default.Save();
        }
    }

    [ObservableProperty]
    private bool _isConvertEnabled = true;

    [ObservableProperty]
    private bool _useFolderConvert;

    [RelayCommand]
    public void AddUsing()
    {
        Usings.Add(AddUsingInput);
        AddUsingInput = string.Empty;
    }

    public void RemoveUsing(string value)
    {
        Usings.Remove(value);
    }

    [ObservableProperty]
    private string _message = "";

    [ObservableProperty]
    private string _messageTitle = "";
    
    [RelayCommand]
    public async Task Convert()
    {
        var options = new JavaConversionOptions();
        options.ClearUsings();

        foreach (string ns in Usings)
        {
            options.AddUsing(ns);
        }

        options.IncludeUsings = IncludeUsings;
        options.IncludeNamespace = IncludeNamespace;
        options.UseDebugAssertForAsserts = UseDebugAssertForAsserts;
        options.UseUnrecognizedCodeToComment = UseUnrecognizedCodeToComment;

        options.WarningEncountered += Options_WarningEncountered;
        options.StateChanged += Options_StateChanged;

        IsConvertEnabled = false;
        await Task.Run(async () =>
        {
            try
            {
                if (UseFolderConvert)
                {
                    await FolderConvert(options);
                }
                else
                {
                    var csharp = JavaToCSharpConverter.ConvertText(JavaText, options);
                    await DispatcherInvoke(() => CSharpText = csharp ?? "");
                }
            }
            catch (Exception ex)
            {
                await DispatcherInvoke(() => Message = $"There was an error converting the text to C#: {ex.GetBaseException().Message}");
            }
            finally
            {
                await DispatcherInvoke(() => IsConvertEnabled = true);
            }
        });
    }

    #region FolderConvert

    /// <summary>
    /// Folder Browser OpenFolderDialog
    /// </summary>
    private async Task OpenFolderDialog()
    {
        if(_desktop?.MainWindow?.StorageProvider is { CanPickFolder: true }) {
            IStorageProvider storageProvider = _desktop.MainWindow.StorageProvider;
            FolderPickerOpenOptions options = new() {
                Title = "Folder Browser",
                AllowMultiple = false,
                SuggestedStartLocation = await storageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents)
            };
            var result = await storageProvider.OpenFolderPickerAsync(options);
            if(!result.Any()) {
                return;
            }
            var path = result[0].Path.AbsolutePath;
            var dir = new DirectoryInfo(path);
            if (!dir.Exists)
            {
                OpenPath = string.Empty;
                JavaText = string.Empty;
                _javaFiles = Array.Empty<FileInfo>();

                return;
            }

            var pDir = dir.Parent;
            if (pDir == null)
            {
                ShowMessage("Fail: Root Directory !!!");
                return;
            }

            OpenPath = path;

            await Task.Run(async () =>
            {
                //list all subdir *.java
                var files = dir.GetFiles("*.java", SearchOption.AllDirectories);
                _javaFiles = files;

                //out java path
                var subStartIndex = path.Length;
                var javaTexts = string.Join("\r\n", files.Select(x => x.FullName[subStartIndex..]));

                await DispatcherInvoke(() => JavaText = javaTexts);
            });
        }
    }

    /// <summary>
    /// Folder Code Convert
    /// </summary>
    /// <param name="options">The user options used for the conversion.</param>
    private async Task FolderConvert(JavaConversionOptions? options)
    {
        if (_javaFiles.Count == 0 || options == null)
            return;

        var dir = new DirectoryInfo(OpenPath);
        var pDir = dir.Parent ?? throw new FileNotFoundException($"dir {OpenPath} parent");
        var dirName = dir.Name;
        var outDirName = $"{dirName}_net_{DateTime.Now.Millisecond}";
        var outDir = pDir.CreateSubdirectory(outDirName);
        if (outDir == null || !outDir.Exists)
            throw new FileNotFoundException($"outDir {outDirName}");

        var outDirFullName = outDir.FullName;
        var subStartIndex = dir.FullName.Length;
        foreach (var jFile in _javaFiles.Where(static x => x.Directory is not null))
        {
            var jPath = jFile.Directory!.FullName;
            var jOutPath = $"{outDirFullName}{jPath[subStartIndex..]}";
            var jOutFileName = Path.GetFileNameWithoutExtension(jFile.Name) + ".cs";
            var jOutFileFullName = Path.Combine(jOutPath, jOutFileName);

            _currentJavaFile = jFile.FullName;
            if (!Directory.Exists(jOutPath))
                Directory.CreateDirectory(jOutPath);

            var jText = File.ReadAllText(_currentJavaFile);
            if (string.IsNullOrEmpty(jText))
                continue;

            try
            {
                var csText = JavaToCSharpConverter.ConvertText(jText, options);
                File.WriteAllText(jOutFileFullName, csText);

                await DispatcherInvoke(() =>
                {
                    CSharpText = $"{ CSharpText } \r\n==================\r\nout.path: { jOutPath },\r\n\t\tfile: {jOutFileName}";
                });
            }
            catch (Exception ex)
            {
                await DispatcherInvoke(() =>
                {
                    CSharpText = $"{ CSharpText } \r\n==================\r\n[ERROR]out.path: { jOutPath },\r\nex: { ex } \r\n";
                });
            }
        }
    }

    #endregion

    private void ShowMessage(string message, string title = "") {
        MessageTitle = title;
        Message = message;
        IsMessageShown = true;
    }

    [ObservableProperty]
    private bool _isMessageShown = true;

    private void Options_StateChanged(object? sender, ConversionStateChangedEventArgs e)
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

    private async void Options_WarningEncountered(object? sender, ConversionWarningEventArgs e)
    {
        if (UseFolderConvert)
        {
            await DispatcherInvoke(() =>
            {
                CSharpText = $"{ CSharpText } \r\n==================\r\n[WARN]out.path: { _currentJavaFile },\r\n\t\tConversionWarning-JavaLine:[{ e.JavaLineNumber}]-Message:[{ e.Message}]\r\n";
            });
        }
        else
        {
            ShowMessage($"Java Line {e.JavaLineNumber}: {e.Message}", "Warning Encountered");
        }
    }

    [RelayCommand]
    public async Task OpenFileDialog()
    {
        if (UseFolderConvert)
        {
            await OpenFolderDialog();
        }
        else if(_desktop?.MainWindow?.StorageProvider is {  CanOpen: true })
        {
            var storageProvider = _desktop.MainWindow.StorageProvider;
            var filePickerOpenOptions = new  FilePickerOpenOptions()
            {
                FileTypeFilter = new FilePickerFileType[]
                {
                    new("Java files")
                    {
                        Patterns = new string[]{ "*.java" },
                    }
                },
            };

            var result = await storageProvider.OpenFilePickerAsync(filePickerOpenOptions);
            if (result.Any())
            {
                OpenPath = result[0].Path.AbsolutePath;
                JavaText = File.ReadAllText(result[0].Path.AbsolutePath);
            }
        }
    }

    public MainWindowViewModel()
    {
        if(App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            _desktop = desktop;
        }
    }

    public MainWindowViewModel(IClassicDesktopStyleApplicationLifetime? desktop)
    {
        _desktop = desktop;
        base.DisplayName = "Java to C# Converter";

        _isConvertEnabled = true;
        _useFolderConvert = false;

        _includeUsings = Properties.Settings.Default.UseUsingsPreference;
        _includeNamespace = Properties.Settings.Default.UseNamespacePreference;
        _useDebugAssertForAsserts = Properties.Settings.Default.UseDebugAssertPreference;
        _useUnrecognizedCodeToComment = Properties.Settings.Default.UseUnrecognizedCodeToComment;
    }

    [RelayCommand]
    public async Task CopyOutput()
    {
        if(_desktop?.MainWindow?.Clipboard is null) {
            return;
        }
        await _desktop.MainWindow.Clipboard.SetTextAsync(CSharpText);
        CopiedText = "Copied!";
        await Dispatcher.UIThread.InvokeAsync(async () => {
            await Task.Delay(500);
            CopiedText = "";
        }, DispatcherPriority.Background);
    }

    [RelayCommand]
    public void ForkMeOnGitHub() => Process.Start(new ProcessStartInfo
    {
        FileName = "http://www.github.com/paulirwin/javatocsharp",
        UseShellExecute = true
    });

    /// <summary>
    /// Dispatcher.UIThread.InvokeAsync
    /// </summary>
    /// <param name="callback"></param>
    private async Task DispatcherInvoke(Action callback) => await Dispatcher.UIThread.InvokeAsync(callback);
}
