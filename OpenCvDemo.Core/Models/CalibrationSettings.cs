namespace OpenCvDemo.Core.Models;

/// <summary>
/// 表示像素坐标到物理坐标的标定参数。
/// </summary>
public sealed class CalibrationSettings
{
    /// <summary>
    /// 获取或设置 X 方向毫米换算系数。
    /// </summary>
    public double ScaleX { get; set; } = 0.05;

    /// <summary>
    /// 获取或设置 Y 方向毫米换算系数。
    /// </summary>
    public double ScaleY { get; set; } = 0.05;

    /// <summary>
    /// 获取或设置 Y 方向符号。
    /// </summary>
    public int YDirection { get; set; } = -1;
}
