# HimeBot 初步项目结构

## 1. 项目目标

HimeBot 是一个基于 C# 和 WPF 的游戏 OCR 自动化程序。初期面向“恋姬大战”，完成以下闭环：

1. 定位并截取游戏窗口。
2. 识别当前场景、文字、数字和可交互目标。
3. 将识别结果转换成结构化游戏状态。
4. 由任务状态机决定下一步语义操作。
5. 执行鼠标或键盘输入。
6. 再次识别画面，验证操作结果。
7. 失败时记录证据并执行有限重试或安全停止。

项目采用模块化单体结构。WPF 只负责界面展示和用户操作，OCR、任务决策以及 Windows 输入均位于独立模块中。

## 2. 技术基线

- 运行时：`.NET 7`
- 目标框架：`net7.0-windows10.0.19041.0`
- 桌面界面：WPF
- 架构方式：MVVM
- 默认平台：x64
- MVVM 工具：`CommunityToolkit.Mvvm`
- Direct3D 11 绑定：`Vortice.Direct3D11` 3.5.0
- 依赖注入、配置和日志：`Microsoft.Extensions.*`
- 测试框架：计划使用 xUnit，当前尚未创建测试项目
- 图像处理：`OpenCvSharp4.Windows` 4.13.0.20261107
- OCR 引擎：`TesseractOCR` 5.3.5，当前使用 `tessdata_fast` 的日文模型

OCR 和图像处理库包含原生依赖，因此初期统一使用 x64，不使用 AnyCPU。

`.NET 7` 是当前项目的明确约束。除非项目所有者主动修改该决定，否则项目文件、依赖选择和示例代码都保持兼容 `.NET 7`。目标框架中的 Windows 10 版本用于开放 Windows Graphics Capture 所需的 Windows SDK 接口，不表示升级到更高的 .NET 版本。

`TesseractOCR` 固定为 5.3.5。曾验证的 5.5.2 会依赖 `Microsoft.Extensions.Logging.Abstractions` 10.0.3，并传递产生若干“不支持 net7.0”的构建警告；5.3.5 使用与项目基线同代的日志抽象 7.0.1，现有 OCR API 无需改写，构建中的相关支持警告已经消失。该降级同时把原生 Tesseract 从 5.5.1 改为 5.3.1，因此仍需保留真实 ROI 人工回归，而不能只以编译成功作为完成标准。

## 3. Solution 目录

```text
HimeBot/
├─ AGENTS.md
├─ PROJECT_STATUS.md
├─ docs/
│  ├─ project-structure.md
│  ├─ decisions/
│  │  └─ 0001-use-windows-graphics-capture.md
│  └─ guides/
│     └─ windows-graphics-capture.md
│
└─ Tutorial/                   # Agent 生成的教学参考项目
   ├─ HimeBot.sln
   ├─ Directory.Build.props
   ├─ Directory.Packages.props
   ├─ .editorconfig
   ├─ README.md
   │
   ├─ src/
   │  ├─ HimeBot.App/
   │  ├─ HimeBot.Core/
   │  ├─ HimeBot.Game.Koihime/
   │  ├─ HimeBot.Platform.Windows/
   │  └─ HimeBot.Vision/
   │
   ├─ tests/
   │  ├─ HimeBot.Game.Koihime.Tests/
   │  ├─ HimeBot.Vision.Tests/
   │  └─ HimeBot.Platform.Windows.Tests/
   │
   └─ assets/
      └─ koihime/
         ├─ templates/
         ├─ samples/
         └─ profiles/
```

`Tutorial/` 是 Agent 生成的教学参考项目目录；根目录保留协作规范和架构文档。这是目标目录，而不是第一步必须全部创建的目录。项目按实际学习和实现进度逐步添加，避免在还没有可运行功能时生成大量空项目。

### 3.1 当前已创建的最小架构

用户实际动手开发的解决方案目前位于仓库根目录的 `HimeBot.sln` 和 `src/`；`Tutorial/` 继续作为智能体生成参考实现的独立工作区。实际项目当前包含：

- `HimeBot.App`：WPF 程序入口、启动目标选择、窗口与截图诊断命令、OCR 与粮食解析诊断命令和状态显示。
- `HimeBot.Core`：游戏启动与窗口定位的结果模型和外部边界接口。
- `HimeBot.Game.Koihime`：恋姬大战专用的粮食库存模型、OCR 文字规范化与数值解析。
- `HimeBot.Platform.Windows`：DMM Shell 启动、Windows 进程窗口定位、客户区尺寸读取与调整、单帧截图和 `SendInput` 鼠标输入。
- `HimeBot.Vision`：使用 OpenCvSharp 生成灰度图、Otsu 二值图和矩形 ROI 副本，并使用 Tesseract 识别日文单行文字。

