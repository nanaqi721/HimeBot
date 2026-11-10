# HimeBot 智能体指南

## 项目背景

HimeBot 是一个使用 C# 和 WPF 开发、由 OCR 驱动的游戏自动化应用程序。首个支持的游戏是 Koihime Taisen。用户正在第一次构建 C# 项目，希望在实现每个小步骤之前先讨论背后的原理。

使用中文进行解释，代码标识符使用英文。首次出现用户不熟悉的 C# 或 WPF 概念时，结合当前改动进行简短说明。

## 教学工作区

将 `Tutorial/` 作为参考实现工作区。智能体生成的所有 .NET 解决方案、项目、源文件、测试、资源和项目级构建配置都必须放在 `Tutorial/` 下。仓库级说明和文档（包括 `AGENTS.md` 与 `docs/`）保留在仓库根目录。

参考实现用于辅助学习，而不是代替用户动手。在生成每个适合教学规模的改动之前，先解释用户手动操作时会创建的命令或文件、该操作代表的设计决定，以及用户如何复现或验证。只有在用户明确要求后，才能在 `Tutorial/` 之外创建实现代码。

## 固定技术基线

- 目标框架保持为 `.NET 7` 和 `net7.0-windows`。
- 桌面 UI 框架保持为 WPF，默认平台保持为 x64。
- 选择支持 `.NET 7` 的 NuGet 包版本，并固定其版本号。
- 只有在用户明确要求后才能更改框架版本。
- UI 状态和命令使用 MVVM；游戏自动化逻辑不得放入 code-behind。

固定使用 `.NET 7` 是有意设置的项目约束。处理无关工作时，不要反复建议升级框架。

## 协作流程

按照适合教学的规模逐步工作：

1. 检查当前仓库和相关文档。
2. 在进行较大的代码改动之前，说明当前目标、设计选择和涉及的文件。
3. 只实现用户要求的最小纵向步骤。
4. 构建受影响的项目；存在相关测试时，运行这些测试。
5. 说明改动内容、验证方法和下一个合理步骤。

当用户要求讨论或规划时，提供可选方案和取舍，不修改代码。当用户直接要求实现时，执行已经商定的小步骤，不要提前搭建后续阶段。

结合具体代码进行教学。对于解决方案（solution）、项目（project）、命名空间（namespace）、接口（interface）、依赖注入（dependency injection）、`async/await`、绑定（binding）、命令（command）或适配器（adapter）等新概念，需要说明：

- 它是什么；
- 当前步骤为什么需要它；
- 它位于本仓库的什么位置；
- 用户如何验证它正常工作。

优先采用能够编译通过的小补丁。除非请求的结果确实需要，否则不要在同一步骤中引入多个框架或架构模式。

## 架构

计划采用模块化单体架构，在 `Tutorial/src/` 下包含以下项目：

- `HimeBot.App`：WPF 视图、ViewModel、资源和组合根（composition root）。
- `HimeBot.Core`：共享模型以及外部边界上的小型接口。
- `HimeBot.Game.Koihime`：Koihime 游戏状态、感知、任务状态机和语义意图。
- `HimeBot.Platform.Windows`：窗口发现、捕获、DPI 转换和输入适配器。
- `HimeBot.Vision`：OCR、图像预处理和模板匹配适配器。

保持单向引用：

```text
HimeBot.App
├─> HimeBot.Game.Koihime
├─> HimeBot.Platform.Windows
└─> HimeBot.Vision

HimeBot.Game.Koihime ──────> HimeBot.Core
HimeBot.Platform.Windows ──> HimeBot.Core
HimeBot.Vision ────────────> HimeBot.Core
```

将其视为目标结构，而不是立即创建所有项目的要求。只有当前实现步骤需要相应职责时，才添加项目。

保持模块具有足够深度：对外暴露小型接口，将 OCR、Win32、重试、坐标和验证细节隐藏在实现中。只有当边界上确实需要至少两个适配器时才添加接口，通常是一个生产适配器和一个确定性的测试适配器。

