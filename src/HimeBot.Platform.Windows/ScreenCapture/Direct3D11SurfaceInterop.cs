using System.Runtime.InteropServices;
using Vortice.Direct3D11;
using Windows.Graphics.DirectX.Direct3D11;
using WinRT;

namespace HimeBot.Platform.Windows.ScreenCapture;

internal static class Direct3D11SurfaceInterop
{

    // 这个 GUID 是原生 COM 接口 ID3D11Texture2D 的接口标识符，也称为 IID。
    private static readonly Guid ID3D11Texture2DGuid =
        new("6F15AAF2-D208-4E89-9AB4-489535D34F9C");

    internal static ID3D11Texture2D GetTexture(
        IDirect3DSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        IDirect3DDxgiInterfaceAccess interfaceAccess =
            surface.As<IDirect3DDxgiInterfaceAccess>();

        IntPtr texturePointer =
            interfaceAccess.GetInterface(
                ID3D11Texture2DGuid);

        if (texturePointer == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Direct3D Surface 返回了空的纹理指针。");
        }

        return new ID3D11Texture2D(
            texturePointer);
    }

    [ComImport]
    [Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]
    private interface IDirect3DDxgiInterfaceAccess
    {
        IntPtr GetInterface(
            in Guid iid);
    }
}