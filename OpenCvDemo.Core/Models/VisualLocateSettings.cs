namespace OpenCvDemo.Core.Models;

/// <summary>
/// 表示视觉定位流程的完整配置。
/// </summary>
public sealed class VisualLocateSettings
{
    /// <summary>
    /// 获取或设置标定配置。
    /// </summary>
    public CalibrationSettings Calibration { get; set; } = new();

    /// <summary>
    /// 获取或设置标准位置配置。
    /// </summary>
    public StandardPositionSettings StandardPosition { get; set; } = new();

    /// <summary>
    /// 获取或设置 ROI 配置。
    /// </summary>
    public RegionOfInterestSettings Roi { get; set; } = new();

    /// <summary>
    /// 获取或设置检测参数配置。
    /// </summary>
    public DetectionParameters Detection { get; set; } = new();

    /// <summary>
    /// 创建默认配置。
    /// </summary>
    /// <returns>默认配置对象。</returns>
    public static VisualLocateSettings CreateDefault()
    {
        return new VisualLocateSettings();
    }
}
