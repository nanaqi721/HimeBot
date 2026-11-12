# HimeBot 项目状态

> 最近验证日期：2026-11-12

本文档用于在新对话中恢复 HimeBot 的开发上下文。先阅读 `AGENTS.md`，再阅读本文档。在更改布局、引用关系、模块职责、自动化流程或实现阶段之前，先阅读 `docs/project-structure.md`。

## 工作区职责

- 仓库根目录中的 `HimeBot.sln` 是用户在 Visual Studio 中创建并实际动手开发的项目。引导用户在此处进行适合教学规模的改动；不要在未说明的情况下替换用户的工作。
- `Tutorial/` 是 `AGENTS.md` 要求的智能体生成参考实现。除非用户明确要求使用，否则它不是当前实际动手开发的项目。
- `learn/` 包含用户的学习笔记。除非用户明确要求编辑，否则应予以保留。

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

Windows 鼠标输入技术验证已完成第一步：`SendInputAdapter` 能够根据已定位的游戏窗口，将客户区像素坐标经 `ClientToScreen` 转为屏幕坐标，再按虚拟桌面归一化后通过 Win32 `SendInput` 提交绝对鼠标移动。WPF 已经由 MVVM 命令调用该 adapter。人工运行已确认光标可以正确移动到游戏客户区中心。`ClickClientPoint` 已经组合鼠标移动、左键按下和左键抬起事件并通过构建，但尚未接入 WPF 命令，也尚未进行游戏内人工验证。

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

本阶段已经完成的人工观察：

- 直接从所在文件夹启动 `koisen.exe`，或者通过早期 HimeBot 可执行文件启动流程启动它，都会使其短暂打开后退出。
- 在 HimeBot 之外双击 DMM 创建的桌面快捷方式，可以打开 DMM GAMES PLAYER 并启动游戏。
- 通过当前 HimeBot `.url` 流程可以启动 DMM，并使 `koisen.exe` 正常启动和保持运行。
- 通过“测试移动鼠标”命令，光标可以从 HimeBot 按钮移动到游戏客户区中心。
- Chrome 逐步扩大至完全遮挡游戏时，游戏渲染画面停止更新；WGC 保存的图片仍然有效，但内容重复；移开 Chrome 后画面恢复更新。

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
├─ ViewModels/MainViewModel.cs
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
└─ Windowing/WindowsGameWindowLocator.cs
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

`SendInput` 鼠标移动已完成构建和人工运行验证：按钮命令能将游戏客户区中心坐标交给 `SendInputAdapter`，光标的实际落点正确。左键按下和抬起事件的底层方法已通过构建，但还没有界面入口，也不能据此断言游戏已经接受点击。

## 未解决的决定

- 后续的成功检测机制尚未决定。可能的信号包括检测游戏进程或游戏窗口；当前启动器有意只报告 Shell 已接受请求。
- 尚未决定 HimeBot 应该继续要求用户选择 DMM 快捷方式、记住上一次选择，还是自动发现该快捷方式。
- 待验证的遮挡处理方案是：自动化期间用 `SetWindowPos(HWND_TOPMOST)` 使游戏保持在普通非置顶窗口上方，并同时使用 `SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE` 避免改变尺寸、位置和焦点；停止自动化时用 `HWND_NOTOPMOST` 恢复。该方案尚未实现或人工验证，且不保证胜过其他置顶窗口、最小化状态或安全桌面。

## 下一步目标

继续阶段 0 的 `SendInput` 技术验证：

1. 在 `MainViewModel` 中增加测试点击命令，由它调用现有的 `ClickClientPoint`。
2. 在 WPF 中增加手动触发按钮；构建成功后，由用户在游戏中选择一个无破坏性、可观察的点击目标。
3. 以游戏界面是否产生可见反应作为“游戏接受输入”的人工判定；`SendInput` 返回的插入数量只代表 Windows 已接受事件。
4. 目前仍只有一个真实输入 adapter，不新增 Core interface，也不引入 xUnit。

当前仍只承诺一次性客户区截图和手动触发的单次鼠标诊断，不实现连续捕获、实时预览、最小化窗口捕获、OCR、自动点击流程或任务状态机。
