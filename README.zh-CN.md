# 视觉定位 Demo

[English](README.md) | [简体中文](README.zh-CN.md) | [日本語](README.ja.md) | [한국어](README.ko.md)

基于 OpenCV 的视觉定位类库和 WPF 演示程序。用于在图像中定位目标工件，并计算其相对于标准参考位置的 X/Y/R 偏移量。

## 跑起来

用 VS2022 打开 `OpenCvDemo.sln`，启动项目选 `OpenCvDemo.Demo`。

界面就三个操作：选图片 → 执行定位 → 看结果。结果会显示 X/Y/R 偏移、置信度、原始图、二值化图和识别叠加图。

编译完的东西在 `OpenCvDemo.Demo\bin\Debug\net8.0-windows\`，部署时把 exe、dll、`Config` 目录和 `runtimes` 目录一起拷过去就行。

## 项目结构

- `OpenCvDemo.Core` — 类库，图片处理、轮廓识别、偏移计算全在这
- `OpenCvDemo.Demo` — WPF 壳，调 Core 看效果用的

如果只需要定位能力，删掉 Demo 项目，只留 Core。

## 配置

配置文件：`OpenCvDemo.Demo\Config\visual-locate-settings.json`，编译自动拷到输出目录。

```json
{
  "Calibration": {
    "ScaleX": 0.05,
    "ScaleY": 0.05,
    "YDirection": -1
  },
  "StandardPosition": {
    "CenterX": 640.0,
    "CenterY": 480.0,
    "Angle": 0.0
  },
  "Roi": {
    "X": 0,
    "Y": 0,
    "Width": 0,
    "Height": 0
  },
  "Detection": {
    "GaussianKernelSize": 5,
    "MorphologyKernelSize": 3,
    "MinArea": 5000.0,
    "MaxArea": 500000.0,
    "MinAspectRatio": 0.5,
    "MaxAspectRatio": 2.0,
    "MinSolidity": 0.55,
    "InvertBinary": true,
    "MinConfidence": 0.35
  }
}
```

调参顺序参考：先把 `MinArea`/`MaxArea` 调好排掉噪点和背景 → 再用 `MinAspectRatio`/`MaxAspectRatio` 卡目标形状 → 干扰多就拉高 `MinSolidity` → 前景/背景明暗反了切 `InvertBinary`。

标定后 `ScaleX`/`ScaleY` 填实际值，`CenterX`/`CenterY`/`Angle` 用标准工件拍一张当零点。

## 接入你的项目

两种方式：

**项目引用**（推荐）—— 直接把 `OpenCvDemo.Core.csproj` 加进你的 solution，适合还要调算法的场景。

**DLL 引用** —— 从 `OpenCvDemo.Core\bin\Debug\net8.0\` 拷 `OpenCvDemo.Core.dll`、`OpenCvSharp.dll`、`OpenCvSharpExtern.dll` 到你的项目里。

调用：

```csharp
using OpenCvDemo.Core.Models;
using OpenCvDemo.Core.Services;

var settings = new VisualLocateSettingsStore()
    .Load(@"D:\Config\visual-locate-settings.json");

var result = new VisualLocator().Locate(@"D:\Images\sample.png", settings);

if (result.Result.IsFound)
{
    Console.WriteLine($"X = {result.Result.OffsetX_mm:F4} mm");
    Console.WriteLine($"Y = {result.Result.OffsetY_mm:F4} mm");
    Console.WriteLine($"R = {result.Result.OffsetR_deg:F4} °");
    Console.WriteLine($"Confidence = {result.Result.Confidence:P2}");
}
else
{
    Console.WriteLine("未识别到目标。");
}

Console.WriteLine(result.ResultJson);
```

## 返回结果

`VisualLocator.Locate()` 返回 `VisualLocateExecutionResult`：

| 字段 | 说明 |
|---|---|
| `Result` | 数值结果，见下方字段 |
| `ResultJson` | 格式化 JSON 文本 |
| `OverlayImageBytes` | 叠加识别标记的 PNG |
| `BinaryImageBytes` | 二值化 PNG |

`Result` 里的字段：

| 字段 | 说明 |
|---|---|
| `OffsetX_mm` / `OffsetY_mm` | X/Y 偏移（mm） |
| `OffsetR_deg` | 角度偏移（°） |
| `IsFound` | 是否识别成功 |
| `Confidence` | 置信度 |
| `CenterX_pix` / `CenterY_pix` | 目标中心点像素坐标 |
| `Angle_deg` | 目标当前角度 |
| `Message` | 处理信息 |

JSON 长这样：

```json
{
  "offsetX_mm": 1.235,
  "offsetY_mm": -0.842,
  "offsetR_deg": 2.135,
  "isFound": true,
  "confidence": 0.91,
  "centerX_pix": 652.4,
  "centerY_pix": 471.2,
  "angle_deg": 17.635,
  "message": "识别成功。"
}
```

## 算法流程

读图 → ROI 裁剪 → 灰度 → 高斯滤波 → OTSU 二值化 → 开闭运算去噪 → 提取外轮廓 → 面积/长宽比/实心率筛选 → `minAreaRect` 算中心点和角度 → 跟标准位置比对出 X/Y/R。