当前引用方向为：

```text
HimeBot.App
├─> HimeBot.Game.Koihime
├─> HimeBot.Platform.Windows
├─> HimeBot.Vision
└─> HimeBot.Core

HimeBot.Platform.Windows ──> HimeBot.Core
```

当前代码按以下职责放置：

| 职责 | 目录 | 当前状态 |
|---|---|---|
| 选择 DMM 快捷方式、启动、查找窗口和显示状态 | `src/HimeBot.App/` | 已实现并人工验证 |
| 启动游戏的 interface 与结果模型 | `src/HimeBot.Core/Ports/`、`src/HimeBot.Core/Launching/` | 已实现 |
| 窗口定位的 interface 与结果模型 | `src/HimeBot.Core/Ports/`、`src/HimeBot.Core/Windowing/` | 已实现 |
| Shell 启动 adapter | `src/HimeBot.Platform.Windows/Launching/` | 已实现并人工验证 |
| Windows 窗口定位与 Win32 客户区读取 | `src/HimeBot.Platform.Windows/Windowing/`、`src/HimeBot.Platform.Windows/Native/` | 已实现并人工验证 |
| Windows 窗口置顶与取消置顶 | `src/HimeBot.Platform.Windows/Windowing/`、`src/HimeBot.Platform.Windows/Native/` | 已接入 WPF，并完成构建和人工运行验证 |
| Windows 客户区尺寸调整 | `src/HimeBot.Platform.Windows/Windowing/`、`src/HimeBot.Platform.Windows/Native/` | 已验证尺寸下拉菜单已完成构建和人工运行验证 |
| Windows 游戏窗口居中 | `src/HimeBot.Platform.Windows/Windowing/`、`src/HimeBot.Platform.Windows/Native/` | 待办：移动到当前显示器工作区中心，尚未实现 |
| 单帧截图与 Windows Graphics Capture | `src/HimeBot.Platform.Windows/ScreenCapture/` | WGC 遮挡隔离与客户区动态裁剪均已人工运行验证 |
| 客户区坐标鼠标移动与点击 | `src/HimeBot.Platform.Windows/Input/` | `SendInput` 移动和左键点击均已实现并人工验证 |
| 灰度图、Otsu 二值图与矩形 ROI 裁剪 | `src/HimeBot.Vision/ImageProcessing/` | 已通过 MVVM 命令接入 WPF；资源数字 ROI 已人工验证 |
| 日文单行 OCR | `src/HimeBot.Vision/Ocr/`、`src/HimeBot.Vision/tessdata/` | 已接入 WPF；真实资源 ROI 已正确识别 `236万/454万` |
| 粮食文字规范化与数值解析 | `src/HimeBot.Game.Koihime/Domain/`、`src/HimeBot.Game.Koihime/Perception/` | 已接入现有 OCR 命令并完成构建；真实 ROI 的解析显示待人工验证 |

`ScreenCapture/ClientCropRectangleCalculator.cs` 是为后续学习测试保留的内部纯计算类。它目前尚未接入生产截图路径，不代表现有裁剪行为已经重构；`WindowsGameScreenshotCapture` 仍使用自身的 `TryGetClientCropRectangle` 完成 Win32 查询、坐标计算和越界校验。

`HimeBot.Game.Koihime` 已在第一次需要游戏专用文字含义时创建。当前游戏模块只包含粮食库存模型和纯文字解析，不需要 Core 类型，因此暂时不引用 `HimeBot.Core`；测试项目仍未创建。

## 4. 项目引用方向

```text
HimeBot.App
├─> HimeBot.Game.Koihime
├─> HimeBot.Platform.Windows
└─> HimeBot.Vision

HimeBot.Game.Koihime ──────> HimeBot.Core
HimeBot.Platform.Windows ──> HimeBot.Core
HimeBot.Vision ────────────> HimeBot.Core
```

规则：

- `HimeBot.App` 是 composition root，负责组装生产 adapters。
- `HimeBot.Core` 不引用其他项目。
- 游戏模块不能引用 WPF。
- Windows 和 Vision 模块不能引用具体游戏模块。
- 禁止循环引用。

## 5. 模块职责

### 5.1 HimeBot.App

WPF 程序入口，只负责界面、数据绑定和模块组装。

