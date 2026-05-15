using System.Windows;
using OpenCvDemo.Core.Services;
using OpenCvDemo.Demo.ViewModels;

namespace OpenCvDemo.Demo;

/// <summary>
/// 表示视觉定位演示主窗口。
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// 初始化主窗口。
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel(new VisualLocator(), new VisualLocateSettingsStore());
    }
}
