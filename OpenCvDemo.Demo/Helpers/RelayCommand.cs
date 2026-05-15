using System.Windows.Input;

namespace OpenCvDemo.Demo.Helpers;

/// <summary>
/// 提供同步命令的基础实现。
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    /// <summary>
    /// 初始化同步命令。
    /// </summary>
    /// <param name="execute">执行逻辑。</param>
    /// <param name="canExecute">可执行判断。</param>
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
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
        return _canExecute?.Invoke() ?? true;
    }

    /// <summary>
    /// 执行命令。
    /// </summary>
    /// <param name="parameter">命令参数。</param>
    public void Execute(object? parameter)
    {
        _execute();
    }

    /// <summary>
    /// 主动刷新命令状态。
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
