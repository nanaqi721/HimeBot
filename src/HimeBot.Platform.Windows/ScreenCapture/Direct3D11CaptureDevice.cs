using HimeBot.Platform.Windows.Native;
using System.Runtime.InteropServices;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Windows.Graphics.DirectX.Direct3D11;

namespace HimeBot.Platform.Windows.ScreenCapture;

internal sealed class Direct3D11CaptureDevice : IDisposable
{
    private bool _isDisposed;

    private Direct3D11CaptureDevice(
        ID3D11Device nativeDevice,
        IDirect3DDevice winRtDevice)
    {
        NativeDevice = nativeDevice;
        WinRtDevice = winRtDevice;
    }

    internal ID3D11Device NativeDevice { get; }

    internal IDirect3DDevice WinRtDevice { get; }


    internal static Direct3D11CaptureDevice Create()
    {
        ID3D11Device nativeDevice =
            D3D11.D3D11CreateDevice(
                DriverType.Hardware,
                DeviceCreationFlags.BgraSupport);

        try
        {
            using IDXGIDevice dxgiDevice =
                nativeDevice.QueryInterface<IDXGIDevice>();

            int hresult =
                NativeMethods.CreateDirect3D11DeviceFromDXGIDevice(
                    dxgiDevice.NativePointer,
                    out IntPtr winRtDevicePointer);

            Marshal.ThrowExceptionForHR(hresult);

            if (winRtDevicePointer == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    "创建 WinRT Direct3D 设备失败：返回了空指针。");
            }

            try
            {
                IDirect3DDevice winRtDevice =
                    WinRT.MarshalInterface<IDirect3DDevice>.FromAbi(
                        winRtDevicePointer);

                return new Direct3D11CaptureDevice(
                    nativeDevice,
                    winRtDevice);
            }
            finally
            {
                Marshal.Release(winRtDevicePointer);
            }
        }
        catch
        {
            nativeDevice.Dispose();
            throw;
        }
    }

    /*internal static Direct3D11CaptureDevice Create()
    {
        ID3D11Device nativeDevice =
            D3D11.D3D11CreateDevice(
                DriverType.Hardware,
                DeviceCreationFlags.BgraSupport);

        try
        {
            using IDXGIDevice dxgiDevice =
                nativeDevice.QueryInterface<IDXGIDevice>();

            IDirect3DDevice winRtDevice =
                D3D11.CreateDirect3D11DeviceFromDXGIDevice<
                    IDirect3DDevice>(
                    dxgiDevice);

            return new Direct3D11CaptureDevice(
                nativeDevice,
                winRtDevice);
        }
        catch
        {
            nativeDevice.Dispose();
            throw;
        }
    }*/



    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        try
        {
            WinRtDevice.Dispose();
        }
        finally
        {
            NativeDevice.Dispose();
        }
    }

    private static class NativeMethods
    {
        [DllImport("d3d11.dll", ExactSpelling = true)]
        internal static extern int CreateDirect3D11DeviceFromDXGIDevice(
            IntPtr dxgiDevice,
            out IntPtr graphicsDevice);
    }
}