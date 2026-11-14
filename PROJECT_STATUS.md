# HimeBot 项目状态

> 最近验证日期：2026-11-14

本文档用于在新对话中恢复 HimeBot 的开发上下文。先阅读 `AGENTS.md`，再阅读本文档。在更改布局、引用关系、模块职责、自动化流程或实现阶段之前，先阅读 `docs/project-structure.md`。

## 工作区职责

- 仓库根目录中的 `HimeBot.sln` 是用户在 Visual Studio 中创建并实际动手开发的项目。引导用户在此处进行适合教学规模的改动；不要在未说明的情况下替换用户的工作。
- `Tutorial/` 是 `AGENTS.md` 要求的智能体生成参考实现。除非用户明确要求使用，否则它不是当前实际动手开发的项目。
- `learn/` 包含用户的学习笔记。除非用户明确要求编辑，否则应予以保留。

## 本次会话交接摘要

### 当前可用功能

当前根解决方案已经形成以下可人工操作的纵向链路：

1. 选择并通过 Windows Shell 打开 DMM 游戏 `.url` 快捷方式。
2. 按 `koisen` 进程名称查找游戏主窗口并读取客户区尺寸。
3. 使用 Windows Graphics Capture 保存不包含标题栏和边框的客户区 PNG。
4. 将鼠标移动到游戏客户区中心，并发送一次左键点击。
5. 设置或取消游戏窗口置顶。
6. 从单个下拉菜单选择已验证尺寸并调整游戏客户区；默认选项为 `1280 × 720`。
7. 对最近一次成功截图生成灰度图或 Otsu 二值图，并保存到 `%LocalAppData%/HimeBot/processed/`。
8. 按 `1280 × 720` 参考坐标裁剪顶部资源数字区域，并保存为 `*-resource-roi.png`。

最近一次 x64 Debug 构建成功，结果为 3 个警告、0 个错误。警告均为 OpenCvSharp 分析器使用的编译器版本高于当前 .NET 7 SDK 编译器版本；灰度图和二值图已经人工运行验证，资源数字 ROI 裁剪按钮仍待人工运行验证。上述尺寸下拉菜单已由用户人工运行验证，可以正常工作。当前没有测试项目，用户明确决定暂不引入 xUnit。

### 发生的问题与解决结论

| 事项 | 发生的情况 | 处理与结论 |
| --- | --- | --- |
| 游戏启动 | 直接启动 `koisen.exe` 会短暂出现后退出 | 改为选择 DMM 创建的 `.url`，验证其 `dmmgameplayer:` URI，再交给 Windows Shell 打开；已人工验证 |
| 窗口查找 | 游戏启动初期偶尔取得无效客户区尺寸 | 当前保留“查找游戏窗口”手动重试，尚未增加自动等待和重试策略 |
| 早期截图方案 | `CopyFromScreen` 会截入遮挡窗口；`PrintWindow` 对游戏返回黑屏 | 两种方案均不作为生产后备路径，生产截图固定使用 Windows Graphics Capture |
| WGC 客户区截图 | WGC 原始帧包含标题栏和边框 | 使用 `ClientToScreen` 与 DWM 扩展窗口边界动态计算客户区裁剪位置，已人工验证 |
| 裁剪计算抽取 | 曾新增 `ClientCropRectangleCalculator`，但用户认为立即替换原方法收益有限 | 类暂时保留但不接入生产截图；等以后学习并引入测试后再决定是否替换 |
| `SendInput` 编译 | `NativeInput[] inputs = [input];` 触发 CS8652，因为当前语言版本不支持 collection expression | 改为兼容当前项目的 `new NativeInput[] { input }`，随后构建和人工输入验证成功 |
| 窗口最小化 | 最小化后无法按原流程截图或移动、点击鼠标 | 已确认这与最小化窗口的可用表面、坐标和交互状态有关；本阶段明确跳过后台最小化自动化 |
| 完全遮挡游戏 | Chrome 完全覆盖游戏后，游戏停止提交新画面，WGC 连续取得相同帧；移开后恢复 | 这是目标游戏停止渲染，而不是 WGC 复制遮挡内容；已增加设置和取消置顶功能作为当前规避方案 |
| 客户区尺寸 | 游戏接受 `16:9` 或近似比例，但拒绝 `1280 × 800` 并恢复原尺寸 | 移除自由宽高输入，改为单个已验证尺寸下拉菜单；无论调整成功或失败都刷新实际窗口信息 |
| 窗口居中 | 希望将游戏窗口移动到屏幕中心 | 已记录为待办，计划按窗口当前所在显示器的工作区居中；尚未编写相关代码 |
| OpenCvSharp 分析器 | OpenCvSharp 4.13.0.20261107 的分析器引用编译器 4.14，当前 .NET 7 SDK 使用编译器 4.7，因此产生 `CS9057` | 不影响当前构建和运行；在选择兼容 .NET 7 的最终包版本时一并处理 |
| 整图 Otsu 二值化 | 人物高光和复杂背景被压成大块纯白或纯黑，文字细节也可能丢失 | 二值化功能正常，但不将整图结果直接用于 OCR；先裁剪文字 ROI，再比较彩色、灰度和二值输入 |

