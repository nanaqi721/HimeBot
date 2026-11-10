这段代码的核心作用是：

> 创建一个原生 Direct3D 11 设备，再把它桥接成 Windows Graphics Capture 能接受的 WinRT 设备，并统一管理二者的生命周期。

这里的 `Create()` 不是创建两个互不相关的 GPU 设备，而是为同一套 Direct3D 资源准备两种接口：

- `NativeDevice`：Vortice 的原生 `ID3D11Device`，供纹理复制、GPU 回读等底层操作使用。
- `WinRtDevice`：WinRT 的 `IDirect3DDevice`，供 `Direct3D11CaptureFramePool` 使用。

## 类本身的设计

```
internal sealed class Direct3D11CaptureDevice : IDisposable
```

- `internal`：只能在 `HimeBot.Platform.Windows` 程序集内部使用，不暴露给其他项目。
- `sealed`：禁止继承，避免继承类改变资源释放规则。
- `IDisposable`：这个类拥有 GPU/COM 资源，使用完必须主动释放，不能只等垃圾回收器处理。

构造函数是 `private`：

```
private Direct3D11CaptureDevice(
    ID3D11Device nativeDevice,
    IDirect3DDevice winRtDevice)
```

意味着外部不能随意构造对象，只能通过 `Create()`。这样可以保证对象一旦创建成功，两个设备属性一定都是有效的。

------

## `Create()` 的整体流程

```
创建原生 ID3D11Device
          │
          ▼
查询 IDXGIDevice 接口
          │
          ▼
调用原生桥接函数
CreateDirect3D11DeviceFromDXGIDevice
          │
          ▼
取得 WinRT ABI 裸指针
          │
          ▼
FromAbi 投影为 C# 的 IDirect3DDevice
          │
          ▼
返回同时持有两种设备接口的包装对象
```

### 1. 创建原生 D3D11 设备

```
ID3D11Device nativeDevice =
    D3D11.D3D11CreateDevice(
        DriverType.Hardware,
        DeviceCreationFlags.BgraSupport);
```

`ID3D11Device` 可以理解为程序访问 Direct3D 11 GPU 功能的主要入口。

`DriverType.Hardware` 表示使用真实显卡硬件，而不是软件渲染器。

`DeviceCreationFlags.BgraSupport` 表示设备支持 BGRA 像素格式。Windows Graphics Capture 常用：

```
B8G8R8A8_UNorm
```

所以这个标志是 WGC 兼容性所需的。

如果设备创建失败，`D3D11CreateDevice` 会抛出异常，方法直接结束，此时还没有资源需要手动清理。

------

### 2. 查询设备的 DXGI 接口

```
using IDXGIDevice dxgiDevice =
    nativeDevice.QueryInterface<IDXGIDevice>();
```

Direct3D 和 DXGI 是相互关联的两层：

- Direct3D 负责纹理、GPU 命令等图形操作。
- DXGI 负责显卡设备、交换链和图形资源之间的基础连接。

这里使用 COM 的 `QueryInterface`，询问：

> 这个原生 D3D11 设备是否也能以 `IDXGIDevice` 接口访问？

成功后，`dxgiDevice` 只是一个临时接口，用来执行后面的桥接。

`using` 保证离开作用域时调用 `dxgiDevice.Dispose()`，释放 `QueryInterface` 增加的 COM 引用。它不会把 `nativeDevice` 本身一并销毁，因为两份接口引用分别计算生命周期。

------

### 3. 调用 Windows 原生桥接函数

```
int hresult =
    NativeMethods.CreateDirect3D11DeviceFromDXGIDevice(
        dxgiDevice.NativePointer,
        out IntPtr winRtDevicePointer);
```

这个函数接收原生 `IDXGIDevice` 的 COM 指针，然后创建一个 WinRT 可识别的 Direct3D 设备包装。

输入：

```
dxgiDevice.NativePointer
```

这是 `IDXGIDevice` 对应的原生 COM 指针。

输出：

```
winRtDevicePointer
```

这是 WinRT 对象在 ABI 层的裸指针。ABI 可以理解为托管 C# 和原生 Windows 组件之间约定的二进制调用边界。

此时它还不是方便使用的 C# 对象，只是一个 `IntPtr`。

------

### 4. 检查 HRESULT

```
Marshal.ThrowExceptionForHR(hresult);
```

许多 Windows 原生 API 不直接抛异常，而是返回一个 `HRESULT`：

- 非负值通常表示成功。
- 负值表示失败。

`ThrowExceptionForHR` 会把失败的 HRESULT 转换成对应的 .NET 异常。这样后续代码就可以使用正常的 `try/catch` 处理失败。

------

### 5. 防御性检查空指针

```
if (winRtDevicePointer == IntPtr.Zero)
{
    throw new InvalidOperationException(
        "创建 WinRT Direct3D 设备失败：返回了空指针。");
}
```

理论上，成功的 HRESULT 应该同时返回有效指针。但这里仍然进行检查，避免出现“API 报告成功，输出却不可用”的异常情况。

否则后面的 `FromAbi` 会尝试投影空指针，错误信息会更难理解。

------

### 6. 将 ABI 指针投影为 C# WinRT 对象

```
IDirect3DDevice winRtDevice =
    WinRT.MarshalInterface<IDirect3DDevice>.FromAbi(
        winRtDevicePointer);
```

这里的“投影”是指：

> 把原生 WinRT ABI 指针转换成 C# 可以调用和管理的 `IDirect3DDevice` 对象。

