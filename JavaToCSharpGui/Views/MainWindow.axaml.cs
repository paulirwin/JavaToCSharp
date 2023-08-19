using Avalonia.Controls;
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

        string actualThemeVariant = ActualThemeVariant == ThemeVariant.Light ? nameof(ThemeVariant.Light) : nameof(ThemeVariant.Dark);
        var vm = new MainWindowViewModel(actualThemeVariant, storageProvider, dispatcher, clipboard);
        DataContext = vm;
        this.Usings.DoubleTapped += (_, _) => vm.RemoveSelectedUsingCommand.Execute(null);
    }
}