### 当前未提交工作树

本次会话结束时仍有以下未提交内容，下一窗口必须保留并在此基础上继续：

```text
 M PROJECT_STATUS.md
 M docs/project-structure.md
 M src/HimeBot.App/MainWindow.xaml
 M src/HimeBot.App/ViewModels/MainViewModel.cs
 M src/HimeBot.Vision/ImageProcessing/ImagePreprocessor.cs
```

这些改动包含 Otsu 二值预处理、通用 ROI 裁剪、对应的 MVVM 按钮链路，以及项目状态和结构说明。工作树中的其他项目文件保持原状。

### 下一窗口恢复顺序

1. 阅读根目录 `AGENTS.md`、本文件和 `docs/project-structure.md`。
2. 执行 `git status --short`，确认上述未提交文件仍然存在。
3. 不要重新实现已经完成人工验证的尺寸下拉菜单。
4. 将游戏客户区设置为 `1280 × 720`，保存新截图并人工运行“裁剪资源数字”，确认生成的 `*-resource-roi.png` 主要包含顶部 `236万 / 454万`。
5. 根据实际裁剪结果微调区域坐标；区域稳定后再选择 OCR 引擎，并比较该区域的彩色、灰度和二值输入。xUnit 继续暂缓。

## 当前里程碑

第一个启动功能已经从 WPF 经由 MVVM 连接到 Windows 启动器，并且已经确定 DMM 专用的启动目标。当前实现已经通过成功的 x64 构建验证。

人工观察已经证明原先直接启动 `koisen.exe` 的方案不可行：从 HimeBot 启动或直接从所在文件夹启动时，它只会短暂打开，随后退出。据用户反馈，在 HimeBot 之外双击 DMM 创建的桌面快捷方式，可以打开 DMM GAMES PLAYER 并启动游戏。该快捷方式是 Internet 快捷方式（`.url`），其 URI 使用 `dmmgameplayer:` 协议，因此当前 UI 改为选择并验证该快捷方式，而不再选择游戏可执行文件。

端到端人工验证已经完成：在 HimeBot 中选择 DMM 创建的 `恋戦.url` 并发送启动请求后，DMM GAMES PLAYER 能够正常启动游戏，`koisen.exe` 可以保持运行。因此当前启动链路已经通过人工验证。

Windows 启动器现在使用 Shell 激活语义打开经过验证的 `.url`。启动器返回成功仍然只表示 Windows Shell 接受了请求。游戏真正启动成功是本次人工观察的结果，目前程序还没有自动检测游戏进程或窗口。

游戏窗口定位功能也已经完成第一个纵向验证：WPF 通过 `IGameWindowLocator` 调用 `WindowsGameWindowLocator`，按 `koisen` 进程名称筛选具有主窗口的候选项，并通过 Win32 `GetClientRect` 读取客户区尺寸。人工验证能够返回游戏窗口标题以及 `746 × 420` 的客户区尺寸。窗口初始化期间曾短暂返回无效尺寸，再次查询后结果正常；当前阶段保留手动重试，尚未加入自动等待或重试策略。

