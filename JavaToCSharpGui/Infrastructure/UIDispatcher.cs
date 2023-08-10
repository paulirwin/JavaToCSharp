namespace JavaToCSharpGui.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Avalonia.Threading;

/// <inheritdoc cref="IUIDispatcher" />
public class UIDispatcher : IUIDispatcher
{
    private readonly IDispatcher _dispatcher;

    public UIDispatcher(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    /// <inheritdoc/>
    public async Task InvokeAsync(Action callback, DispatcherPriority priority)
    {
        if (_dispatcher is Dispatcher avaloniaDispatcher)
        {
            await avaloniaDispatcher.InvokeAsync(callback, priority);
        }
    }
}
