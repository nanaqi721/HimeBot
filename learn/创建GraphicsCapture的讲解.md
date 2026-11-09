这个方法的作用可以概括成一句话：

> 把游戏窗口的原生句柄 `HWND`，转换成 Windows Graphics Capture 能识别的 `GraphicsCaptureItem`。

它还没有真正截图，只是告诉 WGC：“以后要捕获的是这个窗口”。

## 一、整体执行流程

```
windowHandle
    │
    ├─ 1. 检查句柄是否有效
    │
    ├─ 2. 取得 Windows 的截图对象工厂
    │
    ├─ 3. 调用 COM 接口 CreateForWindow
    │
    ├─ 4. Windows 返回 HRESULT 和原生对象指针
    │
    ├─ 5. 检查 HRESULT
    │
    ├─ 6. 把原生指针包装成 GraphicsCaptureItem
    │
    └─ 7. 释放本方法持有的原生引用并返回对象
```

后面的捕获流程才会使用这个对象：

```
GraphicsCaptureItem
    ↓
创建 Direct3D11CaptureFramePool
    ↓
创建 GraphicsCaptureSession
    ↓
StartCapture()
    ↓
收到第一帧
```

## 二、类和字段

```
internal static class GraphicsCaptureItemFactory
```

### `internal`

表示这个类只能被当前项目，也就是 `HimeBot.Platform.Windows` 程序集内部使用。

`HimeBot.App` 或其他项目不能直接访问它。这样可以把复杂的 Windows COM 细节隐藏在平台模块内部。

### `static class`

静态类不能通过 `new` 创建实例：

```
// 不允许
var factory = new GraphicsCaptureItemFactory();
```

只能直接通过类名调用：

```
GraphicsCaptureItemFactory.CreateForWindow(windowHandle);
```

因为工厂没有需要保存的状态，所以使用静态类比较合适。

### 输出类型的 GUID

```
private static readonly Guid GraphicsCaptureItemInterfaceId =
    new("79C3F95B-31F7-4EC2-A464-632EF5D30760");
```

这里涉及三个语法。

`private` 表示只有当前类内部可以使用。

`static readonly` 表示整个程序中只有一份，并且初始化后不能再重新赋值。

下面是目标类型推断语法：

```
Guid value = new("...");
```

它等价于：

```
Guid value =
    new Guid("...");
```

因为变量左侧已经写了 `Guid`，右侧可以省略一次类型名称。

这个 GUID 表示：

```
Windows.Graphics.Capture.IGraphicsCaptureItem
```

也就是告诉 Windows：

> 请创建并返回一个实现了 `IGraphicsCaptureItem` 的对象。

## 三、方法声明

```
internal static GraphicsCaptureItem CreateForWindow(
    IntPtr windowHandle)
```

逐项看：

- `internal`：仅当前项目可调用。
- `static`：通过类名直接调用。
- `GraphicsCaptureItem`：方法的返回类型。
- `CreateForWindow`：方法名称。
- `IntPtr windowHandle`：接收窗口句柄。

### `IntPtr` 是什么

`IntPtr` 用来保存原生指针或 Windows 句柄。

在 C++ 的 Win32 API 中，窗口句柄通常写作：

```
HWND
```

到了 C# 中一般对应：

```
IntPtr
```

它不是窗口对象本身，而是 Windows 给这个窗口分配的一个标识值。

## 四、检查窗口句柄

```
if (windowHandle == IntPtr.Zero)
{
    throw new ArgumentException(
        "窗口句柄不能为空。",
        nameof(windowHandle));
}
```

`IntPtr.Zero` 相当于原生代码里的空指针：

```
nullptr
```

如果句柄为零，说明没有指向任何有效窗口。

### `throw`

```
throw new ArgumentException(...);
```

表示方法无法继续执行，并抛出一个异常。

这里使用 `ArgumentException`，因为错误来自调用者传入的参数。

### `nameof`

```
nameof(windowHandle)
```

编译后的结果就是字符串：

```
"windowHandle"
```

但它比直接写字符串更安全。如果以后重命名了参数，IDE 也会同步修改 `nameof` 中的名称。

## 五、取得 Windows 的 COM 工厂

