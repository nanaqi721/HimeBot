# HimeBot 项目状态

> 最近验证日期：2026-11-15

本文档是新窗口恢复开发上下文的入口。先阅读根目录 `AGENTS.md`，再阅读本文档；更改项目布局、引用方向、模块职责或实现阶段前，还要阅读 `docs/project-structure.md`。

## 当前里程碑

根解决方案已经完成第一条从截图到粮食状态的最小纵向链路：

```text
Windows Graphics Capture 客户区截图
    → 按固定参考坐标裁剪资源数字 ROI
    → Tesseract 日文单行识别
    → Koihime 模块规范化并解析粮食文字
    → WPF 状态栏显示 OCR 证据与粮食数值
```

真实运行时，彩色 ROI 已正确识别顶部粮食文本 `236万/454万`（Tesseract 原文可能在 `/` 后保留一个空格）。用户已确认降级到 `TesseractOCR` 5.3.5 后 OCR 仍能运行成功。粮食解析器和 WPF 展示已完成构建，等待使用真实 ROI 人工确认最终显示。

首页场景识别的诊断链路也已完成接入：App 使用最近一次 `1280 × 720` 客户区截图调用 Vision 的 OpenCV 模板匹配器，再把最佳分数与位置交给 Koihime 的 `HomeSceneRecognizer`，生成 `Home/Unknown` 并显示判断证据。用户已确认接线后的项目能够编译并启动运行；该链路只报告场景，不会触发鼠标输入。

原有的总场景观察器已经按场景拆分：`KoihimeFrameCapture` 统一执行 WGC 截图和 `1280 × 720` 校验，`HomeSceneObserver`、`MissionPanelObserver` 与 `RewardReceiptDialogObserver` 分别观察首页、任务面板和奖励领取弹窗。WPF 提供三个独立诊断按钮；任务编排可以在不同步骤只调用当前需要的观察者，避免单个观察器随场景数量持续膨胀。

首页任务入口的两种状态样本仍保留在 `assets/koihime/templates/`，但人工审核发现其绝对分数会随首页背景大幅波动，因此运行时不再判断有无奖励。当前动作改为：每次点击前重新观察场景，只有确认 `Home` 后，才在 `1280 × 720` 客户区固定坐标 `(1235, 565)` 点击任务入口；点击后每隔 `200 ms` 重新观察，最长等待 `3 s`，只有确认 `MissionPanel` 后才报告成功。

## 已完成功能

### 已有基础链路

- 选择并通过 Windows Shell 打开 DMM 游戏 `.url` 快捷方式。
- 按 `koisen` 进程名查找游戏主窗口并读取客户区尺寸。
- 使用 Windows Graphics Capture 保存不含标题栏和边框的客户区 PNG。
- 使用 `SendInput` 在客户区像素坐标中移动和点击鼠标。
- 设置或取消游戏窗口置顶。
- 从已验证的近似 `16:9` 尺寸中调整客户区，默认使用 `1280 × 720`。

### 本窗口完成的图像处理

- `HimeBot.Vision` 固定引用 `OpenCvSharp4.Windows` `4.13.0.20261107`。
- `ImagePreprocessor` 能生成灰度图、整图 Otsu 二值图和矩形 ROI 副本。
- 诊断图片保存到 `%LocalAppData%/HimeBot/processed/`。
- 当前资源数字 ROI 使用 `1280 × 720` 客户区参考坐标：

```text
X = 540
Y = 12
Width = 195
Height = 48
```

- 用户已经人工确认 `*-resource-roi.png` 能稳定截取 `236万 / 454万`。
- 整图 Otsu 二值化会把复杂背景和人物高光压成大片纯黑或纯白，因此不直接用于 OCR；当前彩色 ROI 已能正确识别，暂时不增加灰度或二值预处理。
- `TemplateMatcher` 使用 OpenCV 彩色 `CCoeffNormed` 模板匹配，返回最佳分数、位置和模板尺寸；`IsSuccess` 只表示算法成功执行，不表示已经确认首页。
- 首页模板 `assets/koihime/templates/home-settings-icon.png` 为 `62 × 52`，取自 `1280 × 720` 首页截图中的稳定“ホーム設定”圆形图标，并在构建时复制到 App 输出目录。
- WPF 已增加“匹配首页模板”诊断按钮；按钮仅在存在最近一次成功截图时启用，并显示模板证据以及 `Home/Unknown` 场景结果。