```text
HimeBot.App/
├─ App.xaml
├─ App.xaml.cs
├─ Views/
│  ├─ MainWindow.xaml
│  ├─ DashboardView.xaml
│  ├─ LivePreviewView.xaml
│  ├─ RunLogView.xaml
│  └─ SettingsView.xaml
├─ ViewModels/
│  ├─ MainViewModel.cs
│  ├─ DashboardViewModel.cs
│  ├─ LivePreviewViewModel.cs
│  └─ SettingsViewModel.cs
├─ Converters/
└─ Resources/
```

约束：

- View 不直接调用 OCR、截图或鼠标输入。
- code-behind 只保留纯界面行为。
- ViewModel 通过自动化模块的 interface 启动和停止任务。
- 自动化核心不依赖 `Window`、`Dispatcher`、`BitmapSource` 等 WPF 类型。

### 5.2 HimeBot.Core

保存跨模块共享的基础模型，以及外部依赖所在的 seam。

```text
HimeBot.Core/
├─ Automation/
│  ├─ RunRequest.cs
│  ├─ RunReport.cs
│  ├─ RunEvent.cs
│  └─ AutomationStatus.cs
├─ Imaging/
│  ├─ ScreenFrame.cs
│  ├─ PixelRegion.cs
│  ├─ OcrResult.cs
│  └─ MatchResult.cs
├─ Input/
│  ├─ InputAction.cs
│  └─ InputResult.cs
└─ Ports/
   ├─ IGameWindow.cs
   ├─ ITextRecognizer.cs
   ├─ ITemplateMatcher.cs
   └─ IRunRecorder.cs
```

`HimeBot.Core` 不能成为通用代码堆积区。只有确实需要被两个或更多模块共享的模型或 interface 才能放入其中。

### 5.3 HimeBot.Game.Koihime

恋姬大战的深模块，包含游戏状态、画面理解、任务状态机和语义操作。

当前实际结构：

```text
HimeBot.Game.Koihime/
├─ Domain/
│  └─ GrainStock.cs
└─ Perception/
   ├─ GrainStockParseResult.cs
   └─ GrainStockParser.cs
```

`GrainStockParser` 对 OCR 原文执行 Unicode 兼容规范化、移除空白，并严格解析“当前粮食/容量”两段以“万”为单位的非负 `int`。`万`只作为固定显示单位，不换算为实际个数；游戏允许当前粮食数值大于容量，因此解析器不施加大小关系约束。App 在现有 OCR 命令中调用这一解析器并显示原文、置信度、规范化文字与结构化数值。当前只有一个确定性解析实现，不增加 interface。

后续目标结构：

```text
HimeBot.Game.Koihime/
├─ KoihimeAutomation.cs
├─ Domain/
│  ├─ GameScene.cs
│  ├─ GameState.cs
│  ├─ GameIntent.cs
│  ├─ TargetId.cs
│  └─ RecognitionEvidence.cs
├─ Perception/
│  ├─ KoihimeObserver.cs
│  ├─ SceneRecognizer.cs
│  └─ GrainStockParser.cs
├─ Interaction/
│  ├─ IntentExecutor.cs
│  ├─ TargetLocator.cs
│  └─ PostconditionVerifier.cs
├─ Tasks/
│  ├─ IGameTask.cs
│  ├─ DailyTask.cs
│  └─ TaskDecision.cs
└─ Profiles/
   ├─ GameProfile.cs
   └─ ProfileLoader.cs
```

对 WPF 暴露的主要 interface 保持精简：

```csharp
public interface IKoihimeAutomation
{
    Task<RunReport> RunAsync(
        RunRequest request,
        IProgress<RunEvent>? progress,
        CancellationToken cancellationToken);
}
```

任务状态机产生语义意图，例如：

```csharp
public abstract record GameIntent;

public sealed record OpenMissionPanel : GameIntent;
public sealed record ClaimReward(RewardId RewardId) : GameIntent;
public sealed record SelectStage(StageId StageId) : GameIntent;
```

任务不能直接操作坐标或使用固定延时。坐标解析、点击、等待和结果验证由 `Interaction` 内部处理。

### 5.4 HimeBot.Platform.Windows

封装所有 Windows 相关实现。

```text
HimeBot.Platform.Windows/
├─ Launching/
│  └─ WindowsGameLauncher.cs
├─ Windowing/
│  ├─ GameWindowControlResult.cs
│  ├─ WindowsGameWindowController.cs
│  └─ WindowsGameWindowLocator.cs
├─ ScreenCapture/
│  ├─ WindowsGameScreenshotCapture.cs
│  ├─ ClientCropRectangleCalculator.cs    # 已保留，尚未接入生产截图路径
│  ├─ GraphicsCaptureItemFactory.cs
│  ├─ Direct3D11CaptureDevice.cs
│  ├─ Direct3D11SurfaceInterop.cs
│  └─ GameScreenshotCaptureResult.cs
├─ Input/
│  ├─ GameInputResult.cs
│  └─ SendInputAdapter.cs                 # 已验证客户区坐标鼠标移动与左键点击
├─ Dpi/
│  └─ CoordinateTransformer.cs            # 尚未实现
└─ Native/
   └─ NativeMethods.cs
```

