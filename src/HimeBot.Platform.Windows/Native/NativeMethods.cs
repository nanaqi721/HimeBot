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
	internal const uint PrintWindowClientOnly = 0x00000001;

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
		"user32.dll",
		SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool PrintWindow(
		IntPtr windowHandle,
		IntPtr destinationDeviceContext,
		uint flags);
}
