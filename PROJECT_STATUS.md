# HimeBot 项目状态

> 最近验证日期：2026-11-08

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

本阶段已经完成的人工观察：

- 直接从所在文件夹启动 `koisen.exe`，或者通过早期 HimeBot 可执行文件启动流程启动它，都会使其短暂打开后退出。
- 在 HimeBot 之外双击 DMM 创建的桌面快捷方式，可以打开 DMM GAMES PLAYER 并启动游戏。
- 通过当前 HimeBot `.url` 流程可以启动 DMM，并使 `koisen.exe` 正常启动和保持运行。

## 当前仓库结构

```text
HimeBot/
├─ AGENTS.md
├─ PROJECT_STATUS.md
├─ Directory.Build.props
├─ HimeBot.sln
├─ docs/
│  └─ project-structure.md
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
├─ Launching/WindowsGameLauncher.cs
├─ Native/NativeMethods.cs
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
<TargetFramework>net7.0-windows</TargetFramework>
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
HimeBot.Core              -> bin/x64/Debug/net7.0-windows/HimeBot.Core.dll
HimeBot.Platform.Windows  -> bin/x64/Debug/net7.0-windows/HimeBot.Platform.Windows.dll
HimeBot.App               -> bin/x64/Debug/net7.0-windows/HimeBot.App.dll

构建成功：0 个警告，0 个错误
```

目前没有测试项目或测试用例，因此未运行自动化测试，也不声明任何测试结果。

窗口定位已经完成人工验证：游戏运行时能够识别主窗口并返回窗口标题与 `746 × 420` 客户区尺寸。针对同一运行窗口的诊断调用连续执行 10 次，结果保持一致。

## 未解决的决定

- 后续的成功检测机制尚未决定。可能的信号包括检测游戏进程或游戏窗口；当前启动器有意只报告 Shell 已接受请求。
- 尚未决定 HimeBot 应该继续要求用户选择 DMM 快捷方式、记住上一次选择，还是自动发现该快捷方式。

## 下一步目标

获取一张游戏客户区截图：

1. 复用已经验证的游戏窗口定位结果。
2. 选择并验证适合游戏窗口的单帧捕获方式。
3. 将一张客户区画面保存为 PNG 诊断文件。
4. 人工确认截图不是黑屏、内容与游戏一致，并且图片尺寸与客户区尺寸一致。

这一步只验证一次性截图，不实现连续捕获、实时预览、OCR、鼠标输入或任务状态机。
