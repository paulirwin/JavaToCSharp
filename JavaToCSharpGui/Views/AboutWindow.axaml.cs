using System.Diagnostics;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Input;

namespace JavaToCSharpGui.Views;

public partial class AboutWindow : Window
{
    private readonly string _version;

    public AboutWindow()
    {
        var assembly = Assembly.GetExecutingAssembly();
        _version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                   ?? assembly.GetName().Version?.ToString()
                   ?? "Unknown";

        InitializeComponent();
        DataContext = this;


    }

    public string VersionString => $"Version {_version}";

    private void GitHubLinkTapped(object? sender, TappedEventArgs e) => Process.Start(new ProcessStartInfo
    {
        FileName = "https://github.com/paulirwin/javatocsharp",
        UseShellExecute = true
    });
}