该模块隐藏以下复杂度：

- 游戏窗口查找和状态检查。
- 客户区与屏幕坐标转换。
- Windows DPI 缩放。
- 窗口截图。
- 鼠标和键盘输入。
- 前台窗口和安全条件校验。

核心逻辑统一使用游戏客户区像素坐标，不使用 WPF 的 DIP 坐标。

当前单帧截图的内部数据流是：

```text
HWND
  -> GraphicsCaptureItem
  -> free-threaded frame pool
  -> IDirect3DSurface / ID3D11Texture2D
  -> staging texture
  -> 按 RowPitch 读取 BGRA 像素
  -> 按 DWM 边界动态裁剪客户区
  -> 按 Bitmap Stride 写入 PNG
```

WGC 选型、Direct3D/WinRT 桥接和客户区裁剪约束见 `docs/decisions/0001-use-windows-graphics-capture.md`；可复现的验证与排错步骤见 `docs/guides/windows-graphics-capture.md`。

`ClientCropRectangleCalculator` 接受托管的 `Size` 与 `Point`，只负责裁剪起点、尺寸有效性和帧范围校验，不调用 Win32、WGC 或 Direct3D。当前保留它是为了以后引入测试时继续评估；在接入之前，生产数据流仍完全由 `WindowsGameScreenshotCapture` 执行。

`SendInputAdapter` 目前暴露客户区像素坐标移动和左键点击入口，内部隐藏窗口句柄重新取得、客户区校验、`ClientToScreen`、虚拟桌面坐标归一化与 Win32 `SendInput` 调用。点击入口复用移动步骤，再提交左键按下和抬起事件。WPF 通过 MVVM 命令手动触发这两个诊断操作，二者均已人工验证；目前仍没有第二个 input adapter，因此不新增 Core interface。

`WindowsGameWindowController` 目前暴露设置或取消置顶以及调整客户区尺寸的入口，内部隐藏窗口句柄重新取得、完整窗口与客户区尺寸换算、`SetWindowPos` 参数、调整后尺寸验证与 Win32 错误读取。它与只负责查找窗口的 `WindowsGameWindowLocator` 分离，并已通过 MVVM 命令接入 WPF；置顶控制和一组近似 `16:9` 的客户区尺寸均已人工验证。当前只有一个 Windows 实现，因此不新增 Core interface。

### 5.5 HimeBot.Vision

负责与具体游戏无关的图像处理、OCR 调用和模板匹配。

当前实际结构：

```text
HimeBot.Vision/
├─ ImageProcessing/
│  ├─ ImagePreprocessor.cs
│  └─ ImageProcessingResult.cs
├─ Ocr/
│  ├─ OcrRecognitionResult.cs
│  └─ TesseractTextRecognizer.cs
└─ tessdata/
   └─ jpn.traineddata
```

`ImagePreprocessor` 当前对外提供异步灰度副本、Otsu 二值副本和矩形 ROI 副本操作，隐藏 OpenCV 原生库加载、路径与区域边界检查、图片读取、输出目录创建、资源释放和异常转换。App 通过 MVVM 命令处理最近一次成功截图，并将诊断图片保存到 `%LocalAppData%/HimeBot/processed/`。当前资源数字诊断按钮使用 `1280 × 720` 参考截图上的临时坐标；区域稳定后再移入 Koihime 专用配置。

`TesseractTextRecognizer` 当前加载输出目录中的 `tessdata/jpn.traineddata`，使用日文 LSTM 与单行分割模式，并返回 OCR 原文和平均置信度。App 直接注入这一生产实现；由于还没有第二个 OCR adapter 或测试 adapter，当前不创建 `ITextRecognizer`。`Matching/` 仍等到第一次需要模板匹配时再创建。

OCR adapter 只返回原始文字和置信度。空白清理、文字纠错、数值范围和游戏规则校验属于 `HimeBot.Game.Koihime`。

## 6. 主要 seam 与 adapters

seam 是可以替换实现而不修改调用方的位置。本项目初期保留以下 seam：

