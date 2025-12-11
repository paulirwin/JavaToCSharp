using Avalonia.Input.Platform;

namespace JavaToCSharpGui.Infrastructure;

/// <inheritdoc cref="ITextClipboard" />
internal class TextClipboard(IClipboard? clipboard) : ITextClipboard
{
    /// <inheritdoc />
    public async Task<string?> GetTextAsync()
    {
        if (clipboard is null)
        {
            return null;
        }
        return await clipboard.TryGetTextAsync();
    }

    /// <inheritdoc />
    public async Task SetTextAsync(string? text)
    {
        if(clipboard is null)
        {
            return;
        }
        await clipboard.SetTextAsync(text);
    }
}
