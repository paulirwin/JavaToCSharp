namespace JavaToCSharpGui.Infrastructure;

/// <summary>
/// Provides access to the host clipboard
/// </summary>
public interface ITextClipboard
{
    /// <summary>
    /// Gets the clipboard's text.
    /// </summary>
    /// <returns>A <c>Task</c> representing the async operation that returns the clipboard text.</returns>
    Task<string?> GetTextAsync();

    /// <summary>
    /// Sets the clipboard's text.
    /// </summary>
    /// <param name="text">The text to be copied to the clipboard.</param>
    /// <returns>A <c>Task</c> representing the async operation.</returns>
    Task SetTextAsync(string? text);
}
