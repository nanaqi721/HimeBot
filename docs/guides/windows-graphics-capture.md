# Windows Graphics Capture 实现与排错指南

本文记录 HimeBot 当前已经运行验证的单帧游戏截图流程。修改 `src/HimeBot.Platform.Windows/ScreenCapture/`、Direct3D/WinRT 互操作或客户区坐标换算前，先阅读 [0001-use-windows-graphics-capture.md](../decisions/0001-use-windows-graphics-capture.md)。

## 当前能力与边界

- 从已经定位到的游戏进程重新取得主窗口句柄。
- 使用 Windows Graphics Capture 获取一帧 `B8G8R8A8_UNorm` 画面。
- 游戏被普通窗口遮挡时，截图仍只包含游戏自身画面。
- 动态移除标题栏和边框，保存与游戏客户区对应的 PNG。
- 在 UI 线程之外执行截图，并支持取消和 5 秒第一帧超时。

当前尚未实现连续捕获、实时预览、窗口缩放过程中的重建、最小化窗口行为处理、OCR 或自动重试。

## 文件职责

```text
HimeBot.App
├─ MainWindow.xaml                         # 绑定“保存客户区截图”命令
├─ ViewModels/MainViewModel.cs             # 生成路径并异步发起截图
└─ App.xaml.cs                             # 组合 Windows 截图实现

HimeBot.Platform.Windows
├─ Native/NativeMethods.cs                 # GetClientRect、ClientToScreen、DWM 边界
└─ ScreenCapture/
   ├─ WindowsGameScreenshotCapture.cs      # 单帧编排、GPU 回读、裁剪与 PNG 保存
   ├─ GraphicsCaptureItemFactory.cs        # HWND -> GraphicsCaptureItem
   ├─ Direct3D11CaptureDevice.cs            # 原生 D3D11 与 WinRT 设备桥接/所有权
   ├─ Direct3D11SurfaceInterop.cs           # IDirect3DSurface -> ID3D11Texture2D
   └─ GameScreenshotCaptureResult.cs        # 返回状态、路径和最终图片尺寸
```

这些 Windows、COM、WinRT 和 GPU 细节必须留在 `HimeBot.Platform.Windows`。WPF 只发出命令并显示结果，游戏任务逻辑只使用客户区像素，不感知 HWND、DPI、WGC 或 Direct3D 类型。

## 执行流程

1. `CaptureToPngAsync` 检查参数与 `GraphicsCaptureSession.IsSupported()`，再用 `Task.Run` 把同步的原生捕获工作移出 UI 线程。
2. 根据 `GameWindowInfo.ProcessId` 重新取得当前主窗口句柄，避免长期保存可能失效的 HWND。
3. `GraphicsCaptureItemFactory` 通过 `IGraphicsCaptureItemInterop.CreateForWindow` 为该 HWND 创建捕获对象。
4. `Direct3D11CaptureDevice` 创建带 `BgraSupport` 的原生 D3D11 设备，再把 DXGI 设备桥接成 WinRT `IDirect3DDevice`。
5. `CreateFreeThreaded` 创建一缓冲区 frame pool；`FrameArrived` 设置 `ManualResetEventSlim`，调用方等待第一帧，最长 5 秒。
6. `Direct3D11SurfaceInterop` 从帧的 `IDirect3DSurface` 取得原生 `ID3D11Texture2D`。
7. 创建 `ResourceUsage.Staging`、`CpuAccessFlags.Read` 的 staging texture，用 `CopyResource` 从 GPU 帧复制，再 `Map` 到 CPU。
8. GPU 每行的实际跨度是 `MappedSubresource.RowPitch`，按行复制有效的 `width * 4` 字节到紧凑的托管数组；`Unmap` 必须位于 `finally`。
9. 用客户区屏幕原点减去 DWM 可见窗口原点，得到客户区在完整 WGC 帧中的裁剪起点，并校验整个矩形没有越界。
10. 创建客户区大小的 `Bitmap`，按 Bitmap 自己的 `Stride` 逐行写入裁剪像素，解锁后编码为 PNG。
11. 所有 frame、frame pool、capture session、纹理、设备和进程对象按所有权确定性释放。