单帧截图链路已经从 WPF 经由异步 MVVM 命令连接到 `WindowsGameScreenshotCapture`，并能将 PNG 保存到 `%LocalAppData%/HimeBot/captures/`。`CopyFromScreen` 能够获得内容正确的客户区截图，但它读取屏幕最终合成像素，因此其他窗口遮挡游戏时，遮挡内容也会进入截图。随后验证的 Win32 `PrintWindow` 不会带入遮挡窗口，但对该游戏只返回黑屏，因此该实验已经回滚。

Windows Graphics Capture 单帧客户区截图链路已经完成构建和人工运行验证：程序能够从窗口句柄创建 `GraphicsCaptureItem`，建立 Direct3D 11 设备、free-threaded frame pool 与捕获会话，取得第一帧并将 WinRT Surface 转换为原生 `ID3D11Texture2D`。帧通过 staging texture 映射到 CPU，按照 GPU `RowPitch` 逐行读取；随后使用 `ClientToScreen` 与 DWM 可见窗口边界动态计算客户区位置，并按 Bitmap `Stride` 写入 PNG。完整窗口阶段已确认其他窗口的遮挡不会进入 WGC 图片；最新人工运行确认客户区裁剪可以正常保存图片，并移除游戏标题栏和边框。

新的人工观察表明：普通窗口只遮挡游戏的一部分时，游戏仍然继续更新画面；当 Chrome 窗口逐步放大并完全覆盖游戏后，游戏停止更新渲染画面，WGC 仍能保存图像，但多次截图内容相同；移开 Chrome 后游戏继续渲染。这说明 WGC 可以读取目标窗口保留的表面，但不能强制目标游戏提交新帧。

已新增内部纯计算类 `ClientCropRectangleCalculator`，用于根据完整帧尺寸、客户区尺寸、客户区屏幕原点和 DWM 可见窗口原点计算裁剪矩形并校验越界。该类目前有意保留但尚未接入 `WindowsGameScreenshotCapture`；生产截图仍使用原有的 `TryGetClientCropRectangle` 实现，因此本次新增没有改变截图行为。等后续学习并引入测试后，再决定如何接入和调整该类。

Windows 鼠标输入技术验证已经完成：`SendInputAdapter` 能够根据已定位的游戏窗口，将客户区像素坐标经 `ClientToScreen` 转为屏幕坐标，再按虚拟桌面归一化后通过 Win32 `SendInput` 提交绝对鼠标移动。`ClickClientPoint` 在移动成功后继续提交左键按下和左键抬起事件，WPF 通过两个 MVVM 命令分别触发移动与点击。人工运行已确认光标可以正确移动到游戏客户区中心，并且游戏会对测试点击产生实际响应。

窗口层级控制已经完成构建和人工运行验证：`WindowsGameWindowController` 根据已定位窗口的进程 ID 重新取得当前主窗口句柄，并通过 `SetWindowPos` 在 `HWND_TOPMOST` 与 `HWND_NOTOPMOST` 之间切换，同时使用 `SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE` 保持位置、尺寸和焦点不变。WPF 已经通过两个 MVVM 命令提供设置和取消置顶按钮；人工运行确认普通窗口无法覆盖置顶后的游戏，取消置顶后游戏恢复普通 Z-order。

客户区尺寸调整已经完成技术验证：`WindowsGameWindowController.SetClientSize` 根据当前完整窗口与客户区的尺寸差换算目标外框尺寸，通过 `SetWindowPos` 调整后再用 `GetClientRect` 验证。人工运行确认 `1024 × 576`、`1280 × 720`、`1360 × 765`、`1366 × 768`、`1440 × 810`、`1600 × 900`、`1920 × 1080` 和 `2560 × 1440` 可用；`1280 × 800` 会被游戏拒绝并恢复原始大小。WPF 已改为只提供这些已验证尺寸的单个下拉菜单，并在调整成功或失败后刷新实际窗口信息；下拉菜单已完成人工运行验证。

WGC、Direct3D/WinRT 互操作与裁剪的长期决定记录在 `docs/decisions/0001-use-windows-graphics-capture.md`，实现流程和本次错误排查记录在 `docs/guides/windows-graphics-capture.md`。

