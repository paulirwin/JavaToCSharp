using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Collections;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using JavaToCSharp;
using JavaToCSharpGui.Infrastructure;

namespace JavaToCSharpGui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private bool _includeUsings = true;
    private bool _includeNamespace = true;
    private bool _includeComments = true;
    private bool _useDebugAssertForAsserts;
    private bool _useUnrecognizedCodeToComment;
    private bool _convertSystemOutToConsole;

    private readonly IHostStorageProvider? _storageProvider;
    private readonly IUIDispatcher _dispatcher;
    private readonly ITextClipboard? _clipboard;

    /// <summary>
    /// Constructor for the Avalonia Designer view inside the IDE.
    /// </summary>
    public MainWindowViewModel()
    {
        _dispatcher = new UIDispatcher(Avalonia.Threading.Dispatcher.UIThread);
        if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow is not null)
        {
            _storageProvider = new HostStorageProvider(desktop.MainWindow.StorageProvider);
            _clipboard = new TextClipboard(desktop.MainWindow.Clipboard);
        }
    }

    /// <summary>
    /// Real constructor
    /// </summary>
    /// <param name="storageProvider">The storage provider.</param>
    /// <param name="dispatcher">The UI Thread Dispatcher.</param>
    /// <param name="clipboard">The clipboard.</param>
    public MainWindowViewModel(IHostStorageProvider storageProvider, IUIDispatcher dispatcher, ITextClipboard clipboard)
    {
        _storageProvider = storageProvider;
        _dispatcher = dispatcher;
        _clipboard = clipboard;
        DisplayName = "Java to C# Converter";

        _isConvertEnabled = true;
        _useFolderConvert = false;

        _includeUsings = Properties.Settings.Default.UseUsingsPreference;
        _includeNamespace = Properties.Settings.Default.UseNamespacePreference;
        _includeComments = Properties.Settings.Default.IncludeComments;
        _useDebugAssertForAsserts = Properties.Settings.Default.UseDebugAssertPreference;
        _useUnrecognizedCodeToComment = Properties.Settings.Default.UseUnrecognizedCodeToComment;
        _convertSystemOutToConsole = Properties.Settings.Default.ConvertSystemOutToConsole;
    }

    [ObservableProperty]
    private string _addUsingInput = "";

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

    [ObservableProperty]
    private string? _selectedUsing;

    [RelayCommand]
    public void RemoveSelectedUsing()
    {
        if(SelectedUsing is not null && Usings.Contains(SelectedUsing))
        {
            Usings.Remove(SelectedUsing);
        }
    }

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
    
    public bool IncludeComments
    {
        get => _includeComments;
        set
        {
            SetProperty(ref _includeComments, value);
            Properties.Settings.Default.IncludeComments = value;
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
    
    public bool ConvertSystemOutToConsole
    {
        get => _convertSystemOutToConsole;
        set
        {
            _convertSystemOutToConsole = value;
            SetProperty(ref _convertSystemOutToConsole, value);
            Properties.Settings.Default.ConvertSystemOutToConsole = value;
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
        options.IncludeComments = IncludeComments;
        options.UseDebugAssertForAsserts = UseDebugAssertForAsserts;
        options.UseUnrecognizedCodeToComment = UseUnrecognizedCodeToComment;
        options.ConvertSystemOutToConsole = ConvertSystemOutToConsole;

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
        if(_storageProvider?.CanPickFolder is true) {
            FolderPickerOpenOptions options = new() {
                Title = "Folder Browser",
                AllowMultiple = false,
                SuggestedStartLocation = await _storageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents)
            };
            var result = await _storageProvider.OpenFolderPickerAsync(options);
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
                var javaTexts = string.Join(Environment.NewLine, files.Select(x => x.FullName[subStartIndex..]));

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
                    CSharpText = $"{CSharpText} {Environment.NewLine}=================={Environment.NewLine}out.path: { jOutPath },{Environment.NewLine}\t\tfile: {jOutFileName}";
                });
            }
            catch (Exception ex)
            {
                await DispatcherInvoke(() =>
                {
                    CSharpText = $"{CSharpText} {Environment.NewLine}=================={Environment.NewLine}[ERROR]out.path: { jOutPath },{Environment.NewLine}ex: { ex } {Environment.NewLine}";
                });
            }
        }
    }

    #endregion

    [RelayCommand]
    public void ClearMessage()
    {
        MessageTitle = "";
        Message = "";
        IsMessageShown = false;
    }

    private void ShowMessage(string message, string title = "") {
        MessageTitle = title;
        Message = message;
        IsMessageShown = true;
    }

    [ObservableProperty]
    private bool _isMessageShown;

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
                CSharpText = $"{CSharpText} {Environment.NewLine}=================={Environment.NewLine}[WARN]out.path: { _currentJavaFile },{Environment.NewLine}\t\tConversionWarning-JavaLine:[{ e.JavaLineNumber}]-Message:[{ e.Message}]{Environment.NewLine}";
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
        else if(_storageProvider?.CanOpen is true)
        {
            var filePickerOpenOptions = new FilePickerOpenOptions
            {
                FileTypeFilter = new FilePickerFileType[]
                {
                    new("Java files")
                    {
                        Patterns = new[]{ "*.java" },
                    }
                },
            };

            var result = await _storageProvider.OpenFilePickerAsync(filePickerOpenOptions);
            if (result.Any())
            {
                OpenPath = result[0].Path.AbsolutePath;
                JavaText = File.ReadAllText(result[0].Path.AbsolutePath);
            }
        }
    }

    [RelayCommand]
    public async Task CopyOutput()
    {
        if(_clipboard is null)
        {
            return;
        }
        await _clipboard.SetTextAsync(CSharpText);
        CopiedText = "Copied!";
        await _dispatcher.InvokeAsync(async () => {
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
    private async Task DispatcherInvoke(Action callback) => await _dispatcher.InvokeAsync(callback, DispatcherPriority.Normal);
}