## 为什么 WGC 尺寸比客户区大

`GraphicsCaptureItem.Size` 和 WGC 帧描述的是窗口的可见整体，通常包括标题栏与边框；`GetClientRect` 只描述游戏绘制内容所在的客户区。因此曾观察到 WGC 为 `1060 × 641`、客户区为 `1056 × 594`，这不是缩放错误，也不能通过固定减去 4 或 47 像素修正。

当前换算使用：

```text
cropX = 客户区左上角屏幕 X - DWM 可见窗口左边界
cropY = 客户区左上角屏幕 Y - DWM 可见窗口上边界
cropWidth  = GetClientRect 宽度
cropHeight = GetClientRect 高度
```

这样可以适应主题、窗口边框和 DPI 造成的非客户区尺寸变化。若窗口在“取得坐标”和“取得帧”之间发生缩放，越界校验会返回失败，而不是保存错位图片。

## 已遇到的错误与结论

| 现象 | 原因 | 当前处理 |
|---|---|---|
| `CaptureGameScreenshotAsync`、`CanCaptureGameScreenshot` 在 `AsyncRelayCommand` 构造处报“名称不存在于当前上下文” | 构造函数已经引用方法组，但两个方法的实现当时尚未添加；C# 类成员的书写先后顺序本身没有限制 | 在 `MainViewModel` 类内添加签名匹配的方法；异步执行方法接收 `CancellationToken`，启用条件方法返回 `bool` |
| `CopyFromScreen` 截入了覆盖在游戏上的窗口 | 它读取桌面最终合成像素，不是目标窗口自己的表面 | 生产截图改用 WGC |
| `PrintWindow` 输出黑屏 | 该游戏的渲染链路没有向 `PrintWindow` 提供可用内容 | 实验已回滚，不作为后备生产路径 |
| WGC 图片比 `GetClientRect` 大且带标题栏 | WGC 返回可见完整窗口，两个 API 的坐标范围不同 | 使用 `ClientToScreen` 与 DWM 扩展边界动态裁剪 |
| 释放 WinRT 设备时出现 `InvalidCastException`、`E_NOINTERFACE` | Vortice 3.5.0 泛型转换返回旧式 `System.__ComObject`，不能按 C#/WinRT 投影对象释放 | 直接 P/Invoke 原生转换函数，并用 `WinRT.MarshalInterface<T>.FromAbi` 投影 |
| `CreateFreeThreaded` 中出现 `InvalidCastException`、`0x80004002` | 传入 frame pool 的仍是上一条中的不兼容 COM 包装对象 | 使用同一原生桥接修复；不要恢复已注释的泛型转换实现 |
| GPU 帧无法直接供 CPU 稳定读取 | 默认 WGC 纹理不是 CPU-readable staging resource | 复制到 staging texture 后再 `Map` |
| 图片可能错行或倾斜 | GPU `RowPitch` 和 Bitmap `Stride` 都可能大于紧凑行字节数 | 源与目标分别按各自跨度逐行复制 |

COM 裸指针的规则是：创建函数返回的指针在完成 `FromAbi` 投影后由调用方 `Marshal.Release` 一次；投影出来的 WinRT 对象和原生 Vortice 对象再按各自所有权 `Dispose`。不要把 COM 对象强制转换成 `IDisposable` 来猜测其生命周期。

## 人工验证步骤

1. 使用 DMM 快捷方式启动游戏，在 HimeBot 中点击“查找游戏窗口”。
2. 记录界面显示的客户区宽高。
3. 用另一个普通窗口遮挡游戏的一部分，不要最小化游戏。
4. 点击“保存客户区截图”。
5. 打开状态信息给出的 `%LocalAppData%/HimeBot/captures/` PNG。
6. 确认 PNG 宽高与客户区一致、没有标题栏和边框、游戏边缘未被裁掉，且遮挡窗口没有进入图片。

代码变更后的默认构建命令是：

```powershell
dotnet build HimeBot.sln --configuration Debug --property:Platform=x64
```

当前没有自动化测试项目，因此必须把“构建成功”和“人工画面验证成功”分开记录，不能用其中一个代替另一个。
