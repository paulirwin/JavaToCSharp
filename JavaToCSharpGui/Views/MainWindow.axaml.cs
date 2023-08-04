using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

using JavaToCSharpGui.ViewModels;

using System;

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
        DataContext = new MainWindowViewModel((IClassicDesktopStyleApplicationLifetime?)App.Current?.ApplicationLifetime);
    }
}