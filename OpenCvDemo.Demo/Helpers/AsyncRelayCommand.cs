using System.Windows.Input;

namespace OpenCvDemo.Demo.Helpers;

/// <summary>
/// 提供异步命令的基础实现。
/// </summary>
public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _executeAsync;
    private readonly Func<bool>? _canExecute;
    private bool _isExecuting;

    /// <summary>
    /// 初始化异步命令。
    /// </summary>
    /// <param name="executeAsync">异步执行逻辑。</param>
    /// <param name="canExecute">可执行判断。</param>
    public AsyncRelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
    {
        _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        _canExecute = canExecute;
    }

    /// <summary>
    /// 当命令状态变化时触发。
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// 判断命令是否可执行。
    /// </summary>
    /// <param name="parameter">命令参数。</param>
    /// <returns>是否可执行。</returns>
    public bool CanExecute(object? parameter)
    {
        return !_isExecuting && (_canExecute?.Invoke() ?? true);
    }

    /// <summary>
    /// 执行命令。
    /// </summary>
    /// <param name="parameter">命令参数。</param>
    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        try
        {
            _isExecuting = true;
            RaiseCanExecuteChanged();
            await _executeAsync().ConfigureAwait(true);
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// 主动刷新命令状态。
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
