# ビジュアルポジショニング デモ

[English](README.md) | [简体中文](README.zh-CN.md) | [日本語](README.ja.md) | [한국어](README.ko.md)

OpenCV ベースのビジュアルポジショニングライブラリと WPF デモアプリ。画像内の対象ワークを検出し、標準基準位置からの X/Y/R オフセットを計算します。

## クイックスタート

VS2022 で `OpenCvDemo.sln` を開き、`OpenCvDemo.Demo` をスタートアッププロジェクトに設定します。

UI の操作は 3 ステップ：画像選択 → 位置検出実行 → 結果表示。結果には X/Y/R オフセット、信頼度、元画像、二値化画像、オーバーレイ画像が表示されます。

ビルド出力は `OpenCvDemo.Demo\bin\Debug\net8.0-windows\` にあります。デプロイ時は exe、dll、`Config` ディレクトリ、`runtimes` ディレクトリをまとめてコピーしてください。

## プロジェクト構成

- `OpenCvDemo.Core` — 画像処理、輪郭検出、オフセット計算の全ロジックを含むクラスライブラリ
- `OpenCvDemo.Demo` — Core の出力を可視化する WPF シェル

位置検出機能のみ必要な場合は、Demo プロジェクトを削除して Core だけを残してください。

## 設定

設定ファイル：`OpenCvDemo.Demo\Config\visual-locate-settings.json`（ビルド時に出力ディレクトリへ自動コピー）。

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

調整ガイド：まず `MinArea`/`MaxArea` でノイズと背景を除去 → `MinAspectRatio`/`MaxAspectRatio` で対象形状を制約 → 干渉が多い場合は `MinSolidity` を上げる → 前景/背景の明暗が逆の場合は `InvertBinary` を切り替え。

キャリブレーション後、`ScaleX`/`ScaleY` に実際の値を入力し、標準ワークを撮影して `CenterX`/`CenterY`/`Angle` のゼロ点として使用します。

## プロジェクトへの統合

2 つの方法：

**プロジェクト参照**（推奨）— `OpenCvDemo.Core.csproj` をソリューションに直接追加。アルゴリズム調整が必要な場合に適しています。

**DLL 参照** — `OpenCvDemo.Core\bin\Debug\net8.0\` から `OpenCvDemo.Core.dll`、`OpenCvSharp.dll`、`OpenCvSharpExtern.dll` をプロジェクトにコピー。

使用例：

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
    Console.WriteLine("対象が検出されませんでした。");
}

Console.WriteLine(result.ResultJson);
```

## 戻り値

`VisualLocator.Locate()` は `VisualLocateExecutionResult` を返します：

| フィールド | 説明 |
|---|---|
| `Result` | 数値結果（下記参照） |
| `ResultJson` | フォーマット済み JSON 文字列 |
| `OverlayImageBytes` | 検出オーバーレイ付き PNG |
| `BinaryImageBytes` | 二値化 PNG |

`Result` のフィールド：

| フィールド | 説明 |
|---|---|
| `OffsetX_mm` / `OffsetY_mm` | X/Y オフセット（mm） |
| `OffsetR_deg` | 角度オフセット（°） |
| `IsFound` | 検出成功かどうか |
| `Confidence` | 信頼度 |
| `CenterX_pix` / `CenterY_pix` | 対象中心のピクセル座標 |
| `Angle_deg` | 現在の対象角度 |
| `Message` | 処理メッセージ |

JSON 出力：

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
  "message": "検出成功。"
}
```

## アルゴリズムパイプライン

画像読み込み → ROI クロップ → グレースケール → ガウシアンブラー → OTSU 二値化 → モルフォロジー Open/Close ノイズ除去 → 外部輪郭抽出 → 面積/アスペクト比/充実度フィルタ → `minAreaRect` で中心と角度を計算 → 標準位置と比較して X/Y/R を算出。
