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

internal static class NativeMethods
{
	[DllImport(
		"user32.dll",
		SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool GetClientRect(
		IntPtr windowHandle,
		out NativeRect clientRectangle);
}