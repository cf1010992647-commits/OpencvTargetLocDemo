using OpenCvDemo.Core.Models;
using OpenCvSharp;

namespace OpenCvDemo.Core.Abstractions;

/// <summary>
/// 定义视觉定位服务的统一入口。
/// </summary>
public interface IVisualLocator
{
    /// <summary>
    /// 基于图片路径执行视觉定位。
    /// </summary>
    /// <param name="imagePath">待处理图片路径。</param>
    /// <param name="settings">定位所需配置。</param>
    /// <returns>包含数值结果和调试图像的执行结果。</returns>
    VisualLocateExecutionResult Locate(string imagePath, VisualLocateSettings settings);

    /// <summary>
    /// 基于图像对象执行视觉定位。
    /// </summary>
    /// <param name="image">待处理图像。</param>
    /// <param name="settings">定位所需配置。</param>
    /// <returns>包含数值结果和调试图像的执行结果。</returns>
    VisualLocateExecutionResult Locate(Mat image, VisualLocateSettings settings);
}
