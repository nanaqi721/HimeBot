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


// 把下面的字段按照声明顺序排列到内存中。
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

// 不要由 .NET 自动排列字段，
// 每个字段放在什么内存位置，由 [FieldOffset] 明确指定
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

    internal static readonly IntPtr HwndTopmost = new IntPtr(-1);
    internal static readonly IntPtr HwndNotTopmost = new IntPtr(-2);

    internal const uint SwpNoSize = 0x0001;
    internal const uint SwpNoMove = 0x0002;
    internal const uint SwpNoActivate = 0x0010;
    internal const uint SwpNoZOrder = 0x0004;

    [DllImport(
        "user32.dll",
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetClientRect(
        IntPtr windowHandle,
        out NativeRect clientRectangle);

    //把“相对于某个窗口客户区的坐标”，转换成“整个屏幕上的坐标”。
    [DllImport(
        "user32.dll",
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ClientToScreen(
        IntPtr windowHandle,
        ref NativePoint point);

    [DllImport(
    "user32.dll",
    SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetWindowRect(
    IntPtr windowHandle,
    out NativeRect windowRectangle);

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
    // 这个数组的数据是从 C# 传给原生函数的输入数据，原生函数不需要把修改后的内容传回 C#。
    // 我传给你数据，你别修改也别返回
    [In] NativeInput[] inputs,
    int inputSize);

    [DllImport("user32.dll")]
    internal static extern int GetSystemMetrics(
    int index);

    [DllImport(
    "user32.dll",
    SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetWindowPos(
    IntPtr windowHandle,
    IntPtr insertAfter,
    int x,
    int y,
    int width,
    int height,
    uint flags);
}
