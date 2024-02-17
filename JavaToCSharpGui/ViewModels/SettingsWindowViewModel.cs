using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace JavaToCSharpGui.ViewModels;

public partial class SettingsWindowViewModel : ViewModelBase
{
    [ObservableProperty] private string _addUsingInput = "";

    [ObservableProperty] private AvaloniaList<string> _usings = new(CurrentOptions.Options.Usings);

    [ObservableProperty] private string? _selectedUsing;

    [ObservableProperty] private bool _includeUsings = CurrentOptions.Options.IncludeUsings;

    [ObservableProperty] private bool _includeNamespace = CurrentOptions.Options.IncludeNamespace;

    [ObservableProperty] private bool _includeComments = CurrentOptions.Options.IncludeComments;

    [ObservableProperty] private bool _useDebugAssertForAsserts = CurrentOptions.Options.UseDebugAssertForAsserts;

    [ObservableProperty] private bool _unrecognizedCodeToComment = CurrentOptions.Options.UseUnrecognizedCodeToComment;

    [ObservableProperty] private bool _convertSystemOutToConsole = CurrentOptions.Options.ConvertSystemOutToConsole;

    public event EventHandler? CloseRequested;

    [RelayCommand]
    private void RemoveSelectedUsing()
    {
        if (SelectedUsing is not null && Usings.Contains(SelectedUsing))
        {
            Usings.Remove(SelectedUsing);
        }
    }

    [RelayCommand]
    private void AddUsing()
    {
        Usings.Add(AddUsingInput);
        AddUsingInput = string.Empty;
    }

    [RelayCommand]
    private void Save()
    {
        CurrentOptions.Options.IncludeUsings = IncludeUsings;
        CurrentOptions.Options.IncludeNamespace = IncludeNamespace;
        CurrentOptions.Options.IncludeComments = IncludeComments;
        CurrentOptions.Options.UseDebugAssertForAsserts = UseDebugAssertForAsserts;
        CurrentOptions.Options.UseUnrecognizedCodeToComment = UnrecognizedCodeToComment;
        CurrentOptions.Options.ConvertSystemOutToConsole = ConvertSystemOutToConsole;

        CurrentOptions.Options.SetUsings(Usings);

        CurrentOptions.Persist();

        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
