using Avalonia.Threading;

namespace JavaToCSharpGui.Infrastructure;

/// <summary>
/// Provides access to the UI thread.
/// </summary>
public interface IUIDispatcher
{
    /// <summary>
    /// Runs the <paramref name="callback"/> on the UI thread.
    /// </summary>
    /// <param name="callback">The code to run.</param>
    /// <param name="priority">The priority attached to the code.</param>
    /// <returns>A <c>Task</c> representing the async operation.</returns>
    Task InvokeAsync(Action callback, DispatcherPriority priority);
}
