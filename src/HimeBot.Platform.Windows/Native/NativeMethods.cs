using System.Runtime.InteropServices;

namespace HimeBot.Platform.Windows.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct NativeRect
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}

[StructLayout(LayoutKind.Sequential)]
internal struct NativePoint
{
    public int X;
    public int Y;
}

internal static class NativeMethods
{
    internal const int DwmwaExtendedFrameBounds = 9;

    [DllImport(
        "user32.dll",
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetClientRect(
        IntPtr windowHandle,
        out NativeRect clientRectangle);

    [DllImport(
        "user32.dll",
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ClientToScreen(
        IntPtr windowHandle,
        ref NativePoint point);

    [DllImport(
        "dwmapi.dll",
        PreserveSig = true)]
    internal static extern int DwmGetWindowAttribute(
        IntPtr windowHandle,
        int attribute,
        out NativeRect attributeValue,
        int attributeSize);
}
