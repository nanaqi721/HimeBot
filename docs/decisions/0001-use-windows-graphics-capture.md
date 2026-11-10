# 使用 Windows Graphics Capture 获取游戏画面

状态：已接受（2026-11-10）

HimeBot 的 Windows 生产截图路径使用 Windows Graphics Capture（WGC），而不是从桌面最终合成画面复制像素。`Graphics.CopyFromScreen` 会把遮挡游戏的其他窗口一起截入，`PrintWindow` 在恋姬大战窗口上只得到黑屏；WGC 已人工验证能在游戏被其他窗口遮挡时取得游戏自身画面，因此将 WGC 细节封装在 `HimeBot.Platform.Windows` 中。

WGC 返回包含非客户区的可见窗口帧，而自动化坐标以游戏客户区像素为准。实现使用 `ClientToScreen` 得到客户区原点的屏幕坐标，使用 `DwmGetWindowAttribute(DWMWA_EXTENDED_FRAME_BOUNDS)` 得到 WGC 帧对应的窗口原点，两者相减得到动态裁剪位置；不得用固定标题栏或边框像素代替该换算。

在当前 `.NET 7`、C#/WinRT 与 `Vortice.Direct3D11` 3.5.0 组合下，WinRT Direct3D 设备通过原生 `CreateDirect3D11DeviceFromDXGIDevice` 和 `WinRT.MarshalInterface<IDirect3DDevice>.FromAbi` 建立。项目不使用 Vortice 的泛型设备转换辅助方法，因为它产生的旧式 `System.__ComObject` 已在释放设备和创建 frame pool 时触发 `E_NOINTERFACE`。

这一决定带来的约束是：截图实现依赖 Windows 10 19041 SDK 基线和 Direct3D 11；GPU 帧必须先复制到可读的 staging texture，再按 `RowPitch` 逐行读取；最终写入 Bitmap 时还必须独立遵守 `Stride`。当前只承诺一次性客户区 PNG，不承诺连续预览、最小化窗口捕获或窗口尺寸变化期间的自动恢复。
