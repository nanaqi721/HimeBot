# HimeBot 项目状态

> 最近验证日期：2026-11-14

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

- 构建成功，`0` 个错误、`2` 个警告。
- `TesseractOCR` 间接依赖的 .NET 7 支持警告已经消失；当前 `2` 个警告均为 OpenCvSharp `CS9057`。
- 测试命令退出码为 `0`，但解决方案中没有测试项目。
- 临时解析器检查共 `19` 项，全部通过；这些检查不在仓库中，后续修改不会自动回归。
- 用户已人工运行降级后的 OCR；粮食解析接入后的状态栏显示尚待人工复验。

## 当前未提交工作树

以下内容属于当前学习链路，下一窗口必须保留：

```text
 M PROJECT_STATUS.md
 M docs/project-structure.md
 M HimeBot.sln
 M src/HimeBot.App/App.xaml.cs
 M src/HimeBot.App/HimeBot.App.csproj
 M src/HimeBot.App/ViewModels/MainViewModel.cs
?? src/HimeBot.Game.Koihime/
```

## 下一窗口恢复顺序

1. 阅读 `AGENTS.md`、本文档和 `docs/project-structure.md`。
2. 执行 `git status --short`，确认上述未提交文件仍在；保留用户改动。
3. 构建 x64 Debug，确认解决方案中的五个项目都能成功编译。
4. 运行现有链路：保存截图 → 裁剪资源数字 → 识别资源文字。
5. 确认状态栏同时显示 OCR 原文、约 `90.0%` 的置信度、规范化文字、当前粮食与容量。
6. 对 `236万/ 454万`，确认结构化数值显示为 `236万` 与 `454万`，没有换算成实际个数。

## 下一条主线

人工验证新接入的粮食解析显示：

```text
OCR 原文："236万/ 454万"
    → 去除空白并校验格式
    → 当前粮食：236万
    → 容量：454万
```

确认真实 ROI 的最终显示后，再讨论是否将 `ResourceCounterRegionX` 等临时诊断名称统一为粮食领域术语，并决定下一条最小识别闭环；暂时不创建通用坐标选择器、测试项目或自动点击任务。