### 本窗口完成的首页场景识别

人工采集的 `1280 × 720` 模板匹配样本：

| 场景 | 最佳分数 | X | Y |
| --- | ---: | ---: | ---: |
| 首页 1 | 0.84 | 948 | 535 |
| 首页 2 | 0.81 | 948 | 535 |
| 首页 3 | 0.86 | 948 | 535 |
| 其他页面 1 | 0.39 | 320 | 418 |
| 其他页面 2 | 0.36 | 287 | 334 |
| 待机动画 | 0.46 | 132 | 139 |
| 弹窗 | 0.48 | 16 | 634 |

- `GameScene` 当前包含 `Unknown`、`Home`、`MissionPanel` 和 `RewardReceiptDialog`；`Unknown` 表示当前观察证据不足，不代表执行异常。
- `HomeSceneRecognizer` 要求模板分数不低于 `0.75`，且最佳位置在 `(948, 535)` 的 X/Y 各 `±5` 像素内，才返回 `Home`，否则返回 `Unknown`。
- `HomeSceneRecognizer` 只接收基础数值，不引用 Vision、OpenCV 或 WPF 类型；当前只有一个确定性的实现，因此不增加 interface。
- `MissionPanelRecognizer` 要求任务面板模板分数不低于 `0.90`，且最佳位置在 `(24, 14)` 的 X/Y 各 `±5` 像素内，才返回 `MissionPanel`，否则返回 `Unknown`。
- `RewardReceiptDialogRecognizer` 要求“閉じる”模板分数不低于 `0.90`，且位置接近 `(520, 601)`、X/Y 容差各为 `±5`，才返回 `RewardReceiptDialog`。
- `MissionRewardAvailabilityRecognizer` 使用“一括完了”区域平均红色值判断 `Available/Unavailable/Unknown`；初始阈值为 `>= 170` 可领取、`<= 145` 不可领取，中间区间安全返回 `Unknown`。
- `App.xaml.cs` 在组合根创建共享截图模块和三个专用观察者；当前只有一个生产实现，因此没有为每个观察者增加 C# `interface`。

### 本窗口完成的分场景观察重构

- `src/HimeBot.App/Observation/Shared/KoihimeFrameCapture.cs` 统一隐藏截图文件命名、WGC 调用和 `1280 × 720` 尺寸校验；尺寸不符时安全失败并保留已经生成的截图路径。
- `HomeSceneObserver` 只回答当前是否确认首页；`MissionPanelObserver` 只回答是否确认任务面板，并在确认后判断奖励状态；`RewardReceiptDialogObserver` 只回答是否确认“閉じる”领取结果弹窗。
- `MissionPanelObserver` 使用固定区域 `(1070, 566, 141, 48)` 的平均 RGB 证据。颜色分析技术失败时仍保留已经确认的面板状态，但奖励状态降级为 `Unknown`，后续领取动作不得点击。
- WPF 将旧的“观察当前场景”入口替换为“观察首页”“观察任务面板”“观察领取弹窗”三个诊断命令；这些命令只观察，不触发输入。
- `KoihimeMissionPanelOpener` 点击前调用 `HomeSceneObserver`，点击后每隔 `200 ms` 调用 `MissionPanelObserver`，最长等待 `3 s`；只有确认任务面板后才报告成功。
- 旧的 `KoihimeObserver` 与 `KoihimeObservationResult` 已删除，避免总观察器随着新场景持续膨胀。
- 奖励弹窗模板 `reward-receipt-close-button.png` 为 `240 × 63`，取自参考位置 `(520, 601)`；可领取和不可领取按钮裁剪样本为 `141 × 48`，仅用于阈值校准，不在运行时执行模板匹配。
- 分场景观察重构已经通过 x64 Debug 构建和测试命令：两次构建均为 `0` 个错误，首次重新编译显示 `2` 个既有 OpenCvSharp `CS9057` 警告，最终增量构建显示 `0` 个警告；三个新诊断入口和奖励判定尚待人工运行验证。

### 本窗口完成的 OCR 链路

