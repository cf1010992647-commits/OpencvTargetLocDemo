using System.Text.Json;
using OpenCvDemo.Core.Models;

namespace OpenCvDemo.Core.Services;

/// <summary>
/// 提供视觉定位配置文件读写能力。
/// </summary>
public sealed class VisualLocateSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// 从文件中加载配置。
    /// </summary>
    /// <param name="filePath">配置文件路径。</param>
    /// <returns>反序列化后的配置对象。</returns>
    public VisualLocateSettings Load(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("配置文件路径不能为空。", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("未找到配置文件。", filePath);
        }

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<VisualLocateSettings>(json, JsonOptions) ?? VisualLocateSettings.CreateDefault();
    }

    /// <summary>
    /// 将配置保存到文件。
    /// </summary>
    /// <param name="filePath">配置文件路径。</param>
    /// <param name="settings">待保存配置。</param>
    public void Save(string filePath, VisualLocateSettings settings)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("配置文件路径不能为空。", nameof(filePath));
        }

        ArgumentNullException.ThrowIfNull(settings);

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(filePath, json);
    }
}
