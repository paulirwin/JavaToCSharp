using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using JavaToCSharpGui.Infrastructure;
using JavaToCSharpGui.ViewModels;

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
    }

    private void ToggleButton_OnIsCheckedChanged(object sender, RoutedEventArgs e)
    {
        var app = Application.Current;
        if (app is not null)
        {
            var theme = app.ActualThemeVariant;
            app.RequestedThemeVariant = theme == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
        }
    }
}
