# HimeBot 项目状态

> 最近验证日期：2026-11-14

本文档是新窗口恢复开发上下文的入口。先阅读根目录 `AGENTS.md`，再阅读本文档；更改项目布局、引用方向、模块职责或实现阶段前，还要阅读 `docs/project-structure.md`。

## 当前里程碑

根解决方案已经完成并人工验证第一条最小 OCR 纵向链路：

```text
Windows Graphics Capture 客户区截图
    → 按固定参考坐标裁剪资源数字 ROI
    → Tesseract 日文单行识别
    → WPF 状态栏显示 OCR 原文和平均置信度
```

真实运行时，彩色 ROI 已正确识别顶部资源文本 `236万/454万`（Tesseract 原文可能在 `/` 后保留一个空格）。引擎返回的平均置信度为 `0.9`，其含义是比例 `90%`，不是百分制的 `0.9%`。

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

## 本窗口发现的问题与结论

| 事项 | 现象 | 结论或当前处理 |
| --- | --- | --- |
| 整图二值化观感差 | 大片区域过曝或变黑，细节比灰度图少 | Otsu 只能把像素分成两类，整图阈值不适合复杂游戏画面；先裁剪 ROI，再按识别结果决定预处理 |
| 大量按钮坐标如何取得 | 画图工具可人工测量，但维护成本较高 | 当前先使用固定 `1280 × 720` 参考坐标；WPF 截图坐标选择器属于效率工具，已明确延期，不阻塞 OCR 主线 |
| OCR 显示为 `0.9%` | 文字完全正确，但界面置信度看似极低 | 真实 ROI 探针确认 `MeanConfidence = 0.9`；包源码会把 Tesseract 的 `0～100` 值除以 `100`，所以实际是 `90%` |
| 置信度单位错误 | `MainViewModel` 原来使用 `{MeanConfidence:F1}%` | 当前代码已改为 `{MeanConfidence:P1}`，由 .NET 百分比格式自动换算；还需下一窗口人工确认界面显示 `90.0%` |
| OCR 包的 .NET 7 支持警告 | `TesseractOCR 5.5.2` 依赖 `Microsoft.Extensions.Logging.Abstractions 10.0.3`，又传递引入不正式支持 `net7.0` 的 10.0.3 系列依赖 | 已固定回 `TesseractOCR 5.3.5`；其日志抽象依赖为 `7.0.1`，现有 OCR API 无需改写，完整构建中相关支持警告已消失。由于原生 Tesseract 版本也从 5.5.1 变为 5.3.1，仍需用真实 ROI 人工回归识别结果 |
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
- 用户已人工运行 OCR，识别文本正确；`P1` 百分比显示修正尚待人工复验。

## 当前未提交工作树

以下内容属于当前学习链路，下一窗口必须保留：

```text
 M PROJECT_STATUS.md
 M docs/project-structure.md
 M src/HimeBot.App/App.xaml.cs
 M src/HimeBot.App/MainWindow.xaml
 M src/HimeBot.App/ViewModels/MainViewModel.cs
 M src/HimeBot.Vision/HimeBot.Vision.csproj
?? src/HimeBot.Vision/Ocr/
?? src/HimeBot.Vision/tessdata/
```

`tessdata/jpn.traineddata` 是约 2.36 MiB 的二进制语言模型。不要删除或用文本方式改写。

## 下一窗口恢复顺序

1. 阅读 `AGENTS.md`、本文档和 `docs/project-structure.md`。
2. 执行 `git status --short`，确认上述未提交文件仍在；保留用户改动。
3. 构建 x64 Debug，确认 `tessdata/jpn.traineddata` 仍复制到 `HimeBot.App` 输出目录。
4. 运行现有链路：保存截图 → 裁剪资源数字 → 识别资源文字。
5. 确认状态栏显示约 `90.0%`，而不是 `0.9%`；这是当前最小人工完成标准。
6. 使用同一张真实 ROI 人工复验 `TesseractOCR 5.3.5` 的识别文字和置信度，确认包降级没有造成行为回归。

## 下一条主线

OCR 包降级后的真实 ROI 行为和百分比显示确认后，再实现资源文本的规范化与解析：

```text
OCR 原文："236万/ 454万"
    → 去除空白并校验格式
    → 当前值：2,360,000
    → 最大值：4,540,000
```

该规则包含游戏文本含义，应先讨论是否创建 `HimeBot.Game.Koihime`，将规范化与数值解析放入游戏模块；`HimeBot.Vision` 继续只返回 OCR 原文和置信度。暂时不创建通用坐标选择器、测试项目或自动点击任务。
