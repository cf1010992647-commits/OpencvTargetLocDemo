namespace OpenCvDemo.Core.Models;

/// <summary>
/// 表示标准位置参考点参数。
/// </summary>
public sealed class StandardPositionSettings
{
    /// <summary>
    /// 获取或设置标准中心点 X 坐标。
    /// </summary>
    public double CenterX { get; set; } = 640;

    /// <summary>
    /// 获取或设置标准中心点 Y 坐标。
    /// </summary>
    public double CenterY { get; set; } = 480;

    /// <summary>
    /// 获取或设置标准角度。
    /// </summary>
    public double Angle { get; set; }
}