- `HimeBot.Vision` 固定引用 `TesseractOCR` `5.3.5`；该版本传递依赖 `Microsoft.Extensions.Logging.Abstractions` `7.0.1`，与固定的 `.NET 7` 基线兼容。
- 使用官方 `tessdata_fast` 的 `jpn.traineddata`，文件位于 `src/HimeBot.Vision/tessdata/`，构建时复制到应用输出目录的 `tessdata/`。
- `OcrRecognitionResult` 返回调用状态、说明、原始文字和平均置信度。
- `TesseractTextRecognizer`：
  - 从 `AppContext.BaseDirectory/tessdata` 加载日文模型；
  - 使用 `Language.Japanese`、`EngineMode.LstmOnly` 和 `PageSegMode.SingleLine`；
  - 通过 `Task.Run` 避免 OCR 阻塞 WPF UI 线程；
  - 使用 `using` 确定性释放 Engine、Pix Image 和 Page 的原生资源；
  - 当前为每次识别创建一次 Engine，先保证生命周期正确，性能需要实测后再优化。
- `App.xaml.cs` 在组合根创建并注入 `TesseractTextRecognizer`。
- `MainViewModel` 保存最近一次成功 ROI 路径；保存新截图或重新裁剪时清除旧 ROI 状态，避免识别过期图片。
- WPF 已增加“识别资源文字”按钮，按钮只有在 ROI 裁剪成功后才可用。

### 本窗口完成的粮食文字解析

- 首次创建 `HimeBot.Game.Koihime`，并建立 `HimeBot.App` 到游戏模块的单向项目引用；游戏模块当前不引用 Core、Vision 或 WPF。
- `GrainStock` 使用 `int` 保存当前粮食数值和容量，二者的固定单位都是“万”。
- `GrainStockParser`：
  - 使用 Unicode Form KC 将全角数字和全角斜杠转换为兼容形式；
  - 移除空格、换行等 Unicode 空白；
  - 要求文字严格包含一个 `/`，且两边都是“非负整数 + 万”；
  - 只移除单位，不把数值乘以 `10,000`；
  - 按游戏实际规则允许当前粮食数值大于容量。
- `GrainStockParseResult` 在成功时返回规范化文字和 `GrainStock`，失败时保留规范化文字、原因，并令 `Stock` 为 `null`。
- 已用仓库外临时控制台程序完成 `19` 个解析检查，覆盖普通文字、空白、全角字符、当前值大于容量、缺单位、多分隔符、符号、字母和 `int` 溢出；临时文件已经删除。
- `App.xaml.cs` 在组合根创建并注入 `GrainStockParser`；现有“识别资源文字”命令在 OCR 成功后自动解析，不新增测试按钮。

## 本窗口发现的问题与结论

| 事项 | 现象 | 结论或当前处理 |
| --- | --- | --- |
| 整图二值化观感差 | 大片区域过曝或变黑，细节比灰度图少 | Otsu 只能把像素分成两类，整图阈值不适合复杂游戏画面；先裁剪 ROI，再按识别结果决定预处理 |
| 大量按钮坐标如何取得 | 画图工具可人工测量，但维护成本较高 | 当前先使用固定 `1280 × 720` 参考坐标；WPF 截图坐标选择器属于效率工具，已明确延期，不阻塞 OCR 主线 |
| OCR 显示为 `0.9%` | 文字完全正确，但界面置信度看似极低 | 真实 ROI 探针确认 `MeanConfidence = 0.9`；包源码会把 Tesseract 的 `0～100` 值除以 `100`，所以实际是 `90%` |
| 置信度单位错误 | `MainViewModel` 原来使用 `{MeanConfidence:F1}%` | 当前代码已改为 `{MeanConfidence:P1}`，由 .NET 百分比格式自动换算；还需下一窗口人工确认界面显示 `90.0%` |
| OCR 包的 .NET 7 支持警告 | `TesseractOCR 5.5.2` 依赖 `Microsoft.Extensions.Logging.Abstractions 10.0.3`，又传递引入不正式支持 `net7.0` 的 10.0.3 系列依赖 | 已固定回 `TesseractOCR 5.3.5`；其日志抽象依赖为 `7.0.1`，相关构建警告已消失，用户已确认降级后的 OCR 链路运行成功 |
| “万”是否换算 | OCR 识别结果是 `236万/454万` | “万”是固定显示单位，结构化数值保留为 `236` 和 `454`，不乘以 `10,000` |
| 当前粮食能否大于容量 | 游戏内允许出现当前值大于容量 | 这是有效状态，解析器不校验 `CurrentAmount <= Capacity` |
| OpenCvSharp 分析器警告 | 分析器使用编译器 4.14，而 .NET 7 SDK 使用编译器 4.7 | 当前不影响构建和运行；最终确定 OpenCvSharp 兼容版本时处理 |
| 测试缺口 | 当前没有测试项目 | 用户已经决定暂缓 xUnit；`dotnet test` 只完成还原/构建，没有执行测试用例 |

