namespace OpenCvDemo.Core.Models;

/// <summary>
/// 表示识别区域配置。
/// </summary>
public sealed class RegionOfInterestSettings
{
    /// <summary>
    /// 获取或设置左上角 X 坐标。
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// 获取或设置左上角 Y 坐标。
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// 获取或设置区域宽度。
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// 获取或设置区域高度。
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// 获取当前区域是否启用。
    /// </summary>
    public bool IsEnabled => Width > 0 && Height > 0;
}
