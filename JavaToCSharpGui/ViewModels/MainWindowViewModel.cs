using System.Diagnostics;
using Avalonia;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Collections;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using JavaToCSharp;
using JavaToCSharpGui.Infrastructure;

namespace JavaToCSharpGui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private const string CopyToClipboardDefaultText = "Copy to Clipboard";
    
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
        _dispatcher = new UIDispatcher(Dispatcher.UIThread);
        
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: not null } desktop)
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

    [ObservableProperty] private string _addUsingInput = "";

    private IList<FileInfo> _javaFiles = new List<FileInfo>();
    private string _currentJavaFile = "";

    [ObservableProperty] private AvaloniaList<string> _usings = new();

    [ObservableProperty] private string _javaText = "";

    [ObservableProperty] private string _cSharpText = "";

    [ObservableProperty] private string _openPath = "";

    [ObservableProperty] private string _copyToClipboardText = CopyToClipboardDefaultText;

    [ObservableProperty] private string _conversionStateLabel = "";

    [ObservableProperty] private string? _selectedUsing;

    [RelayCommand]
    private void RemoveSelectedUsing()
    {
        if (SelectedUsing is not null && Usings.Contains(SelectedUsing))
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

    public FontFamily MonospaceFontFamily { get; } = FontFamily.Parse("Cascadia Code,SF Mono,DejaVu Sans Mono,Menlo,Consolas");

    [ObservableProperty] private bool _isConvertEnabled = true;

    [ObservableProperty] private bool _useFolderConvert;

    [RelayCommand]
    private void AddUsing()
    {
        Usings.Add(AddUsingInput);
        AddUsingInput = string.Empty;
    }

    public void RemoveUsing(string value)
    {
        Usings.Remove(value);
    }

    [ObservableProperty] private string _message = "";

    [ObservableProperty] private string _messageTitle = "";

    [RelayCommand]
    private async Task Convert()
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
                    string? csharp = JavaToCSharpConverter.ConvertText(JavaText, options);
                    await DispatcherInvoke(() => CSharpText = csharp ?? "");
                }
            }
            catch (Exception ex)
            {
                await DispatcherInvoke(() =>
                    ShowMessage($"There was an error converting the text to C#: {ex.GetBaseException().Message}", "Conversion Error"));

                ConversionStateLabel = "";
            }
            finally
            {
                await DispatcherInvoke(() => IsConvertEnabled = true);
            }
        });
    }

    /// <summary>
    /// Folder Browser OpenFolderDialog
    /// </summary>
    private async Task OpenFolderDialog()
    {
        if (_storageProvider?.CanPickFolder is true)
        {
            FolderPickerOpenOptions options = new()
            {
                Title = "Folder Browser",
                AllowMultiple = false,
                SuggestedStartLocation = await _storageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents)
            };
            
            var result = await _storageProvider.OpenFolderPickerAsync(options);
            
            if (!result.Any())
            {
                return;
            }

            string path = result[0].Path.AbsolutePath;
            var dir = new DirectoryInfo(path);
            
            if (!dir.Exists)
            {
                OpenPath = string.Empty;
                JavaText = string.Empty;
                _javaFiles = Array.Empty<FileInfo>();

                return;
            }

            OpenPath = path;

            await Task.Run(async () =>
            {
                var files = dir.GetFiles("*.java", SearchOption.AllDirectories);
                _javaFiles = files;

                int subStartIndex = path.Length;
                string javaText = string.Join(Environment.NewLine, files.Select(x => x.FullName[subStartIndex..]));

                await DispatcherInvoke(() => JavaText = javaText);
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
        {
            ShowMessage("No Java files found in the specified folder!");
            return;
        }

        var dir = new DirectoryInfo(OpenPath);
        var pDir = dir.Parent ?? throw new FileNotFoundException($"dir {OpenPath} parent");
        string dirName = dir.Name;
        string outDirName = $"{dirName}_net_{DateTime.Now.Millisecond}";
        var outDir = pDir.CreateSubdirectory(outDirName);
        
        if (outDir is not { Exists: true })
            throw new FileNotFoundException($"outDir {outDirName}");

        string outDirFullName = outDir.FullName;
        int subStartIndex = dir.FullName.Length;
        
        foreach (var jFile in _javaFiles.Where(static x => x.Directory is not null))
        {
            string jPath = jFile.Directory!.FullName;
            string jOutPath = $"{outDirFullName}{jPath[subStartIndex..]}";
            string jOutFileName = Path.GetFileNameWithoutExtension(jFile.Name) + ".cs";
            string jOutFileFullName = Path.Combine(jOutPath, jOutFileName);

            _currentJavaFile = jFile.FullName;
            
            if (!Directory.Exists(jOutPath))
                Directory.CreateDirectory(jOutPath);

            string jText = await File.ReadAllTextAsync(_currentJavaFile);
            
            if (string.IsNullOrEmpty(jText))
                continue;

            try
            {
                string? csText = JavaToCSharpConverter.ConvertText(jText, options);
                await File.WriteAllTextAsync(jOutFileFullName, csText);

                await DispatcherInvoke(() =>
                {
                    CSharpText =
                        $"{CSharpText} {Environment.NewLine}=================={Environment.NewLine}out.path: {jOutPath},{Environment.NewLine}\t\tfile: {jOutFileName}";
                });
            }
            catch (Exception ex)
            {
                await DispatcherInvoke(() =>
                {
                    CSharpText =
                        $"{CSharpText} {Environment.NewLine}=================={Environment.NewLine}[ERROR]out.path: {jOutPath},{Environment.NewLine}ex: {ex} {Environment.NewLine}";
                });
            }
        }
    }

    [RelayCommand]
    private void ClearMessage()
    {
        MessageTitle = "";
        Message = "";
        IsMessageShown = false;
    }

    private void ShowMessage(string message, string title = "")
    {
        MessageTitle = title;
        Message = message;
        IsMessageShown = true;
    }

    [ObservableProperty] private bool _isMessageShown;

    private void Options_StateChanged(object? sender, ConversionStateChangedEventArgs e)
    {
        ConversionStateLabel = e.NewState switch
        {
            ConversionState.Starting => "Starting...",
            ConversionState.ParsingJavaAst => "Parsing Java code...",
            ConversionState.BuildingCSharpAst => "Building C# AST...",
            ConversionState.Done => "Done!",
            _ => ConversionStateLabel
        };
    }

    private async void Options_WarningEncountered(object? sender, ConversionWarningEventArgs e)
    {
        if (UseFolderConvert)
        {
            await DispatcherInvoke(() =>
            {
                CSharpText =
                    $"{CSharpText} {Environment.NewLine}=================={Environment.NewLine}[WARN]out.path: {_currentJavaFile},{Environment.NewLine}\t\tConversionWarning-JavaLine:[{e.JavaLineNumber}]-Message:[{e.Message}]{Environment.NewLine}";
            });
        }
        else
        {
            ShowMessage($"Java Line {e.JavaLineNumber}: {e.Message}", "Warning Encountered");
        }
    }

    [RelayCommand]
    private async Task OpenFileDialog()
    {
        if (UseFolderConvert)
        {
            await OpenFolderDialog();
        }
        else if (_storageProvider?.CanOpen is true)
        {
            var filePickerOpenOptions = new FilePickerOpenOptions
            {
                FileTypeFilter = new FilePickerFileType[]
                {
                    new("Java files")
                    {
                        Patterns = new[] { "*.java" },
                    }
                },
            };

            var result = await _storageProvider.OpenFilePickerAsync(filePickerOpenOptions);
            
            if (result.Any())
            {
                OpenPath = result[0].Path.AbsolutePath;
                JavaText = await File.ReadAllTextAsync(result[0].Path.AbsolutePath);
            }
        }
    }

    [RelayCommand]
    private async Task CopyOutput()
    {
        if (_clipboard is null)
        {
            return;
        }

        await _clipboard.SetTextAsync(CSharpText);
        CopyToClipboardText = "Copied!";
        
        await Task.Delay(1000);
        
        await _dispatcher.InvokeAsync(() =>
        {
            CopyToClipboardText = CopyToClipboardDefaultText;
        }, DispatcherPriority.Background);
    }

    [RelayCommand]
    private static void ForkMeOnGitHub() => Process.Start(new ProcessStartInfo
    {
        FileName = "https://www.github.com/paulirwin/javatocsharp",
        UseShellExecute = true
    });

    private async Task DispatcherInvoke(Action callback) =>
        await _dispatcher.InvokeAsync(callback, DispatcherPriority.Normal);
}