## 最近学习阶段已完成

以下实现已经存在于当前代码中，并包含在成功的 x64 构建范围内：

1. 在 `HimeBot.Core` 中添加了 `IGameLauncher` 和 `GameLaunchResult`。
2. 添加了 `WindowsGameLauncher`，包含路径检查、工作目录选择、Shell 执行和有用的失败结果。
3. 在 `HimeBot.App` 中添加并固定了 `CommunityToolkit.Mvvm` 8.4.2 包版本。
4. 添加了 `MainViewModel`、可观察的路径与状态属性、文件选择与启动命令，以及命令启用状态控制。
5. 在 `App.xaml.cs` 中以组合根（composition root）的形式连接依赖项。
6. 添加了最小化的 WPF 路径、状态、选择按钮和启动按钮绑定。
7. 使用 `IGameLaunchTargetPicker` 和 `OpenFileDialogGameLaunchTargetPicker` 替换了可执行文件选择器。
8. 将对话框限制为选择 `.url` 文件，并增加了对 `dmmgameplayer:` URI 协议的内容验证。
9. 将启动器参数重命名为 `launchTargetPath`，并修正 Shell 激活语义：只要没有抛出异常，就表示请求已被接受，即使没有返回新的 `Process` 对象。
10. 在 `HimeBot.Core` 中添加了 `IGameWindowLocator`、`GameWindowInfo` 和 `GameWindowLookupResult`。
11. 在 `HimeBot.Platform.Windows` 中添加了 `WindowsGameWindowLocator`，能够筛选进程主窗口并处理未启动、窗口未就绪和多个候选项。
12. 添加了内部 Win32 `GetClientRect` 声明，用游戏客户区像素表示窗口尺寸，不向 Core 暴露原生窗口句柄。
13. 在 `MainViewModel`、组合根和 WPF 主窗口中接入“查找游戏窗口”命令，并显示进程 ID、窗口标题和客户区尺寸。
14. 添加了单帧截图结果、Windows 截图实现、异步截图命令和“保存客户区截图”按钮，并将诊断 PNG 保存到本地应用数据目录。
15. 人工确认 `CopyFromScreen` 会捕获遮挡内容；`PrintWindow` 对游戏返回黑屏，相关实验代码已经回滚。
16. 为 Windows Graphics Capture 设置了 Windows 10 19041 SDK 编译基线，固定了 `Vortice.Direct3D11` 3.5.0，并验证 `GraphicsCaptureSession.IsSupported()` 可用。
17. 完成 `GraphicsCaptureItem`、Direct3D 11 设备、frame pool、第一帧等待与 WinRT/Direct3D 纹理互操作，并人工验证能够取得正确格式的 WGC 帧。
18. 完成 staging texture、GPU 到 CPU 映射、按 `RowPitch` 复制和 PNG 编码；人工验证完整窗口截图不会包含其他窗口的遮挡内容。
19. 接入基于 DWM 可见窗口边界的客户区动态裁剪；已通过 x64 构建和人工运行验证，最终图片不再包含游戏标题栏与边框。
20. 新增 `GameInputResult`、`SendInputAdapter` 与所需的 Win32 输入结构，通过组合根、`MainViewModel` 命令和 WPF 按钮接入“测试移动鼠标”；已人工确认光标能正确移动到游戏客户区中心。
21. 在 `SendInputAdapter` 中组合客户区坐标移动、左键按下和左键抬起，通过 `MainViewModel` 与“测试鼠标点击”按钮接入；已通过 x64 构建，并人工确认游戏会响应点击。
22. 新增 `GameWindowControlResult`、`WindowsGameWindowController` 与原生 `SetWindowPos` 声明，并通过组合根、`MainViewModel` 命令和 WPF 按钮接入设置与取消置顶；已通过 x64 构建和人工运行验证。
23. 为 `WindowsGameWindowController` 增加客户区尺寸调整与调整后验证，WPF 第一版使用宽高文本输入；已通过 x64 构建，并人工确认一组可用的近似 `16:9` 尺寸以及 `1280 × 800` 被游戏拒绝的行为。
24. 用单个尺寸下拉菜单替换宽高文本输入，只提供已验证尺寸，并在调整成功或失败后刷新实际窗口信息；已通过 x64 构建和人工运行验证。