| Seam | 生产 adapter | 测试 adapter |
|---|---|---|
| `IGameWindow` | Windows 游戏窗口 | Replay/Fake 游戏窗口 |
| `ITextRecognizer` | 实际 OCR 引擎 | 固定结果或样本 OCR |
| `ITemplateMatcher` | OpenCV 模板匹配 | 固定匹配结果 |
| `IRunRecorder` | 文件运行记录 | 内存运行记录 |

只有存在明确替换需求时才增加新 seam，避免为每一个类都创建 interface。

上表是目标 seam，而不是当前接口清单。当前只有一个 Tesseract 生产实现，因此 `ITextRecognizer` 尚未创建；引入确定性测试 adapter 时再建立这一边界。

## 7. 自动化运行流程

```text
捕获游戏画面
    ↓
识别场景、OCR 文字和目标
    ↓
生成 GameState 与 RecognitionEvidence
    ↓
任务状态机生成 TaskDecision
    ↓
IntentExecutor 执行 GameIntent
    ↓
重新捕获并验证后置状态
    ↓
成功继续 / 有限重试 / 安全停止
```

禁止使用大量固定 `Task.Delay` 驱动任务。等待操作应当以“目标状态是否出现”为条件，并设置超时和最大重试次数。

## 8. WPF 初期界面

第一版主窗口包含四个页面：

1. 控制台：选择任务、开始、暂停、停止和 dry-run。
2. 实时画面：显示截图、ROI、锚点、OCR 文字和置信度。
3. 运行记录：显示步骤、耗时、重试和错误。
4. 设置：配置游戏窗口、资源路径、OCR 模型、超时和安全选项。

自动化任务运行在后台线程，通过 `IProgress<RunEvent>` 向 WPF 报告状态。实时画面需要限速，避免频繁生成 `BitmapSource` 阻塞 UI 线程。

## 9. 测试规划

```text
Tutorial/tests/HimeBot.Game.Koihime.Tests/
├─ Fixtures/
│  ├─ home.png
│  ├─ mission-reward.png
│  └─ unknown-popup.png
├─ PerceptionTests.cs
├─ DailyTaskTests.cs
├─ SafetyRuleTests.cs
└─ ReplayTests.cs
```

测试分为三类：

- 任务测试：输入 `GameState`，验证输出的 `GameIntent`。
- 识别测试：输入固定截图，验证场景、文字和置信度。
- 回放测试：用截图序列和 fake input adapter 模拟完整任务。

真实截图和输入测试属于 Windows integration test，不在默认测试集中自动运行。

## 10. 配置与运行证据

建议将用户设置和运行证据放到：

```text
%LocalAppData%/HimeBot/
├─ settings.json
└─ runs/
   └─ 2026-11-05_120000/
      ├─ run.json
      ├─ events.jsonl
      ├─ screenshots/
      └─ regions/
```

运行证据至少记录：

- 原始截图和被识别的 ROI。
- OCR 原文、归一化结果和置信度。
- 当前游戏状态。
- 计划执行的语义操作。
- 实际输入动作。
- 重试、超时和异常。

## 11. 实施阶段

### 阶段 0：技术验证

- 验证游戏窗口能否稳定截图且不会黑屏。
- 验证 `SendInput` 是否能被游戏接受。
- 已用真实资源数字 ROI 验证 Tesseract 日文单行 OCR；包已固定为支持 `.NET 7` 依赖链的 `TesseractOCR` 5.3.5，用户已确认降级后的 OCR 链路运行成功，粮食解析显示待人工验证。
- 验证 DPI、窗口尺寸和坐标换算。

### 阶段 1：最小识别闭环

- 初始化 Solution 和项目引用。
- 实现窗口查找、截图和文件记录。
- 识别首页场景与一个 OCR 区域。
- 完成 dry-run，只输出计划动作而不点击。

### 阶段 2：第一个真实任务

- 实现一个任务状态机。
- 接通截图、识别、决策、输入和验证。
- 加入取消、超时、有限重试和安全停止。

### 阶段 3：WPF 操作体验

- 完成控制台和实时画面。
- 展示识别区域、置信度和运行日志。
- 加入设置保存、暂停与恢复。

### 阶段 4：稳定性和发布

- 扩充截图回归数据集。
- 支持更多分辨率和锚点定位。
- 增加异常恢复流程。
- 处理发布、安装和原生依赖打包。

## 12. 暂不实现

在第一个任务稳定前，暂不实现：

- 通用可视化工作流编辑器。
- 多游戏插件系统。
- 多账号并行执行。
- 云端控制。
- 自动更新。
- 驱动级输入或进程注入。

遇到第二个真实需求后，再判断是否需要抽取新的模块或 seam。