## 验证记录

最近一次完整验证命令：

```powershell
dotnet build HimeBot.sln --configuration Debug --property:Platform=x64
dotnet test HimeBot.sln --configuration Debug --property:Platform=x64
```

结果：

- 构建成功，`0` 个错误；本次最终增量构建输出为 `0` 个警告。
- 首次重新编译受影响项目时仍出现 `2` 个既有 OpenCvSharp `CS9057` 分析器版本警告；它们不影响本次重构编译。`TesseractOCR` 间接依赖的 .NET 7 支持警告已经消失。
- 测试命令退出码为 `0`，但解决方案中没有测试项目。
- 临时解析器检查共 `19` 项，全部通过；这些检查不在仓库中，后续修改不会自动回归。
- 用户已人工运行降级后的 OCR；粮食解析接入后的状态栏显示尚待人工复验。
- 首页模板正负样本已经人工采集，`HomeSceneRecognizer` 注入和诊断命令接线已通过 x64 Debug 构建。
- 2026-11-15 用户确认接线后的程序编译、启动和运行成功；本窗口没有逐项记录首页、其他页面、待机动画与弹窗最终显示的 `Home/Unknown` 结果，因此下个窗口不得虚构这项验证已经完成。
- 重构前的一次场景观察入口及“观察当前场景”按钮曾通过 x64 Debug 构建并由用户人工确认运行成功；该入口现已被三个分场景观察命令替代。
- 两张任务状态样本已确认复制到 App 输出目录；由于分数受首页背景影响，运行时模板证据链路已停用，样本仅作为实验记录保留。
- “确认首页后点击任务入口”链路已通过 x64 Debug 构建：`0` 个错误、`2` 个既有 OpenCvSharp `CS9057` 警告；`dotnet test` 退出码为 `0`，但没有测试项目和测试用例。用户已人工确认首页能够打开任务面板，非首页不会点击。
- 任务面板模板原始证据接线曾因运行中的旧版 HimeBot 锁定 App EXE/DLL 而先使用临时 `OutDir` 验证；旧进程退出后，标准 x64 Debug 构建已经成功。
- `MissionPanelRecognizer` 与安全场景合并接线已通过标准 x64 Debug 构建：`0` 个错误、`2` 个既有 OpenCvSharp `CS9057` 警告；`dotnet test` 退出码为 `0`，但没有测试项目。用户已在构建后人工确认场景判断运行成功。
- 任务面板打开后的后置条件轮询已在独立临时输出目录完成 x64 Debug 编译：`0` 个错误、`2` 个既有 OpenCvSharp `CS9057` 警告；同一临时输出目录下的 `dotnet test` 退出码为 `0`，但没有测试项目。随后用户已完成标准构建和人工验证，确认新闭环运行成功。
- 重构前的精简观察结果和任务面板打开闭环曾通过标准构建与人工验证；当前任务面板打开器已改为分别使用 `HomeSceneObserver` 和 `MissionPanelObserver`，新接线已通过构建，等待再次人工复验。
- 分场景观察重构后的标准 x64 Debug 构建与 `dotnet test` 均以退出码 `0` 完成；解决方案仍没有测试项目，三个新诊断入口需要人工运行验证。

## 当前未提交工作树

以下内容属于当前学习链路，下一窗口必须保留：

```text
 M PROJECT_STATUS.md
 M docs/project-structure.md
 M src/HimeBot.App/App.xaml.cs
 M src/HimeBot.App/HimeBot.App.csproj
 M src/HimeBot.App/Interaction/KoihimeMissionPanelOpener.cs
 M src/HimeBot.App/MainWindow.xaml
 D src/HimeBot.App/Observation/KoihimeObservationResult.cs
 D src/HimeBot.App/Observation/KoihimeObserver.cs
 M src/HimeBot.App/ViewModels/MainViewModel.cs
 M src/HimeBot.Game.Koihime/Domain/GameScene.cs
?? src/HimeBot.App/Observation/Home/
?? src/HimeBot.App/Observation/Mission/
?? src/HimeBot.App/Observation/Reward/
?? src/HimeBot.App/Observation/Shared/
?? src/HimeBot.Game.Koihime/Perception/RewardReceiptDialogRecognizer.cs
```

