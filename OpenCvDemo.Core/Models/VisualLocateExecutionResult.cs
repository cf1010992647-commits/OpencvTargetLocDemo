namespace OpenCvDemo.Core.Models;

/// <summary>
/// 表示一次视觉定位执行后的完整结果。
/// </summary>
public sealed class VisualLocateExecutionResult
{
    /// <summary>
    /// 获取或设置定位结果。
    /// </summary>
    public required VisualLocateResult Result { get; init; }

    /// <summary>
    /// 获取或设置叠加结果图像字节。
    /// </summary>
    public byte[]? OverlayImageBytes { get; init; }

    /// <summary>
    /// 获取或设置二值化图像字节。
    /// </summary>
    public byte[]? BinaryImageBytes { get; init; }

    /// <summary>
    /// 获取结果对应的 JSON 文本。
    /// </summary>
    public string ResultJson => Result.ToJson();
}
