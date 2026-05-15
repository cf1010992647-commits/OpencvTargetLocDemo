using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using OpenCvDemo.Core.Abstractions;
using OpenCvDemo.Core.Models;
using OpenCvDemo.Core.Services;
using OpenCvDemo.Demo.Helpers;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace OpenCvDemo.Demo.ViewModels;

/// <summary>
/// 表示主窗口的视图模型。
/// </summary>
public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly IVisualLocator _visualLocator;
    private readonly VisualLocateSettingsStore _settingsStore;
    private string _selectedImagePath = string.Empty;
    private string _settingsPath = string.Empty;
    private string _statusMessage = "请选择图片后执行定位。";
    private string _resultJson = string.Empty;
    private string _offsetX = "--";
    private string _offsetY = "--";
    private string _offsetR = "--";
    private string _confidence = "--";
    private BitmapSource? _originalImage;
    private BitmapSource? _binaryImage;
    private BitmapSource? _overlayImage;

    /// <summary>
    /// 初始化主窗口视图模型。
    /// </summary>
    /// <param name="visualLocator">视觉定位服务。</param>
    /// <param name="settingsStore">配置文件服务。</param>
    public MainWindowViewModel(IVisualLocator visualLocator, VisualLocateSettingsStore settingsStore)
    {
        _visualLocator = visualLocator ?? throw new ArgumentNullException(nameof(visualLocator));
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));

        SelectImageCommand = new RelayCommand(SelectImage);
        RunLocateCommand = new AsyncRelayCommand(RunLocateAsync, CanRunLocate);
        OpenConfigFolderCommand = new RelayCommand(OpenConfigFolder);
        ResetConfigCommand = new RelayCommand(ResetConfigFile);

        InitializeSettingsFile();
    }

    /// <summary>
    /// 当属性变化时触发。
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 获取选择图片命令。
    /// </summary>
    public ICommand SelectImageCommand { get; }

    /// <summary>
    /// 获取执行定位命令。
    /// </summary>
    public ICommand RunLocateCommand { get; }

    /// <summary>
    /// 获取打开配置目录命令。
    /// </summary>
    public ICommand OpenConfigFolderCommand { get; }

    /// <summary>
    /// 获取重置配置命令。
    /// </summary>
    public ICommand ResetConfigCommand { get; }

    /// <summary>
    /// 获取或设置当前图片路径。
    /// </summary>
    public string SelectedImagePath
    {
        get => _selectedImagePath;
        set
        {
            if (SetProperty(ref _selectedImagePath, value))
            {
                RaiseLocateCommandState();
            }
        }
    }

    /// <summary>
    /// 获取当前配置文件路径。
    /// </summary>
    public string SettingsPath
    {
        get => _settingsPath;
        private set => SetProperty(ref _settingsPath, value);
    }

    /// <summary>
    /// 获取或设置状态文本。
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// 获取或设置结果 JSON。
    /// </summary>
    public string ResultJson
    {
        get => _resultJson;
        set => SetProperty(ref _resultJson, value);
    }

    /// <summary>
    /// 获取或设置 X 偏移显示文本。
    /// </summary>
    public string OffsetX
    {
        get => _offsetX;
        set => SetProperty(ref _offsetX, value);
    }

    /// <summary>
    /// 获取或设置 Y 偏移显示文本。
    /// </summary>
    public string OffsetY
    {
        get => _offsetY;
        set => SetProperty(ref _offsetY, value);
    }

    /// <summary>
    /// 获取或设置 R 偏移显示文本。
    /// </summary>
    public string OffsetR
    {
        get => _offsetR;
        set => SetProperty(ref _offsetR, value);
    }

    /// <summary>
    /// 获取或设置置信度显示文本。
    /// </summary>
    public string Confidence
    {
        get => _confidence;
        set => SetProperty(ref _confidence, value);
    }

    /// <summary>
    /// 获取或设置原始图像预览。
    /// </summary>
    public BitmapSource? OriginalImage
    {
        get => _originalImage;
        set => SetProperty(ref _originalImage, value);
    }

    /// <summary>
    /// 获取或设置二值化图像预览。
    /// </summary>
    public BitmapSource? BinaryImage
    {
        get => _binaryImage;
        set => SetProperty(ref _binaryImage, value);
    }

    /// <summary>
    /// 获取或设置叠加结果图像预览。
    /// </summary>
    public BitmapSource? OverlayImage
    {
        get => _overlayImage;
        set => SetProperty(ref _overlayImage, value);
    }

    /// <summary>
    /// 触发属性变化通知。
    /// </summary>
    /// <param name="propertyName">属性名称。</param>
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 初始化配置文件路径并确保默认文件存在。
    /// </summary>
    private void InitializeSettingsFile()
    {
        SettingsPath = Path.Combine(AppContext.BaseDirectory, "Config", "visual-locate-settings.json");
        EnsureDefaultSettingsFile();
    }

    /// <summary>
    /// 确保默认配置文件存在。
    /// </summary>
    private void EnsureDefaultSettingsFile()
    {
        if (!File.Exists(SettingsPath))
        {
            _settingsStore.Save(SettingsPath, VisualLocateSettings.CreateDefault());
        }
    }

    /// <summary>
    /// 选择待处理图片。
    /// </summary>
    private void SelectImage()
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择待识别图片",
            Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp|所有文件|*.*"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        SelectedImagePath = dialog.FileName;
        LoadOriginalPreview(dialog.FileName);
        StatusMessage = "图片已加载，可以执行定位。";
    }

    /// <summary>
    /// 加载原始图片预览。
    /// </summary>
    /// <param name="imagePath">图片路径。</param>
    private void LoadOriginalPreview(string imagePath)
    {
        using var image = Cv2.ImRead(imagePath, ImreadModes.Color);
        OriginalImage = ConvertToBitmapSource(image);
    }

    /// <summary>
    /// 判断是否允许执行定位。
    /// </summary>
    /// <returns>是否允许执行。</returns>
    private bool CanRunLocate()
    {
        return !string.IsNullOrWhiteSpace(SelectedImagePath);
    }

    /// <summary>
    /// 执行视觉定位。
    /// </summary>
    /// <returns>异步任务。</returns>
    private async Task RunLocateAsync()
    {
        try
        {
            StatusMessage = "正在执行定位...";
            await Task.Yield();

            EnsureDefaultSettingsFile();
            var settings = _settingsStore.Load(SettingsPath);
            var executionResult = _visualLocator.Locate(SelectedImagePath, settings);

            BinaryImage = ConvertFromBytes(executionResult.BinaryImageBytes);
            OverlayImage = ConvertFromBytes(executionResult.OverlayImageBytes);
            ResultJson = executionResult.ResultJson;
            UpdateSummary(executionResult.Result);
            StatusMessage = executionResult.Result.Message;
        }
        catch (Exception ex)
        {
            ResultJson = string.Empty;
            OffsetX = "--";
            OffsetY = "--";
            OffsetR = "--";
            Confidence = "--";
            StatusMessage = $"处理失败：{ex.Message}";
        }
    }

    /// <summary>
    /// 更新结果摘要文本。
    /// </summary>
    /// <param name="result">定位结果。</param>
    private void UpdateSummary(VisualLocateResult result)
    {
        OffsetX = $"{result.OffsetX_mm:F4} mm";
        OffsetY = $"{result.OffsetY_mm:F4} mm";
        OffsetR = $"{result.OffsetR_deg:F4} °";
        Confidence = $"{result.Confidence:P2}";
    }

    /// <summary>
    /// 打开配置文件所在目录。
    /// </summary>
    private void OpenConfigFolder()
    {
        EnsureDefaultSettingsFile();
        System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{SettingsPath}\"",
                UseShellExecute = true
            });
    }

    /// <summary>
    /// 将配置重置为默认值。
    /// </summary>
    private void ResetConfigFile()
    {
        _settingsStore.Save(SettingsPath, VisualLocateSettings.CreateDefault());
        StatusMessage = "默认配置已重置，请按实际样本继续调整。";
    }

    /// <summary>
    /// 刷新执行定位命令状态。
    /// </summary>
    private void RaiseLocateCommandState()
    {
        if (RunLocateCommand is AsyncRelayCommand asyncRelayCommand)
        {
            asyncRelayCommand.RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// 设置属性值并触发通知。
    /// </summary>
    /// <typeparam name="T">属性类型。</typeparam>
    /// <param name="field">字段引用。</param>
    /// <param name="value">新值。</param>
    /// <param name="propertyName">属性名称。</param>
    /// <returns>是否发生更新。</returns>
    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// 将 OpenCV 图像转换为 WPF 位图。
    /// </summary>
    /// <param name="image">OpenCV 图像。</param>
    /// <returns>WPF 位图。</returns>
    private static BitmapSource ConvertToBitmapSource(Mat image)
    {
        var bitmap = image.ToBitmapSource();
        bitmap.Freeze();
        return bitmap;
    }

    /// <summary>
    /// 从字节数组恢复 WPF 位图。
    /// </summary>
    /// <param name="bytes">图像字节数组。</param>
    /// <returns>WPF 位图；为空时返回空。</returns>
    private static BitmapSource? ConvertFromBytes(byte[]? bytes)
    {
        if (bytes is null || bytes.Length == 0)
        {
            return null;
        }

        using var image = Cv2.ImDecode(bytes, ImreadModes.Color);
        return ConvertToBitmapSource(image);
    }
}