```
IGraphicsCaptureItemInterop interop =
    GraphicsCaptureItem.As<IGraphicsCaptureItemInterop>();
```

这是最不直观的一行。

`GraphicsCaptureItem` 是一个 WinRT 类型，但根据窗口句柄创建它的能力没有直接暴露成普通 C# 方法。这个能力位于一个桌面互操作 COM 接口中：

```
IGraphicsCaptureItemInterop
```

`As<T>()` 是泛型方法：

```
As<IGraphicsCaptureItemInterop>()
```

尖括号中的类型表示：

> 请从 `GraphicsCaptureItem` 的激活工厂中，取得 `IGraphicsCaptureItemInterop` 接口。

这里不是把一个已经创建好的 `GraphicsCaptureItem` 强制转换成另一个类型，而是在取得负责创建截图对象的 Windows 工厂接口。

可以把它想象成：

```
Windows 的 GraphicsCaptureItem 工厂
                ↓ 查询接口
IGraphicsCaptureItemInterop
```

## 六、调用 Windows 原生方法

```
int hresult = interop.CreateForWindow(
    windowHandle,
    GraphicsCaptureItemInterfaceId,
    out IntPtr itemPointer);
```

这里把三个信息传给 Windows：

1. `windowHandle`：要捕获哪个窗口。
2. `GraphicsCaptureItemInterfaceId`：希望返回什么类型。
3. `itemPointer`：Windows 把创建结果写到哪里。

### `out` 参数

```
out IntPtr itemPointer
```

`out` 表示这个变量的值由被调用的方法写入。

这行代码同时完成了变量声明：

```
out IntPtr itemPointer
```

等价于先声明，再调用：

```
IntPtr itemPointer;

int hresult = interop.CreateForWindow(
    windowHandle,
    GraphicsCaptureItemInterfaceId,
    out itemPointer);
```

调用结束后，通常会得到两个结果：

```
hresult     → 操作成功还是失败
itemPointer → 创建出来的 GraphicsCaptureItem 原生指针
```

## 七、检查 `HRESULT`

```
Marshal.ThrowExceptionForHR(hresult);
```

Windows COM 方法通常不直接抛出 C# 异常，而是返回一个叫作 `HRESULT` 的整数。

大致规则是：

```
HRESULT >= 0    成功
HRESULT < 0     失败
```

`Marshal.ThrowExceptionForHR` 会检查这个整数：

- 成功：继续向下执行。
- 失败：转换成对应的 C# 异常并抛出。

例如窗口无效、系统不支持或者无法访问目标窗口时，可能抛出 `COMException`。

## 八、防御性检查指针

```
if (itemPointer == IntPtr.Zero)
{
    throw new InvalidOperationException(
        "Windows 没有返回有效的截图对象。");
}
```

正常情况下，如果 `HRESULT` 表示成功，Windows 应该返回非零指针。

这里仍然检查一次，是为了防止出现“调用看起来成功，但没有返回对象”的异常状态。

这时使用 `InvalidOperationException`，因为传入参数可能没有问题，是 Windows 返回的运行状态不符合预期。

## 九、把原生指针转换成 C# 对象

```
return GraphicsCaptureItem.FromAbi(itemPointer);
```

`itemPointer` 仍然只是一个原生地址，不能直接这样使用：

```
itemPointer.Size
```

`FromAbi` 会把这个原生 WinRT 指针包装成 C# 对象：

```
IntPtr
  ↓ FromAbi
GraphicsCaptureItem
```

### ABI 是什么

ABI 是 Application Binary Interface，中文可理解为“应用程序二进制接口”。

在这里，它描述了 C# 与 Windows 原生组件在二进制层面如何传递：

- 指针
- 参数
- 返回值
- 接口方法
- 对象生命周期

`FromAbi` 的作用就是把 ABI 层面的原生对象转换成 C# 可以操作的对象。

转换后就可以访问：

```
captureItem.Size
captureItem.DisplayName
captureItem.Closed
```

## 十、为什么需要 `try/finally`

```
try
{
    return GraphicsCaptureItem.FromAbi(itemPointer);
}
finally
{
    Marshal.Release(itemPointer);
}
```

### `finally`

无论 `try` 中：

- 正常返回；
- 抛出异常；
- 中途失败；

