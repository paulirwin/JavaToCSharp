namespace JavaToCSharpGui.Infrastructure;
using System;
using System.Threading.Tasks;

using Avalonia.Threading;

/// <inheritdoc cref="IUIDispatcher" />
public class UIDispatcher(IDispatcher dispatcher) : IUIDispatcher
{
    /// <inheritdoc/>
    public async Task InvokeAsync(Action callback, DispatcherPriority priority)
    {
        if (dispatcher is Dispatcher avaloniaDispatcher)
        {
            await avaloniaDispatcher.InvokeAsync(callback, priority);
        }
    }
}