不能简单写成普通 C# 强制类型转换，因为 `IntPtr` 只是一个内存地址。`FromAbi` 知道 C#/WinRT 的封送和 COM 生命周期规则。

项目特意使用这种桥接方式，是因为之前使用 Vortice 泛型转换时产生了旧式 `System.__ComObject`，在创建 frame pool 或释放设备时会触发 `E_NOINTERFACE`。当前做法是本项目已经运行验证过的路径。

------

### 7. 返回统一的资源包装对象

```
return new Direct3D11CaptureDevice(
    nativeDevice,
    winRtDevice);
```

创建成功后，返回的对象同时保存：

```
NativeDevice
WinRtDevice
```

调用方可以这样使用：

```
using Direct3D11CaptureDevice captureDevice =
    Direct3D11CaptureDevice.Create();
```

其中：

- `captureDevice.WinRtDevice` 传给 WGC frame pool。
- `captureDevice.NativeDevice` 用于处理捕获到的原生 D3D11 纹理。

------

## 为什么返回前还要 `Marshal.Release`

```
finally
{
    Marshal.Release(winRtDevicePointer);
}
```

这是这段代码最容易困惑的地方。

原生函数返回的 `winRtDevicePointer` 是一个带有 COM 引用计数的裸指针。调用方接到它后，必须释放这份原始引用。

`FromAbi` 已经创建了由 C#/WinRT 管理的投影对象：

```
winRtDevice
```

所以这里释放的是：

> 原生函数通过 `out` 参数交给当前方法的那一份裸指针引用。

不是立即销毁已经投影出来的 `winRtDevice`。

而且 `finally` 在执行 `return` 时仍然会运行，因此顺序是：

1. 创建返回对象。
2. 执行 `finally`，释放裸指针引用。
3. 真正把包装对象返回给调用方。

------

## 外层 `catch` 为什么释放 `nativeDevice`

```
catch
{
    nativeDevice.Dispose();
    throw;
}
```

`nativeDevice` 是最先创建的资源。如果后面任意一步失败，例如：

- 查询 `IDXGIDevice` 失败；
- 原生桥接函数失败；
- 返回空指针；
- `FromAbi` 投影失败；
- 包装对象构造失败；

方法都无法把资源所有权交给返回对象。因此当前方法必须释放 `nativeDevice`。

`throw;` 会保留原始异常和调用栈，继续把异常交给上层处理。

这里不能写：

```
throw exception;
```

因为那样通常会重置异常调用栈，影响问题定位。

------

## 三层资源保护

这段方法实际上有三层生命周期保护：

| 资源                 | 保护方式       | 释放时机                  |
| -------------------- | -------------- | ------------------------- |
| `nativeDevice`       | 外层 `catch`   | 创建过程失败时            |
| `dxgiDevice`         | `using`        | DXGI 桥接完成或异常退出时 |
| `winRtDevicePointer` | 内层 `finally` | `FromAbi` 完成或失败后    |

创建成功后，`nativeDevice` 和投影后的 `winRtDevice` 被交给返回的 `Direct3D11CaptureDevice` 管理。

------

## `Dispose()` 的作用

```
public void Dispose()
{
    if (_isDisposed)
    {
        return;
    }

    _isDisposed = true;
```

这使重复调用 `Dispose()` 不会重复释放同一资源：

```
device.Dispose();
device.Dispose(); // 第二次直接返回
```

然后释放 WinRT 设备：

```
try
{
    WinRtDevice.Dispose();
}
```

最后释放原生设备：

```
finally
{
    NativeDevice.Dispose();
}
```

使用 `finally` 是为了保证：即使 `WinRtDevice.Dispose()` 意外抛出异常，`NativeDevice` 仍然会得到释放。

释放顺序也是合理的：

1. 先释放依赖底层图形设备的 WinRT 包装。
2. 再释放原生 D3D11 设备。

这里的 `_isDisposed` 不是线程同步机制，因此这个类默认由单一所有者管理，不应让多个线程同时调用 `Dispose()`。

------

## `DllImport` 是什么

```
[DllImport("d3d11.dll", ExactSpelling = true)]
internal static extern int CreateDirect3D11DeviceFromDXGIDevice(
    IntPtr dxgiDevice,
    out IntPtr graphicsDevice);
```

`DllImport` 是 P/Invoke 声明，它让 C# 可以调用原生 DLL 导出的函数。

- `"d3d11.dll"`：函数所在的 Windows 系统 DLL。
- `extern`：方法实现在 C# 程序之外。
- `ExactSpelling = true`：按声明的函数名精确查找，不尝试添加字符集后缀。
- 返回的 `int`：原生 `HRESULT`。
- `IntPtr dxgiDevice`：输入的原生 DXGI COM 指针。
- `out IntPtr graphicsDevice`：输出的 WinRT Direct3D 设备 ABI 指针。

## 一句话总结

`Create()` 做的是一次受控的“设备桥接”：

```
Vortice ID3D11Device
        ↓ QueryInterface
原生 IDXGIDevice
        ↓ CreateDirect3D11DeviceFromDXGIDevice
WinRT ABI 指针
        ↓ FromAbi
C# IDirect3DDevice
```

最终把原生设备和 WinRT 设备包装到同一个可释放对象中，同时确保创建过程任何一步失败都不会泄漏 GPU 或 COM 资源。对应实现位于 [Direct3D11CaptureDevice.cs (line 27)](E:\\VsCodeObject\\HimeBot\\src\\HimeBot.Platform.Windows\\ScreenCapture\\Direct3D11CaptureDevice.cs:27)。