`finally` 中的代码都会执行。

即使 `try` 里有 `return`，执行顺序依然是：

```
计算返回值
    ↓
执行 finally
    ↓
真正返回
```

### `Marshal.Release`

COM 对象使用“引用计数”管理生命周期。

Windows 返回 `itemPointer` 时，本方法持有了一次原生引用。`FromAbi` 创建托管包装对象后，需要释放当前方法直接持有的原生指针引用：

```
Marshal.Release(itemPointer);
```

返回的 `GraphicsCaptureItem` 仍然有效，因为托管包装对象会维护它自己需要的引用。

这里的原则是：

```
本方法取得的原生引用
    ↓
本方法负责释放
```

否则每次截图都可能遗留一份原生资源，形成资源泄漏。

## 十一、COM 接口声明

```
[ComImport]
[Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
private interface IGraphicsCaptureItemInterop
```

这不是我们要自己实现的普通 C# 接口。

它是在告诉 .NET：

> Windows 中已经存在这样一个 COM 接口，请按照以下形式调用它。

### `[ComImport]`

表示接口的真正实现来自外部 COM 组件，也就是 Windows。

因此我们不会写：

```
class Something : IGraphicsCaptureItemInterop
```

### `[Guid(...)]`

```
[Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
```

这是 `IGraphicsCaptureItemInterop` 接口本身的身份标识。

代码中有两个不同的 GUID：

| GUID           | 表示什么                                        |
| -------------- | ----------------------------------------------- |
| `3628E81B-...` | 用来找到 `IGraphicsCaptureItemInterop` 工厂接口 |
| `79C3F95B-...` | 要求工厂返回 `IGraphicsCaptureItem` 对象        |

可以理解为：

```
找到哪个工厂接口？
3628E81B-...

要求工厂生产什么对象？
79C3F95B-...
```

### `[InterfaceType]`

```
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
```

表示这个 COM 接口使用 `IUnknown` 形式。

`IUnknown` 是 COM 接口最基础的协议，负责：

- 查询接口；
- 增加引用计数；
- 减少引用计数。

## 十二、原生方法声明

```
[PreserveSig]
int CreateForWindow(
    IntPtr windowHandle,
    in Guid interfaceId,
    out IntPtr result);
```

它对应 Windows 原生方法的大致形式：

```
HRESULT CreateForWindow(
    HWND window,
    REFIID interfaceId,
    void** result);
```

对应关系如下：

| C++       | C#           |
| --------- | ------------ |
| `HRESULT` | `int`        |
| `HWND`    | `IntPtr`     |
| `REFIID`  | `in Guid`    |
| `void**`  | `out IntPtr` |

### `[PreserveSig]`

COM 默认可能自动把失败的 `HRESULT` 转成异常，并改变方法在 C# 中的返回形式。

`[PreserveSig]` 表示：

> 保留原生方法签名，不要自动转换。

所以我们能自己取得：

```
int hresult
```

然后明确执行：

```
Marshal.ThrowExceptionForHR(hresult);
```

这使原生调用过程更容易理解和诊断。

### `in Guid`

```
in Guid interfaceId
```

`in` 表示：

- 参数按引用传递；
- 被调用方法只能读取；
- 不能修改它。

因为原生接口需要的是 `REFIID`，也就是一个只读 GUID 引用。

### `out IntPtr`

```
out IntPtr result
```

表示 Windows 通过这个参数把新对象的原生指针返回给调用者。

## 十三、这个方法不负责什么

当前方法只负责：

```
HWND → GraphicsCaptureItem
```

它不负责：

- 创建 Direct3D 设备；
- 创建帧池；
- 启动截图会话；
- 等待第一帧；
- 转换像素；
- 保存 PNG。

这就是把它单独放进 `GraphicsCaptureItemFactory` 的原因：对外只有一个简单操作，复杂的 COM、GUID、指针和引用计数都隐藏在内部。

还有一个后续需要注意的地方：`CreateForWindow` 失败时可能抛出 `COMException`。接入现有 `WindowsGameScreenshotCapture` 时，我们会在现有的 `ExternalException` 之前单独处理它，避免把 WGC 创建失败错误显示成“PNG 编码失败”。