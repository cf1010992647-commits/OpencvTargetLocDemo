# 비주얼 포지셔닝 데모

[English](README.md) | [简体中文](README.zh-CN.md) | [日本語](README.ja.md) | [한국어](README.ko.md)

OpenCV 기반 비주얼 포지셔닝 라이브러리 및 WPF 데모 앱. 이미지에서 대상 워크피스를 감지하고 표준 기준 위치로부터의 X/Y/R 오프셋을 계산합니다.

## 빠른 시작

VS2022에서 `OpenCvDemo.sln`을 열고 `OpenCvDemo.Demo`를 시작 프로젝트로 설정하세요.

UI는 세 단계로 구성됩니다: 이미지 선택 → 위치 감지 실행 → 결과 보기. 결과에는 X/Y/R 오프셋, 신뢰도, 원본 이미지, 이진화 이미지, 오버레이 이미지가 표시됩니다.

빌드 출력은 `OpenCvDemo.Demo\bin\Debug\net8.0-windows\`에 있습니다. 배포 시 exe, dll, `Config` 디렉터리, `runtimes` 디렉터리를 함께 복사하세요.

## 프로젝트 구조

- `OpenCvDemo.Core` — 모든 이미지 처리, 윤곽 감지, 오프셋 계산 로직을 포함하는 클래스 라이브러리
- `OpenCvDemo.Demo` — Core 출력을 시각화하는 WPF 셸

포지셔닝 기능만 필요한 경우 Demo 프로젝트를 삭제하고 Core만 유지하세요.

## 설정

설정 파일: `OpenCvDemo.Demo\Config\visual-locate-settings.json` (빌드 시 출력 디렉터리로 자동 복사).

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

튜닝 가이드: 먼저 `MinArea`/`MaxArea`로 노이즈와 배경을 필터링 → `MinAspectRatio`/`MaxAspectRatio`로 대상 형태 제한 → 간섭이 많으면 `MinSolidity` 값을 높임 → 전경/배경 밝기가 반전된 경우 `InvertBinary` 전환.

캘리브레이션 후 `ScaleX`/`ScaleY`에 실제 값을 입력하고, 표준 워크피스를 캡처하여 `CenterX`/`CenterY`/`Angle`의 영점으로 사용하세요.

## 프로젝트 통합

두 가지 방법:

**프로젝트 참조** (권장) — `OpenCvDemo.Core.csproj`를 솔루션에 직접 추가. 알고리즘 조정이 필요한 경우에 적합합니다.

**DLL 참조** — `OpenCvDemo.Core\bin\Debug\net8.0\`에서 `OpenCvDemo.Core.dll`, `OpenCvSharp.dll`, `OpenCvSharpExtern.dll`을 프로젝트로 복사.

사용 예:

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
    Console.WriteLine("대상을 감지하지 못했습니다.");
}

Console.WriteLine(result.ResultJson);
```

## 반환 값

`VisualLocator.Locate()`는 `VisualLocateExecutionResult`를 반환합니다:

| 필드 | 설명 |
|---|---|
| `Result` | 수치 결과 (아래 참조) |
| `ResultJson` | 포맷된 JSON 문자열 |
| `OverlayImageBytes` | 감지 오버레이가 포함된 PNG |
| `BinaryImageBytes` | 이진화 PNG |

`Result` 내부 필드:

| 필드 | 설명 |
|---|---|
| `OffsetX_mm` / `OffsetY_mm` | X/Y 오프셋 (mm) |
| `OffsetR_deg` | 각도 오프셋 (°) |
| `IsFound` | 감지 성공 여부 |
| `Confidence` | 신뢰도 |
| `CenterX_pix` / `CenterY_pix` | 대상 중심 픽셀 좌표 |
| `Angle_deg` | 현재 대상 각도 |
| `Message` | 처리 메시지 |

JSON 출력:

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
  "message": "감지 성공."
}
```

## 알고리즘 파이프라인

이미지 읽기 → ROI 크롭 → 그레이스케일 → 가우시안 블러 → OTSU 이진화 → 모폴로지 Open/Close 노이즈 제거 → 외부 윤곽 추출 → 면적/종횡비/충실도 필터 → `minAreaRect`로 중심 및 각도 계산 → 표준 위치와 비교하여 X/Y/R 산출.
