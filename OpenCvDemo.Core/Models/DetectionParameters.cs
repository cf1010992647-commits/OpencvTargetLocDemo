namespace OpenCvDemo.Core.Models;

/// <summary>
/// 表示视觉定位流程中的检测参数。
/// </summary>
public sealed class DetectionParameters
{
    /// <summary>
    /// 获取或设置高斯滤波核大小。
    /// </summary>
    public int GaussianKernelSize { get; set; } = 5;

    /// <summary>
    /// 获取或设置形态学核大小。
    /// </summary>
    public int MorphologyKernelSize { get; set; } = 3;

    /// <summary>
    /// 获取或设置最小轮廓面积。
    /// </summary>
    public double MinArea { get; set; } = 5000;

    /// <summary>
    /// 获取或设置最大轮廓面积。
    /// </summary>
    public double MaxArea { get; set; } = 500000;

    /// <summary>
    /// 获取或设置最小长宽比。
    /// </summary>
    public double MinAspectRatio { get; set; } = 0.5;

    /// <summary>
    /// 获取或设置最大长宽比。
    /// </summary>
    public double MaxAspectRatio { get; set; } = 2.0;

    /// <summary>
    /// 获取或设置最小实心率。
    /// </summary>
    public double MinSolidity { get; set; } = 0.55;

    /// <summary>
    /// 获取或设置是否反转二值化结果。
    /// </summary>
    public bool InvertBinary { get; set; } = true;

    /// <summary>
    /// 获取或设置最小置信度。
    /// </summary>
    public double MinConfidence { get; set; } = 0.35;
}
