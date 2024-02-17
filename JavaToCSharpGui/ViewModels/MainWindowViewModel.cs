using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JavaToCSharp;
using JavaToCSharpGui.Infrastructure;
using JavaToCSharpGui.Views;

namespace JavaToCSharpGui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
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
    }

    private IList<FileInfo> _javaFiles = new List<FileInfo>();
    private string _currentJavaFile = "";

    [ObservableProperty] private string _javaText = "";

    [ObservableProperty] private string _cSharpText = "";

    [ObservableProperty] private string _openPath = "";

    [ObservableProperty] private string _conversionStateLabel = "";

    public FontFamily MonospaceFontFamily { get; } = FontFamily.Parse("Cascadia Code,SF Mono,DejaVu Sans Mono,Menlo,Consolas");

    [ObservableProperty] private bool _isConvertEnabled = true;

    [ObservableProperty] private bool _useFolderConvert;

    [ObservableProperty] private string _message = "";

    [ObservableProperty] private string _messageTitle = "";

    [RelayCommand]
    private async Task Convert()
    {
        CurrentOptions.Options.WarningEncountered += Options_WarningEncountered;
        CurrentOptions.Options.StateChanged += Options_StateChanged;

        IsConvertEnabled = false;
        await Task.Run(async () =>
        {
            try
            {
                if (UseFolderConvert)
                {
                    await FolderConvert(CurrentOptions.Options);
                }
                else
                {
                    string? csharp = JavaToCSharpConverter.ConvertText(JavaText, CurrentOptions.Options);
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
                CurrentOptions.Options.WarningEncountered -= Options_WarningEncountered;
                CurrentOptions.Options.StateChanged -= Options_StateChanged;
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

            string path = result[0].Path.LocalPath;
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
                OpenPath = result[0].Path.LocalPath;
                JavaText = await File.ReadAllTextAsync(result[0].Path.LocalPath);
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
        ConversionStateLabel = "Copied C# code to clipboard!";

        await Task.Delay(2000);

        await _dispatcher.InvokeAsync(() =>
        {
            ConversionStateLabel = "";
        }, DispatcherPriority.Background);
    }

    [RelayCommand]
    private static void ForkMeOnGitHub() => Process.Start(new ProcessStartInfo
    {
        FileName = "https://github.com/paulirwin/javatocsharp",
        UseShellExecute = true
    });

    [RelayCommand]
    private static void OpenSettings()
    {
        var parent = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
        var settings = new SettingsWindow();

        if (parent is not null)
        {
            settings.ShowDialog(parent);
        }
        else
        {
            settings.Show();
        }
    }

    private async Task DispatcherInvoke(Action callback) =>
        await _dispatcher.InvokeAsync(callback, DispatcherPriority.Normal);
}