## 本次会话交接摘要

- 固定技术基线仍为 `.NET 7`、WPF、x64；没有升级目标框架。
- `TesseractOCR` 已从 `5.5.2` 固定为 `5.3.5`，从而避免间接引入不正式支持 .NET 7 的 `Microsoft.Extensions.Logging.Abstractions 10.0.3`；用户已确认降级后 OCR 能运行。
- 粮食 OCR 原文由 `GrainStockParser` 规范化和解析；“万”只是固定单位，不乘以 `10,000`，结构化数值继续使用 `int`，并允许当前粮食大于容量。
- 首页模板只截取稳定的“ホーム設定”图标，文件为 `assets/koihime/templates/home-settings-icon.png`；通知红点不属于模板。
- Vision 的 `TemplateMatcher` 只产生通用分数和位置，Koihime 的 `HomeSceneRecognizer` 才解释游戏场景；App 负责组装两者并显示证据。
- App 使用 `KoihimeFrameCapture` 共享 WGC 截图和尺寸校验；`HomeSceneObserver`、`MissionPanelObserver` 与 `RewardReceiptDialogObserver` 各自隐藏对应场景的视觉匹配与解释细节。当前每类只有一个生产实现，因此暂不增加 C# `interface`。
- 任务入口无奖励与有奖励模板均为 `90 × 90`，但不再参与运行时判定；任务入口点击点取样本区域中心 `(1235, 565)`，且只在最新观察结果为 `Home` 时提交输入。
- 任务面板固定标题模板为 `310 × 74`；`MissionPanelRecognizer` 使用分数 `>= 0.90`、位置接近 `(24, 14)` 且 X/Y 容差各为 `±5` 的规则生成 `MissionPanel/Unknown`，该判断本身不触发输入。
- `KoihimeMissionPanelOpener` 点击前使用首页观察者确认前置条件，点击后每隔 `200 ms` 使用任务面板观察者验证后置条件，最长等待 `3 s`；旧接线曾完成人工验证，新拆分接线等待人工复验。
- 首页判定规则暂定为分数 `>= 0.75`，且位置接近 `(948, 535)`、X/Y 容差各为 `±5`；该规则目前仅适用于 `1280 × 720` 客户区截图。
- 安全规则不变：`Unknown` 不触发鼠标输入，不采用“识别不到就移动到中心双击”的恢复方案；待机动画恢复策略应在观察循环设计时另行讨论。
- 当前没有 xUnit 测试项目。`dotnet test` 退出码为 `0` 只代表还原/项目加载成功，没有自动测试用例可报告。

## 下一窗口恢复顺序

1. 阅读 `AGENTS.md`、本文档和 `docs/project-structure.md`。
2. 执行 `git status --short`，确认上述未提交文件仍在；保留用户改动。
3. 将游戏客户区保持为 `1280 × 720`，依次人工验证“观察首页”“观察任务面板”“观察领取弹窗”三个诊断按钮。
4. 在可领取任务面板确认结果为 `MissionPanel + Available`，在领取结束后的面板确认结果为 `MissionPanel + Unavailable`；如阈值不稳定，记录状态栏中的平均 RGB 后再调整。
5. 再次人工验证“打开任务”仍满足：首页确认后才点击，点击后必须确认任务面板。
6. 上述观察结果稳定后，再实现最小的奖励领取任务；领取任务应组合任务面板观察者与领取弹窗观察者，不把点击和重试塞入观察者。

## 下一条主线

分场景观察重构后，下一条主线是先用已有三个样本验证观察结果，再实现奖励领取任务：

```text
MissionPanelObserver 确认 MissionPanel + Available
    → 奖励任务点击右下角“一括完了”
    → RewardReceiptDialogObserver 确认领取弹窗
    → 奖励任务点击“閉じる”
    → MissionPanelObserver 确认 MissionPanel + Unavailable
```

当前观察和点击坐标仅适用于 `1280 × 720` 客户区。程序不会判断首页红色感叹号，也不会根据首页奖励状态决定是否进入任务面板。任务面板打开后的状态轮询与超时已实现；奖励领取点击、弹窗关闭、自动重试和恢复尚未实现。
