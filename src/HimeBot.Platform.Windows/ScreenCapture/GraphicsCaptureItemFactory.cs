using System.Runtime.InteropServices;
using Windows.Graphics.Capture;

namespace HimeBot.Platform.Windows.ScreenCapture;

internal static class GraphicsCaptureItemFactory
{
    private static readonly Guid GraphicsCaptureItemInterfaceId =
        new("79C3F95B-31F7-4EC2-A464-632EF5D30760");

    internal static GraphicsCaptureItem CreateForWindow(
        IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            throw new ArgumentException(
                "窗口句柄不能为空。",
                nameof(windowHandle));
        }

        IGraphicsCaptureItemInterop interop =
            GraphicsCaptureItem.As<IGraphicsCaptureItemInterop>();

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
            return GraphicsCaptureItem.FromAbi(itemPointer);
        }
        finally
        {
            Marshal.Release(itemPointer);
        }
    }

    [ComImport]
    [Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
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