using Avalonia.Controls;
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
        this.Usings.DoubleTapped += (_, _) => vm.RemoveSelectedUsingCommand.Execute(null);
    }
}