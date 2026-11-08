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

Windows 启动器现在使用 Shell 激活语义打开经过验证的 `.url`。如果 `Process.Start` 没有抛出异常，只表示 Windows Shell 接受了请求；这不能证明 DMM 或游戏已经完成启动。通过 HimeBot 新 `.url` 流程进行的端到端人工验证仍处于待验证状态，包括确认 DMM 收到请求以及 `koisen.exe` 能够保持运行。

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

本阶段已经完成的人工观察：

- 直接从所在文件夹启动 `koisen.exe`，或者通过早期 HimeBot 可执行文件启动流程启动它，都会使其短暂打开后退出。
- 在 HimeBot 之外双击 DMM 创建的桌面快捷方式，可以打开 DMM GAMES PLAYER 并启动游戏。
- 通过当前 HimeBot `.url` 流程启动 DMM 并使 `koisen.exe` 保持运行，尚未完成人工验证，仍处于待验证状态。

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
└─ Ports/IGameLauncher.cs

src/HimeBot.Platform.Windows/
└─ Launching/WindowsGameLauncher.cs
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

## 未解决的决定

- 通过 HimeBot 执行端到端启动仍待人工验证：需要确认选中的 `.url` 已交给 DMM GAMES PLAYER，并确认 DMM 能够启动 `koisen.exe` 且使其保持运行。
- 后续的成功检测机制尚未决定。可能的信号包括检测游戏进程或游戏窗口；当前启动器有意只报告 Shell 已接受请求。
- 尚未决定 HimeBot 应该继续要求用户选择 DMM 快捷方式、记住上一次选择，还是自动发现该快捷方式。

## 下一步目标

人工验证 DMM 启动路径：

1. 运行 HimeBot，并选择 DMM 创建的 `恋戦.url` 快捷方式。
2. 单击启动按钮，验证 DMM GAMES PLAYER 是否打开或获得焦点。
3. 验证 DMM 是否启动 `koisen.exe`，以及游戏是否保持运行。
4. 记录观察到的状态消息和任何失败行为。

在完成这些步骤之前，DMM 是否收到 HimeBot 请求以及 `koisen.exe` 的最终启动结果都必须保持“待验证”状态。当前的成功结果只表示“Windows Shell 已接受请求”。

在这个仅涉及启动功能的步骤中，不要创建 `HimeBot.Game.Koihime`、`HimeBot.Vision`、OCR、捕获或输入代码。
