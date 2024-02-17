using Avalonia.Platform.Storage;

namespace JavaToCSharpGui.Infrastructure;

/// <summary>
/// Provides either a file or directory picker UI.
/// </summary>
public interface IHostStorageProvider
{
    /// <summary>
    /// Can the folder picker be opened on the current platform.
    /// </summary>
    bool CanPickFolder { get; }

    /// <summary>
    /// Can the file picker be opened on the current platform.
    /// </summary>
    bool CanOpen { get; }

    /// <summary>
    /// Can the save file picker be opened on the current platform.
    /// </summary>
    bool CanSave { get; }

    /// <summary>
    /// Gets the path to a well known folder.
    /// </summary>
    /// <param name="wellKnownFolder">The well known folder to search for.</param>
    /// <returns>The path to the well known folder, or <c>null</c> if not found.</returns>
    Task<IStorageFolder?> TryGetWellKnownFolderAsync(WellKnownFolder wellKnownFolder);

    /// <summary>
    /// Opens the folder picker dialog.
    /// </summary>
    /// <param name="options">The folder picker configuration.</param>
    /// <returns>A list of selected folders.</returns>
    Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options);

    /// <summary>
    /// Opens file picker dialog.
    /// </summary>
    /// <param name="options">The file picker configuration.</param>
    /// <returns>A list of selected files.</returns>
    Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options);

    /// <summary>
    /// Opens the save file picker dialog.
    /// </summary>
    /// <param name="options">The file picker configuration.</param>
    /// <returns>The selected file.</returns>
    Task<IStorageFile?> OpenSaveFileDialogAsync(FilePickerSaveOptions options);

    /// <summary>
    /// Tries to get a folder from a path.
    /// </summary>
    /// <param name="path">The path to the folder.</param>
    /// <returns>The folder, or <c>null</c> if not found.</returns>
    Task<IStorageFolder?> TryGetFolderFromPathAsync(string path);
}
