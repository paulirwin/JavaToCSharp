using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Avalonia.Input.Platform;

namespace JavaToCSharpGui.Infrastructure;

/// <inheritdoc cref="ITextClipboard" />
internal class TextClipboard : ITextClipboard
{
    private readonly IClipboard? _clipboard;

    public TextClipboard(IClipboard? clipboard) => _clipboard = clipboard;

    /// <inheritdoc />
    public async Task SetTextAsync(string? text)
    {
        if(_clipboard is null)
        {
            return;
        }
        await _clipboard.SetTextAsync(text);
    }
}