本阶段已经完成的人工观察：

- 直接从所在文件夹启动 `koisen.exe`，或者通过早期 HimeBot 可执行文件启动流程启动它，都会使其短暂打开后退出。
- 在 HimeBot 之外双击 DMM 创建的桌面快捷方式，可以打开 DMM GAMES PLAYER 并启动游戏。
- 通过当前 HimeBot `.url` 流程可以启动 DMM，并使 `koisen.exe` 正常启动和保持运行。
- 通过“测试移动鼠标”命令，光标可以从 HimeBot 按钮移动到游戏客户区中心。
- 通过“测试鼠标点击”命令，游戏会对客户区中心的左键点击产生实际响应。
- Chrome 逐步扩大至完全遮挡游戏时，游戏渲染画面停止更新；WGC 保存的图片仍然有效，但内容重复；移开 Chrome 后画面恢复更新。
- 设置游戏置顶后，普通窗口无法覆盖游戏；取消置顶后，游戏恢复普通窗口层级。
- 游戏接受已验证的 `16:9` 或近似 `16:9` 客户区尺寸；明显为 `16:10` 的 `1280 × 800` 会被游戏恢复为调整前的原始大小。

## 当前仓库结构

```text
HimeBot/
├─ AGENTS.md
├─ PROJECT_STATUS.md
├─ Directory.Build.props
├─ HimeBot.sln
├─ docs/
│  ├─ project-structure.md
│  ├─ decisions/
│  │  └─ 0001-use-windows-graphics-capture.md
│  └─ guides/
│     └─ windows-graphics-capture.md
├─ learn/
│  └─ 如何安全移动项目.md
├─ src/
│  ├─ HimeBot.App/
│  ├─ HimeBot.Core/
│  └─ HimeBot.Platform.Windows/
└─ Tutorial/                  # 智能体生成的参考项目
```

目前相关的实现文件包括：

```text
src/HimeBot.App/
├─ FilePicking/
│  ├─ IGameLaunchTargetPicker.cs
│  └─ OpenFileDialogGameLaunchTargetPicker.cs
├─ ViewModels/
│  ├─ ClientSizeOption.cs
│  └─ MainViewModel.cs
├─ HimeBot.App.csproj
├─ App.xaml.cs
└─ MainWindow.xaml

src/HimeBot.Core/
├─ Launching/GameLaunchResult.cs
├─ Ports/IGameLauncher.cs
├─ Ports/IGameWindowLocator.cs
└─ Windowing/
   ├─ GameWindowInfo.cs
   └─ GameWindowLookupResult.cs

src/HimeBot.Platform.Windows/
├─ Input/
│  ├─ GameInputResult.cs
│  └─ SendInputAdapter.cs
├─ Launching/WindowsGameLauncher.cs
├─ Native/NativeMethods.cs
├─ ScreenCapture/ClientCropRectangleCalculator.cs
├─ ScreenCapture/Direct3D11CaptureDevice.cs
├─ ScreenCapture/Direct3D11SurfaceInterop.cs
├─ ScreenCapture/GameScreenshotCaptureResult.cs
├─ ScreenCapture/GraphicsCaptureItemFactory.cs
├─ ScreenCapture/WindowsGameScreenshotCapture.cs
└─ Windowing/
   ├─ GameWindowControlResult.cs
   ├─ WindowsGameWindowController.cs
   └─ WindowsGameWindowLocator.cs
```

## 已验证的项目引用

```text
HimeBot.App
├─> HimeBot.Core
└─> HimeBot.Platform.Windows
                      └─> HimeBot.Core
```

- `HimeBot.Core` 不引用任何项目。
- `HimeBot.Platform.Windows` 只引用 `HimeBot.Core`。
- `HimeBot.App` 引用这两个下层模块，并继续作为后续的组合根（composition root）。

## 共享构建基线

根目录中的 `Directory.Build.props` 当前提供以下配置：

