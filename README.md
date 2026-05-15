# Visual Positioning Demo

[English](README.md) | [简体中文](README.zh-CN.md) | [日本語](README.ja.md) | [한국어](README.ko.md)

OpenCV-based visual positioning library and WPF demo. Locates a target workpiece in an image and computes X/Y/R offset from a standard reference position.

## Quick Start

Open `OpenCvDemo.sln` in VS2022, set `OpenCvDemo.Demo` as the startup project.

The UI has three steps: Select Image → Execute Locate → View Results. Results include X/Y/R offset, confidence score, original image, binary image, and overlay image.

Build output is under `OpenCvDemo.Demo\bin\Debug\net8.0-windows\`. For deployment, copy the exe, dlls, `Config` directory, and `runtimes` directory together.

## Project Structure

- `OpenCvDemo.Core` — Class library containing all image processing, contour detection, and offset calculation logic
- `OpenCvDemo.Demo` — WPF shell for visualizing Core outputs

If you only need the positioning capability, remove the Demo project and keep Core.

## Configuration

Config file: `OpenCvDemo.Demo\Config\visual-locate-settings.json` (auto-copied to output directory).

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

Tuning guide: adjust `MinArea`/`MaxArea` first to filter noise and background → use `MinAspectRatio`/`MaxAspectRatio` to constrain target shape → raise `MinSolidity` if there's interference → toggle `InvertBinary` if foreground/background brightness is inverted.

After calibration, fill in actual values for `ScaleX`/`ScaleY`, and capture a standard workpiece to use as the zero point for `CenterX`/`CenterY`/`Angle`.

## Integration

Two options:

**Project reference** (recommended) — Add `OpenCvDemo.Core.csproj` directly into your solution.

**DLL reference** — Copy `OpenCvDemo.Core.dll`, `OpenCvSharp.dll`, `OpenCvSharpExtern.dll` from `OpenCvDemo.Core\bin\Debug\net8.0\` to your project.

Usage:

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
    Console.WriteLine("Target not found.");
}

Console.WriteLine(result.ResultJson);
```

## Return Values

`VisualLocator.Locate()` returns `VisualLocateExecutionResult`:

| Field | Description |
|---|---|
| `Result` | Numerical results (see below) |
| `ResultJson` | Formatted JSON string |
| `OverlayImageBytes` | PNG with detection overlay |
| `BinaryImageBytes` | Binary PNG |

Fields inside `Result`:

| Field | Description |
|---|---|
| `OffsetX_mm` / `OffsetY_mm` | X/Y offset (mm) |
| `OffsetR_deg` | Angular offset (°) |
| `IsFound` | Whether detection succeeded |
| `Confidence` | Confidence score |
| `CenterX_pix` / `CenterY_pix` | Target center pixel coordinates |
| `Angle_deg` | Current target angle |
| `Message` | Processing message |

JSON output:

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
  "message": "Detection successful."
}
```

## Algorithm Pipeline

Read Image → ROI Crop → Grayscale → Gaussian Blur → OTSU Binarization → Morphological Open/Close Denoising → Extract External Contours → Filter by Area/Aspect Ratio/Solidity → `minAreaRect` for Center & Angle → Compare Against Standard Position for X/Y/R.
