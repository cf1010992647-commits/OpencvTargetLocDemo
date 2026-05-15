using OpenCvDemo.Core.Abstractions;
using OpenCvDemo.Core.Models;
using OpenCvSharp;

namespace OpenCvDemo.Core.Services;

/// <summary>
/// 基于 OpenCV 轮廓分析实现的视觉定位服务。
/// </summary>
public sealed class VisualLocator : IVisualLocator
{
    /// <inheritdoc />
    public VisualLocateExecutionResult Locate(string imagePath, VisualLocateSettings settings)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            throw new ArgumentException("图片路径不能为空。", nameof(imagePath));
        }

        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException("未找到待处理图片。", imagePath);
        }

        using var image = Cv2.ImRead(imagePath, ImreadModes.Color);
        return Locate(image, settings);
    }

    /// <inheritdoc />
    public VisualLocateExecutionResult Locate(Mat image, VisualLocateSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (image.Empty())
        {
            throw new ArgumentException("输入图像为空。", nameof(image));
        }

        using var colorImage = EnsureBgrImage(image);
        using var roiImage = CropRoi(colorImage, settings.Roi, out var roiRect);
        using var grayImage = CreateGrayImage(roiImage);
        using var binaryImage = CreateBinaryImage(grayImage, settings.Detection);
        var candidate = TryFindBestCandidate(binaryImage, settings.Detection);
        using var overlayImage = colorImage.Clone();
        DrawReference(overlayImage, settings.StandardPosition);

        if (candidate is null || candidate.Score < settings.Detection.MinConfidence)
        {
            return new VisualLocateExecutionResult
            {
                Result = new VisualLocateResult
                {
                    IsFound = false,
                    Confidence = candidate?.Score ?? 0,
                    Message = candidate is null ? "未识别到符合条件的目标轮廓。" : "检测到候选轮廓，但置信度低于阈值。"
                },
                OverlayImageBytes = EncodeAsPng(overlayImage),
                BinaryImageBytes = EncodeAsPng(binaryImage)
            };
        }

        var result = BuildResult(candidate, roiRect, settings);
        DrawCandidate(overlayImage, candidate, roiRect, result);

        return new VisualLocateExecutionResult
        {
            Result = result,
            OverlayImageBytes = EncodeAsPng(overlayImage),
            BinaryImageBytes = EncodeAsPng(binaryImage)
        };
    }

    /// <summary>
    /// 将图像标准化为 BGR 三通道图像。
    /// </summary>
    /// <param name="image">输入图像。</param>
    /// <returns>标准化后的图像。</returns>
    private static Mat EnsureBgrImage(Mat image)
    {
        if (image.Channels() == 3)
        {
            return image.Clone();
        }

        using var converted = new Mat();
        if (image.Channels() == 1)
        {
            Cv2.CvtColor(image, converted, ColorConversionCodes.GRAY2BGR);
        }
        else
        {
            Cv2.CvtColor(image, converted, ColorConversionCodes.BGRA2BGR);
        }

        return converted.Clone();
    }

    /// <summary>
    /// 按配置裁剪 ROI 区域。
    /// </summary>
    /// <param name="image">原始图像。</param>
    /// <param name="roi">ROI 配置。</param>
    /// <param name="roiRect">输出实际裁剪区域。</param>
    /// <returns>裁剪后的图像。</returns>
    private static Mat CropRoi(Mat image, RegionOfInterestSettings roi, out Rect roiRect)
    {
        if (!roi.IsEnabled)
        {
            roiRect = new Rect(0, 0, image.Width, image.Height);
            return image.Clone();
        }

        var x = Math.Clamp(roi.X, 0, Math.Max(image.Width - 1, 0));
        var y = Math.Clamp(roi.Y, 0, Math.Max(image.Height - 1, 0));
        var width = Math.Clamp(roi.Width, 1, image.Width - x);
        var height = Math.Clamp(roi.Height, 1, image.Height - y);
        roiRect = new Rect(x, y, width, height);
        return new Mat(image, roiRect).Clone();
    }

    /// <summary>
    /// 生成灰度图像。
    /// </summary>
    /// <param name="image">输入图像。</param>
    /// <returns>灰度图像。</returns>
    private static Mat CreateGrayImage(Mat image)
    {
        using var gray = new Mat();
        Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
        return gray.Clone();
    }

    /// <summary>
    /// 生成用于轮廓提取的二值图像。
    /// </summary>
    /// <param name="grayImage">灰度图像。</param>
    /// <param name="parameters">检测参数。</param>
    /// <returns>二值图像。</returns>
    private static Mat CreateBinaryImage(Mat grayImage, DetectionParameters parameters)
    {
        using var blurred = new Mat();
        var gaussianKernel = NormalizeKernelSize(parameters.GaussianKernelSize);
        Cv2.GaussianBlur(grayImage, blurred, new Size(gaussianKernel, gaussianKernel), 0);

        using var thresholded = new Mat();
        var thresholdType = parameters.InvertBinary
            ? ThresholdTypes.BinaryInv | ThresholdTypes.Otsu
            : ThresholdTypes.Binary | ThresholdTypes.Otsu;
        Cv2.Threshold(blurred, thresholded, 0, 255, thresholdType);

        using var kernel = Cv2.GetStructuringElement(
            MorphShapes.Rect,
            new Size(NormalizeKernelSize(parameters.MorphologyKernelSize), NormalizeKernelSize(parameters.MorphologyKernelSize)));
        using var morphed = new Mat();
        Cv2.MorphologyEx(thresholded, morphed, MorphTypes.Open, kernel);
        Cv2.MorphologyEx(morphed, morphed, MorphTypes.Close, kernel);

        return morphed.Clone();
    }

    /// <summary>
    /// 从二值图像中查找最佳候选轮廓。
    /// </summary>
    /// <param name="binaryImage">二值图像。</param>
    /// <param name="parameters">检测参数。</param>
    /// <returns>最佳候选轮廓；未找到时返回空。</returns>
    private static CandidateContour? TryFindBestCandidate(Mat binaryImage, DetectionParameters parameters)
    {
        Cv2.FindContours(binaryImage, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

        CandidateContour? best = null;
        foreach (var contour in contours)
        {
            var candidate = EvaluateContour(contour, parameters);
            if (candidate is null)
            {
                continue;
            }

            if (best is null || candidate.Score > best.Score)
            {
                best = candidate;
            }
        }

        return best;
    }

    /// <summary>
    /// 根据面积、长宽比和实心率评估轮廓。
    /// </summary>
    /// <param name="contour">待评估轮廓。</param>
    /// <param name="parameters">检测参数。</param>
    /// <returns>候选结果；不符合条件时返回空。</returns>
    private static CandidateContour? EvaluateContour(Point[] contour, DetectionParameters parameters)
    {
        var area = Cv2.ContourArea(contour);
        if (area < parameters.MinArea || area > parameters.MaxArea)
        {
            return null;
        }

        var boundingRect = Cv2.BoundingRect(contour);
        if (boundingRect.Height == 0)
        {
            return null;
        }

        var aspectRatio = (double)boundingRect.Width / boundingRect.Height;
        if (aspectRatio < parameters.MinAspectRatio || aspectRatio > parameters.MaxAspectRatio)
        {
            return null;
        }

        var rectArea = boundingRect.Width * boundingRect.Height;
        if (rectArea <= 0)
        {
            return null;
        }

        var solidity = area / rectArea;
        if (solidity < parameters.MinSolidity)
        {
            return null;
        }

        var rotatedRect = Cv2.MinAreaRect(contour);
        var angle = NormalizeAngle(rotatedRect);
        var score = Math.Round((Math.Clamp(area / parameters.MaxArea, 0, 1) * 0.4) + (Math.Clamp(solidity, 0, 1) * 0.6), 4);
        return new CandidateContour(rotatedRect, angle, score);
    }

    /// <summary>
    /// 构造最终输出结果。
    /// </summary>
    /// <param name="candidate">候选轮廓。</param>
    /// <param name="roiRect">实际 ROI 区域。</param>
    /// <param name="settings">定位配置。</param>
    /// <returns>最终结果。</returns>
    private static VisualLocateResult BuildResult(CandidateContour candidate, Rect roiRect, VisualLocateSettings settings)
    {
        var centerX = candidate.RotatedRect.Center.X + roiRect.X;
        var centerY = candidate.RotatedRect.Center.Y + roiRect.Y;
        var deltaXPixel = centerX - settings.StandardPosition.CenterX;
        var deltaYPixel = centerY - settings.StandardPosition.CenterY;

        return new VisualLocateResult
        {
            IsFound = true,
            Confidence = candidate.Score,
            CenterX_pix = Math.Round(centerX, 3),
            CenterY_pix = Math.Round(centerY, 3),
            Angle_deg = Math.Round(candidate.Angle, 4),
            OffsetX_mm = Math.Round(deltaXPixel * settings.Calibration.ScaleX, 4),
            OffsetY_mm = Math.Round(deltaYPixel * settings.Calibration.ScaleY * settings.Calibration.YDirection, 4),
            OffsetR_deg = Math.Round(NormalizeAngleDifference(candidate.Angle - settings.StandardPosition.Angle), 4),
            Message = "识别成功。"
        };
    }

    /// <summary>
    /// 在叠加图上绘制标准参考点。
    /// </summary>
    /// <param name="overlayImage">叠加图像。</param>
    /// <param name="standardPosition">标准位置配置。</param>
    private static void DrawReference(Mat overlayImage, StandardPositionSettings standardPosition)
    {
        var center = new Point((int)Math.Round(standardPosition.CenterX), (int)Math.Round(standardPosition.CenterY));
        Cv2.DrawMarker(overlayImage, center, Scalar.DeepSkyBlue, MarkerTypes.Cross, 28, 2);
        Cv2.PutText(
            overlayImage,
            $"Std ({standardPosition.CenterX:F1},{standardPosition.CenterY:F1})",
            new Point(center.X + 10, center.Y + 24),
            HersheyFonts.HersheySimplex,
            0.7,
            Scalar.DeepSkyBlue,
            2);
    }

    /// <summary>
    /// 在叠加图上绘制识别轮廓和结果文本。
    /// </summary>
    /// <param name="overlayImage">叠加图像。</param>
    /// <param name="candidate">候选轮廓。</param>
    /// <param name="roiRect">实际 ROI 区域。</param>
    /// <param name="result">数值结果。</param>
    private static void DrawCandidate(Mat overlayImage, CandidateContour candidate, Rect roiRect, VisualLocateResult result)
    {
        var vertices = candidate.RotatedRect.Points()
            .Select(point => new Point((int)Math.Round(point.X + roiRect.X), (int)Math.Round(point.Y + roiRect.Y)))
            .ToArray();

        Cv2.Polylines(overlayImage, [vertices], true, Scalar.LimeGreen, 3);

        var center = new Point((int)Math.Round(result.CenterX_pix), (int)Math.Round(result.CenterY_pix));
        Cv2.Circle(overlayImage, center, 5, Scalar.Red, -1);

        Cv2.PutText(
            overlayImage,
            $"X={result.OffsetX_mm:F3}mm  Y={result.OffsetY_mm:F3}mm  R={result.OffsetR_deg:F3}deg",
            new Point(18, 34),
            HersheyFonts.HersheySimplex,
            0.78,
            Scalar.Orange,
            2);
        Cv2.PutText(
            overlayImage,
            $"Confidence={result.Confidence:P0}",
            new Point(18, 66),
            HersheyFonts.HersheySimplex,
            0.7,
            Scalar.Orange,
            2);
    }

    /// <summary>
    /// 将图像编码为 PNG 字节数组。
    /// </summary>
    /// <param name="image">待编码图像。</param>
    /// <returns>PNG 字节数组。</returns>
    private static byte[] EncodeAsPng(Mat image)
    {
        Cv2.ImEncode(".png", image, out var bytes);
        return bytes;
    }

    /// <summary>
    /// 将核大小修正为大于零的奇数。
    /// </summary>
    /// <param name="size">原始核大小。</param>
    /// <returns>修正后的奇数核大小。</returns>
    private static int NormalizeKernelSize(int size)
    {
        var normalized = Math.Max(size, 1);
        return normalized % 2 == 0 ? normalized + 1 : normalized;
    }

    /// <summary>
    /// 归一化旋转矩形角度。
    /// </summary>
    /// <param name="rotatedRect">旋转矩形。</param>
    /// <returns>归一化后的角度。</returns>
    private static double NormalizeAngle(RotatedRect rotatedRect)
    {
        var angle = rotatedRect.Angle;
        if (rotatedRect.Size.Width < rotatedRect.Size.Height)
        {
            angle += 90;
        }

        while (angle < 0)
        {
            angle += 180;
        }

        while (angle >= 180)
        {
            angle -= 180;
        }

        return angle;
    }

    /// <summary>
    /// 将角度差归一化到 [-90, 90] 区间。
    /// </summary>
    /// <param name="angle">原始角度差。</param>
    /// <returns>归一化后的角度差。</returns>
    private static double NormalizeAngleDifference(double angle)
    {
        while (angle > 90)
        {
            angle -= 180;
        }

        while (angle < -90)
        {
            angle += 180;
        }

        return angle;
    }

    /// <summary>
    /// 表示已通过筛选的候选轮廓。
    /// </summary>
    /// <param name="RotatedRect">最小外接旋转矩形。</param>
    /// <param name="Angle">归一化角度。</param>
    /// <param name="Score">综合置信度。</param>
    private sealed record CandidateContour(RotatedRect RotatedRect, double Angle, double Score);
}
