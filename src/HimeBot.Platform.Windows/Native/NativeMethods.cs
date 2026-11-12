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

[StructLayout(LayoutKind.Sequential)]
internal struct NativeMouseInput
{
    public int X;
    public int Y;
    public uint MouseData;
    public uint Flags;
    public uint Time;
    public UIntPtr ExtraInfo;
}

[StructLayout(LayoutKind.Explicit)]
internal struct NativeInputUnion
{
    [FieldOffset(0)]
    public NativeMouseInput Mouse;
}

[StructLayout(LayoutKind.Sequential)]
internal struct NativeInput
{
    public uint Type;
    public NativeInputUnion Data;
}

internal static class NativeMethods
{
    internal const int DwmwaExtendedFrameBounds = 9;

    internal const uint InputMouse = 0;
    internal const uint MouseEventMove = 0x0001;
    internal const uint MouseEventVirtualDesk = 0x4000;
    internal const uint MouseEventAbsolute = 0x8000;
    internal const uint MouseEventLeftDown = 0x0002;
    internal const uint MouseEventLeftUp = 0x0004;

    internal const int SmXVirtualScreen = 76;
    internal const int SmYVirtualScreen = 77;
    internal const int SmCxVirtualScreen = 78;
    internal const int SmCyVirtualScreen = 79;

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

    [DllImport(
    "user32.dll",
    SetLastError = true)]
    internal static extern uint SendInput(
    uint inputCount,
    [In] NativeInput[] inputs,
    int inputSize);

    [DllImport("user32.dll")]
    internal static extern int GetSystemMetrics(
    int index);
}
