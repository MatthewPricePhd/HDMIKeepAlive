using System.Windows.Input;

namespace HDMIKeepAlive.UI.ViewModels;

/// <summary>
/// Async ICommand implementation for UI actions.
/// </summary>
public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<object?, CancellationToken, Task> executeAsync;
    private readonly Func<object?, bool>? canExecute;
    private bool isExecuting;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncRelayCommand"/> class.
    /// </summary>
    public AsyncRelayCommand(
        Func<object?, CancellationToken, Task> executeAsync,
        Func<object?, bool>? canExecute = null)
    {
        this.executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        this.canExecute = canExecute;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc />
    public bool CanExecute(object? parameter)
    {
        return !isExecuting && (canExecute?.Invoke(parameter) ?? true);
    }

    /// <inheritdoc />
    public async void Execute(object? parameter)
    {
        await ExecuteAsync(parameter).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the command asynchronously.
    /// </summary>
    public async Task ExecuteAsync(object? parameter, CancellationToken cancellationToken = default)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        isExecuting = true;
        RaiseCanExecuteChanged();
        try
        {
            await executeAsync(parameter, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// Raises command availability change notification.
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