```xml
<TargetFramework>net7.0-windows10.0.19041.0</TargetFramework>
<Platforms>x64</Platforms>
<PlatformTarget>x64</PlatformTarget>
<ImplicitUsings>enable</ImplicitUsings>
<Nullable>enable</Nullable>
```

解决方案中仍同时存在 `Any CPU` 和 `x64` 配置名称，但项目基线和计划使用的活动平台是 x64。从命令行验证时，应显式执行 x64 构建。

两个类库项目文件中仍保留无害的空 `<PropertyGroup>` 元素。可以在后续清理时将其删除，但它们不会阻碍开发。

## 最近验证

在仓库根目录运行：

```powershell
dotnet build HimeBot.sln --configuration Debug --property:Platform=x64
```

最近一次结果：

```text
HimeBot.Core              -> bin/x64/Debug/net7.0-windows10.0.19041.0/HimeBot.Core.dll
HimeBot.Platform.Windows  -> bin/x64/Debug/net7.0-windows10.0.19041.0/HimeBot.Platform.Windows.dll
HimeBot.App               -> bin/x64/Debug/net7.0-windows10.0.19041.0/HimeBot.App.dll

构建成功：0 个警告，0 个错误
```

目前没有测试项目或测试用例，因此未运行自动化测试，也不声明任何测试结果。`ClientCropRectangleCalculator` 已随根解决方案完成 x64 Debug 构建验证，但尚未接入生产截图路径，也没有对应的自动化测试。

窗口定位已经完成人工验证：游戏运行时能够识别主窗口并返回窗口标题与 `746 × 420` 客户区尺寸。针对同一运行窗口的诊断调用连续执行 10 次，结果保持一致。

截图实验已经人工验证：`CopyFromScreen` 在无遮挡时能够保存正确画面，但会带入遮挡内容；`PrintWindow` 对游戏返回黑屏。Windows Graphics Capture 能够保存内容正确的窗口 PNG，并且其他窗口遮挡游戏时不会把遮挡内容写入图片。客户区动态裁剪也已完成人工运行验证，最终 PNG 可以正常保存且不再包含标题栏和边框。

`SendInput` 鼠标移动与左键点击均已完成构建和人工运行验证：两个按钮命令能将游戏客户区中心坐标交给 `SendInputAdapter`，光标的实际落点正确，游戏也会对点击产生响应。

`SetWindowPos` 窗口层级控制已完成构建和人工运行验证：设置置顶后普通窗口无法覆盖游戏，取消置顶后恢复普通 Z-order。该结果不改变最小化、其他置顶窗口、锁屏或安全桌面的既有限制。

客户区尺寸下拉菜单已完成构建和人工运行验证。默认 `1280 × 720` 与其他已验证选项可以正常调整游戏窗口；自由宽高输入已经移除，调整后会刷新实际窗口信息。

## 未解决的决定

- 后续的成功检测机制尚未决定。可能的信号包括检测游戏进程或游戏窗口；当前启动器有意只报告 Shell 已接受请求。
- 尚未决定 HimeBot 应该继续要求用户选择 DMM 快捷方式、记住上一次选择，还是自动发现该快捷方式。

## 待办事项

### 游戏窗口居中

状态：尚未实现。

计划继续阶段 0 的窗口控制：

1. 读取游戏窗口当前所在显示器的工作区，不把任务栏区域计算在内。
2. 将完整游戏窗口移动到该工作区中心，同时保持窗口尺寸、Z-order 和焦点不变。
3. 通过 MVVM 命令和 WPF 按钮手动触发窗口居中。
4. 构建后人工验证窗口位置；如果有多台显示器，再验证窗口会在当前显示器居中。

计划使用 `MonitorFromWindow` 和 `GetMonitorInfo` 获取当前显示器工作区，再使用现有的 `SetWindowPos` 移动窗口。当前尚未添加相关 Win32 声明、控制器方法、MVVM 命令或 WPF 按钮。

当前仍只承诺一次性客户区截图、手动输入诊断、窗口层级控制和客户区尺寸调整，不实现连续捕获、实时预览、最小化窗口捕获、OCR、自动点击流程或任务状态机。
