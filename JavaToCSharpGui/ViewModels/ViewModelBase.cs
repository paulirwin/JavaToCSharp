using CommunityToolkit.Mvvm.ComponentModel;

namespace JavaToCSharpGui.ViewModels;

public partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private string _displayName = "";
}
