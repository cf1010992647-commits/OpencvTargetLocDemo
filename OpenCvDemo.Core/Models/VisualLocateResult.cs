using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenCvDemo.Core.Models;

/// <summary>
/// 表示视觉定位的数值结果。
/// </summary>
public sealed class VisualLocateResult
{
    /// <summary>
    /// 获取或设置 X 方向偏移量。
    /// </summary>
    [JsonPropertyName("offsetX_mm")]
    public double OffsetX_mm { get; set; }

    /// <summary>
    /// 获取或设置 Y 方向偏移量。
    /// </summary>
    [JsonPropertyName("offsetY_mm")]
    public double OffsetY_mm { get; set; }

    /// <summary>
    /// 获取或设置角度偏移量。
    /// </summary>
    [JsonPropertyName("offsetR_deg")]
    public double OffsetR_deg { get; set; }

    /// <summary>
    /// 获取或设置是否识别成功。
    /// </summary>
    [JsonPropertyName("isFound")]
    public bool IsFound { get; set; }

    /// <summary>
    /// 获取或设置置信度。
    /// </summary>
    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    /// <summary>
    /// 获取或设置中心点 X 像素坐标。
    /// </summary>
    [JsonPropertyName("centerX_pix")]
    public double CenterX_pix { get; set; }

    /// <summary>
    /// 获取或设置中心点 Y 像素坐标。
    /// </summary>
    [JsonPropertyName("centerY_pix")]
    public double CenterY_pix { get; set; }

    /// <summary>
    /// 获取或设置归一化后的角度。
    /// </summary>
    [JsonPropertyName("angle_deg")]
    public double Angle_deg { get; set; }

    /// <summary>
    /// 获取或设置处理说明。
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 将结果转换为 JSON 文本。
    /// </summary>
    /// <returns>格式化后的 JSON 文本。</returns>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    }
}
