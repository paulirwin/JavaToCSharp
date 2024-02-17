using Avalonia.Platform.Storage;

namespace JavaToCSharpGui.Infrastructure;

/// <inheritdoc cref="IHostStorageProvider" />
public class HostStorageProvider(IStorageProvider storageProvider) : IHostStorageProvider
{
    /// <inheritdoc />
    public bool CanPickFolder => storageProvider.CanPickFolder;

    /// <inheritdoc />
    public bool CanOpen => storageProvider.CanOpen;

    /// <inheritdoc />
    public bool CanSave => storageProvider.CanSave;

    /// <inheritdoc />
    public async Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options)
    {
        return await storageProvider.OpenFilePickerAsync(options);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
    {
        return await storageProvider.OpenFolderPickerAsync(options);
    }

    /// <inheritdoc />
    public async Task<IStorageFolder?> TryGetWellKnownFolderAsync(WellKnownFolder wellKnownFolder)
    {
        return await storageProvider.TryGetWellKnownFolderAsync(wellKnownFolder);
    }

    /// <inheritdoc />
    public async Task<IStorageFile?> OpenSaveFileDialogAsync(FilePickerSaveOptions options)
    {
        return await storageProvider.SaveFilePickerAsync(options);
    }

    /// <inheritdoc />
    public async Task<IStorageFolder?> TryGetFolderFromPathAsync(string path)
    {
        return await storageProvider.TryGetFolderFromPathAsync(path);
    }
}
