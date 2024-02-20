using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using JavaToCSharpGui.Infrastructure;
using JavaToCSharpGui.ViewModels;
using TextMateSharp.Grammars;

namespace JavaToCSharpGui.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        IHostStorageProvider storageProvider = new HostStorageProvider(StorageProvider);
        IUIDispatcher dispatcher = new UIDispatcher(Avalonia.Threading.Dispatcher.UIThread);
        ITextClipboard clipboard = new TextClipboard(Clipboard);

        var vm = new MainWindowViewModel(storageProvider, dispatcher, clipboard);
        DataContext = vm;

        ConfigureEditors();
        InstallTextMate();
    }

    private void ConfigureEditors()
    {
        ConfigureEditor(JavaTextEditor);
        ConfigureEditor(CSharpTextEditor);
    }

    private static void ConfigureEditor(ITextEditorComponent editor)
    {
        // TODO.PI: make these options in settings for people with odd preferences
        editor.Options.ConvertTabsToSpaces = true;
        editor.Options.IndentationSize = 4;
    }

    private void InstallTextMate()
    {
        var appTheme = Application.Current?.ActualThemeVariant ?? ThemeVariant.Dark;
        var registryOptions = new RegistryOptions(appTheme == ThemeVariant.Dark ? ThemeName.DarkPlus : ThemeName.LightPlus);

        var javaTextMate = JavaTextEditor.InstallTextMate(registryOptions);
        javaTextMate.SetGrammar(registryOptions.GetScopeByLanguageId(registryOptions.GetLanguageByExtension(".java").Id));

        var csharpTextMate = CSharpTextEditor.InstallTextMate(registryOptions);
        csharpTextMate.SetGrammar(registryOptions.GetScopeByLanguageId(registryOptions.GetLanguageByExtension(".cs").Id));
    }

    private void ToggleButton_OnIsCheckedChanged(object sender, RoutedEventArgs e)
    {
        var app = Application.Current;
        if (app is not null)
        {
            var theme = app.ActualThemeVariant;
            app.RequestedThemeVariant = theme == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
            InstallTextMate();
        }
    }
}
