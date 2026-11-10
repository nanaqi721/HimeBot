using System.Runtime.InteropServices;
using Windows.Graphics.Capture;

namespace HimeBot.Platform.Windows.ScreenCapture;

internal static class GraphicsCaptureItemFactory
{
    /// <summary>
    /// COM 中，一个对象可以实现多个接口。调用者需要通过 IID，也就是接口 GUID，明确表示：
    /// 请返回这个对象的哪一种接口指针？
    /// 这里的 GUID 是 WinRT ABI 层的 IGraphicsCaptureItem 接口 ID。也就是说，
    /// 后面调用 Windows 时要求返回一个可以被投影成 GraphicsCaptureItem 的接口指针。
    /// </summary>
    private static readonly Guid GraphicsCaptureItemInterfaceId =
        new("79C3F95B-31F7-4EC2-A464-632EF5D30760");

    /// <summary>
    /// 把一个传统的 Win32 窗口句柄 HWND，
    /// 转换成 Windows Graphics Capture API 能使用的 GraphicsCaptureItem
    /// </summary>
    /// <param name="windowHandle">Win32窗口句柄</param>
    /// <returns> GraphicsCaptureItem </returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    internal static GraphicsCaptureItem CreateForWindow(
        IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            throw new ArgumentException(
                "窗口句柄不能为空。",
                nameof(windowHandle));
        }

        /// <summary>
        /// GraphicsCaptureItem 是 C#/WinRT 投影出来的托管类型，但它背后仍然建立在 COM/WinRT ABI 上。
        /// As<T>() 的意思近似于：
        /// 从 GraphicsCaptureItem 的激活工厂中查询它是否支持 
        /// IGraphicsCaptureItemInterop 接口，并返回该接口的托管包装。
        /// 可以把“激活工厂”理解成 Windows 为某个 WinRT 类型提供的系统级对象创建入口。
        /// </summary>
        IGraphicsCaptureItemInterop interop =
            GraphicsCaptureItem.As<IGraphicsCaptureItemInterop>();

        // 请根据这个 HWND 创建一个截图目标，并把它的 IGraphicsCaptureItem 接口地址给我。
        int hresult = interop.CreateForWindow(
            windowHandle,
            GraphicsCaptureItemInterfaceId,
            out IntPtr itemPointer);

        Marshal.ThrowExceptionForHR(hresult);

        if (itemPointer == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Windows 没有返回有效的截图对象。");
        }

        try
        {
            // ABI 是 Application Binary Interface，即“二进制调用约定”。
            // 这里的 itemPointer 只是一个原生 COM 接口地址，
            // C# 无法直接使用它的属性和方法。
            // FromAbi 会将它包装或投影成托管对象：GraphicsCaptureItem
            return GraphicsCaptureItem.FromAbi(itemPointer);
        }
        finally
        {
            Marshal.Release(itemPointer);
        }
    }

    // 这是从外部 COM 系统导入的接口，不需要也不能由当前 C# 代码实现。
    [ComImport]
    [Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
    // 表示这是一个继承 COM IUnknown 的接口。
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IGraphicsCaptureItemInterop
    {

        [PreserveSig]
        int CreateForWindow(
            IntPtr windowHandle,
            in Guid interfaceId,
            out IntPtr result);
    }
}