游戏任务生成 `OpenMissionPanel` 或 `ClaimReward` 等语义意图。平台适配器将这些意图转换为坐标和输入操作。原始坐标、固定延迟和 OCR 引擎类型不得进入任务逻辑。

## 文档索引

在新对话开始时或继续实现之前，先阅读 [`PROJECT_STATUS.md`](PROJECT_STATUS.md) 以恢复工作上下文。该文档记录最近验证的里程碑、未解决的决定和下一步目标；每完成一个学习里程碑后都要更新它。

在更改项目布局、引用关系、模块职责、自动化流程或实现阶段之前，先阅读 [`docs/project-structure.md`](docs/project-structure.md)。它是计划中仓库结构的单一事实来源。

未来的文档按照以下方式组织；只有需要创建其中第一份文档时，才创建对应目录：

```text
docs/
├─ project-structure.md       # 架构、目录规划和实现阶段
├─ decisions/
│  └─ 0001-use-windows-graphics-capture.md
└─ guides/
   └─ windows-graphics-capture.md
```

项目布局或引用方向发生变化时，在同一次改动中更新 `docs/project-structure.md`。当某项决定的原因需要跨越当前对话长期保留时，将其记录在 `docs/decisions/`。可重复执行的人工操作流程放入 `docs/guides/`。

修改 Windows 截图、Direct3D/WinRT 互操作、GPU 像素读取或客户区裁剪前，先阅读 [`docs/decisions/0001-use-windows-graphics-capture.md`](docs/decisions/0001-use-windows-graphics-capture.md) 和 [`docs/guides/windows-graphics-capture.md`](docs/guides/windows-graphics-capture.md)。

## C# 与 WPF 约定

- 启用可空引用类型（nullable reference types）和隐式 using（implicit usings）。
- 使用异步 I/O，并在长时间运行的自动化流程中传递 `CancellationToken`。
- 保持 WPF UI 线程响应；捕获、OCR 和自动化工作应在 UI 线程之外运行。
- 通过适合绑定的进度或可观察状态将更新传递到 UI，不要让自动化模块引用 WPF 类型。
- 以确定性的方式释放捕获帧、原生句柄和其他拥有所有权的资源。
- 使用游戏客户端像素存储坐标，并在 Windows 模块内部完成 WPF DIP 或屏幕坐标转换。
- 记录 OCR 文本、规范化值、置信度、区域、操作、重试和失败截图，以便诊断。

## Windows 截图约束

- 生产截图使用 `WindowsGameScreenshotCapture` 的 Windows Graphics Capture 路径；`CopyFromScreen` 和 `PrintWindow` 只保留为已经否决的实验结论，不作为自动后备路径。
- WGC 帧是包含非客户区的可见窗口帧。客户区裁剪必须由 `ClientToScreen` 和 DWM 扩展窗口边界动态计算，不得硬编码标题栏或边框像素。
- 在当前 `.NET 7`、C#/WinRT 与 `Vortice.Direct3D11` 3.5.0 组合下，使用项目现有的原生 `CreateDirect3D11DeviceFromDXGIDevice` 加 `WinRT.MarshalInterface<T>.FromAbi` 桥接；不要改回 Vortice 泛型转换辅助方法。
- GPU 回读必须先复制到 staging texture，分别遵守源 `RowPitch` 与目标 Bitmap `Stride`，并确保 `Map` 后在 `finally` 中 `Unmap`。

## 验证

当前用户实际开发项目位于仓库根目录。用户明确要求修改该项目后，在仓库根目录运行：

```powershell
dotnet build HimeBot.sln --configuration Debug --property:Platform=x64
dotnet test HimeBot.sln --configuration Debug --property:Platform=x64
```

智能体生成的参考实现仍使用：

```powershell
dotnet build Tutorial/HimeBot.sln
dotnet test Tutorial/HimeBot.sln
```

只有在受影响的项目构建成功、相关测试通过、架构文档反映了结构变化，并且用户获得了适合初学者的验证步骤后，改动才算完成。如果测试项目尚未创建或某项检查无法运行，应明确说明，不能虚构成功结果。
