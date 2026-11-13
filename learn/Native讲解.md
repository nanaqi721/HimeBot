# NativeMethod讲解

# [StructLayout(LayoutKind.Sequential)]讲解

<span style="color:red;">**这个声明的结构体是什么，为什么要声明**</span>

```
<span style="color:red;">这是红色字体</span>
```

```csharp
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

```

这是一个“给 Windows API 使用的鼠标输入数据包”。它对应 Win32 中的原生 `MOUSEINPUT` 结构体。

它本身不会移动鼠标；HimeBot 填好这张“表格”后，再把它交给 Windows 的 `SendInput` 函数，Windows 才会执行鼠标移动或点击。

项目中的调用关系是：

```
NativeMouseInput
    ↓ 放入
NativeInput
    ↓ 传给
SendInput(...)
    ↓
Windows 执行鼠标操作
```

### 为什么需要自己声明？

`SendInput` 是 Windows 提供的原生函数，不是 .NET 方法。Windows 只认识固定格式的内存数据，而 C# 不会自动知道 `MOUSEINPUT` 的格式，因此要在 C# 中声明一个具有相同字段顺序和数据类型的结构体。

```
[StructLayout(LayoutKind.Sequential)]
```

这表示：

> 按照代码中声明字段的先后顺序，在内存中排列这些字段。

这对 P/Invoke（C# 调用原生 DLL 的机制）非常重要。字段顺序或类型不匹配时，Windows 就可能读到错误的数据。

字段名称其实可以不同，真正必须匹配的是字段顺序、类型和内存大小。

### 每个字段表示什么？

```
public int X;
public int Y;
```

鼠标坐标或移动量。具体含义取决于 `Flags`：

- 普通移动时，可以表示相对移动量。
- 设置 `MouseEventAbsolute` 时，表示经过归一化的绝对坐标，范围是 `0～65535`。

HimeBot 移动鼠标时使用的是绝对坐标：

```
Flags =
    MouseEventMove |
    MouseEventAbsolute |
    MouseEventVirtualDesk;
```

------

```
public uint MouseData;
```

额外的鼠标数据，主要用于：

- 鼠标滚轮滚动量；
- X1、X2 侧键。

当前的鼠标移动和左键点击不需要它，所以设置为 `0`。

------

```
public uint Flags;
```

说明这次输入到底是什么操作。它是一个位标志组合，例如：

```
MouseEventMove       // 移动
MouseEventAbsolute   // 使用绝对坐标
MouseEventVirtualDesk// 坐标基于整个虚拟桌面
MouseEventLeftDown   // 左键按下
MouseEventLeftUp     // 左键抬起
```

所谓“位标志”，就是多个选项可以使用 `|` 组合在一个整数中。

------

```
public uint Time;
```

事件时间戳。设置为 `0` 表示让 Windows 自动填写时间。

------

```
public UIntPtr ExtraInfo;
```

调用者附加的额外标记。例如，程序可以用它识别某个输入是否由自己生成。

`UIntPtr` 的大小会跟随运行平台：

- 32 位进程中是 4 字节；
- 64 位进程中是 8 字节。

这正好对应 Windows 原生结构中的指针大小。HimeBot 当前不需要额外标记，所以使用：

```
ExtraInfo = UIntPtr.Zero
```

### 为什么使用 `struct` 而不是 `class`？

C# 的 `struct` 是值类型，更适合表示这种：

- 字段固定；
- 内存布局固定；
- 只负责传递数据；
- 需要嵌入另一个原生结构的数据块。

在当前代码中，鼠标输入会被放进 `NativeInput`，然后以数组形式传给 `SendInput`：

```
NativeMethods.SendInput(
    (uint)inputs.Length,
    inputs,
    Marshal.SizeOf<NativeInput>());
```

例如，左键点击实际上提交两个数据包：

```
MouseEventLeftDown
        ↓
MouseEventLeftUp
```

Windows 依次接收到“按下”和“抬起”，才构成一次完整点击。

因此，可以把 `NativeMouseInput` 理解为：

> C# 为了和 Windows `SendInput` 通信，按照 Windows 规定格式制作的一张鼠标操作表单。



# [StructLayout(LayoutKind.Explicit)]讲解

```csharp
[StructLayout(LayoutKind.Explicit)]
internal struct NativeInputUnion
{
    [FieldOffset(0)]
    public NativeMouseInput Mouse;
}
```

<span style="color:red;">这是在声明什么</span>



它是在 C# 中模拟 C/C++ 的 `union`（联合体）。

Windows 的 `INPUT` 原生结构大致是：

```
struct INPUT
{
    DWORD type;

    union
    {
        MOUSEINPUT    mi;
        KEYBDINPUT    ki;
        HARDWAREINPUT hi;
    };
};
```

同一个 `INPUT` 可以表示：

- 鼠标输入；
- 键盘输入；
- 其他硬件输入。

但一次只能使用其中一种。

### `LayoutKind.Explicit` 是什么？

```
[StructLayout(LayoutKind.Explicit)]
```

表示：

> 不要由 .NET 自动排列字段，每个字段放在什么内存位置，由 `[FieldOffset]` 明确指定。

这里：

```
[FieldOffset(0)]
public NativeMouseInput Mouse;
```

表示 `Mouse` 从这个结构体的第 `0` 个字节开始存放。

目前只有一个字段，所以看起来有点多余。完整版本可能是：

```
[StructLayout(LayoutKind.Explicit)]
internal struct NativeInputUnion
{
    [FieldOffset(0)]
    public NativeMouseInput Mouse;

    [FieldOffset(0)]
    public NativeKeyboardInput Keyboard;

    [FieldOffset(0)]
    public NativeHardwareInput Hardware;
}
```

三个字段都从偏移量 `0` 开始，所以会共享同一块内存：

```
同一块内存
┌─────────────────────────────┐
│ Mouse                        │
│ Keyboard  与 Mouse 重叠      │
│ Hardware  与 Mouse 重叠      │
└─────────────────────────────┘
```

这就是联合体的特点：多个字段共享同一块内存，而不是依次排列。

### Windows 怎么知道当前是哪一种？

外层的 `NativeInput` 还有一个 `Type`：

```
[StructLayout(LayoutKind.Sequential)]
internal struct NativeInput
{
    public uint Type;
    public NativeInputUnion Data;
}
```

创建鼠标输入时：

```
var input = new NativeInput
{
    Type = NativeMethods.InputMouse,
    Data = new NativeInputUnion
    {
        Mouse = new NativeMouseInput
        {
            // 鼠标数据
        }
    }
};
```

其中：

```
Type = NativeMethods.InputMouse
```

相当于告诉 Windows：

> `Data` 这块共享内存目前应当按照鼠标输入来解释。

整体结构可以理解为：

```
NativeInput
├── Type = InputMouse       告诉 Windows 数据类型
└── Data                    联合体，共享数据区域
    └── Mouse               当前按鼠标数据解释
```

### 为什么现在只有 `Mouse`？

因为 HimeBot 当前只需要鼠标输入，还没有模拟键盘，所以只声明了：

```
public NativeMouseInput Mouse;
```

即使目前只有一个成员，保留 `NativeInputUnion` 仍然能够准确反映 Windows 原生 `INPUT` 的结构层次。如果以后需要键盘输入，可以继续增加 `Keyboard` 字段，并同样使用 `[FieldOffset(0)]`。

所以这段代码的完整含义是：

> 声明一块可以容纳不同输入类型的共享内存区域；目前只支持把它作为鼠标输入读取。



# p/invoke讲解

## [DllImport("user32.dll")]

```csharp
[DllImport("user32.dll")]
```

表示这个方法的真正实现不在 C# 中，而在 Windows 的 `user32.dll` 里。

`user32.dll` 提供大量窗口、鼠标、键盘相关的 Win32 API，例如：

- `GetClientRect`
- `ClientToScreen`
- `SendInput`

默认情况下，.NET 会按照 C# 方法名寻找同名原生函数，所以这里会寻找

## SetLastError = true

```csharp
SetLastError = true
```

表示调用结束后，让 .NET 保存 Windows 原生函数产生的错误码。

如果调用失败，可以读取它：

```
bool success = GetClientRect(
    windowHandle,
    out NativeRect rectangle);

if (!success)
{
    int errorCode = Marshal.GetLastWin32Error();
}
```

这个错误码可以进一步转换成可读的异常：

```
throw new Win32Exception(
    Marshal.GetLastWin32Error());
```

如果没有设置 `SetLastError = true`，之后读取的错误码可能已经被其他原生调用改变，不能保证属于这次 `GetClientRect` 调用。



## [return: MarshalAs(UnmanagedType.Bool)]

```
[return: MarshalAs(UnmanagedType.Bool)]
```

它描述的是返回值如何转换。

注意这里的 `return:`，意思是该特性应用于“方法返回值”，不是方法本身。

Windows 返回的是：

```
BOOL
```

Win32 的 `BOOL` 实际上是一个4字节整数：

- `0`：失败；
- 非 `0`：成功。

而 C# 希望得到：

```
bool
```

所以这段声明告诉封送器：

> 按照 Win32 的4字节 `BOOL` 读取返回值，然后转换为 C# 的 `bool`。

“封送”（marshalling）就是在 C# 数据和原生内存数据之间进行转换。



## [DllImport("dwmapi.dll",PreserveSig = true)]
`dwmapi.dll` 是 Windows 的 Desktop Window Manager API 动态链接库，中文通常称为“桌面窗口管理器 API”。

其中：

```
DWM = Desktop Window Manager
API = Application Programming Interface
DLL = Dynamic Link Library
```

DWM 负责 Windows 桌面的视觉合成，例如：

- 把各个窗口的画面合成为最终桌面；
- 窗口边框、阴影和动画；
- 窗口缩略图；
- 窗口的实际可见边界；
- 一些窗口显示属性。

HimeBot 使用它，主要不是为了特效，而是为了获得窗口真正的可见边界。

### `DwmGetWindowAttribute` 是什么？

这个函数用于读取某个窗口的 DWM 属性。

Windows 原始声明大致是：

```
HRESULT DwmGetWindowAttribute(
    HWND  hwnd,
    DWORD dwAttribute,
    PVOID pvAttribute,
    DWORD cbAttribute
);
```

HimeBot 将其声明为：

```
[DllImport(
    "dwmapi.dll",
    PreserveSig = true)]
internal static extern int DwmGetWindowAttribute(
    IntPtr windowHandle,
    int attribute,
    out NativeRect attributeValue,
    int attributeSize);
```

当前项目主要使用的属性是：

```
internal const int DwmwaExtendedFrameBounds = 9;
```

`9` 对应 Windows 的：

```
DWMWA_EXTENDED_FRAME_BOUNDS
```

它表示：

> 获取窗口在屏幕上的扩展边框范围，也就是 DWM 认为的可见窗口边界。

### 每个参数的含义

```
IntPtr windowHandle
```

要查询哪个窗口，对应 Win32 的 `HWND`。

------

```
int attribute
```

想读取哪个属性。

当前会传入：

```
NativeMethods.DwmwaExtendedFrameBounds
```

也就是数值 `9`。

------

```
out NativeRect attributeValue
```

由 DWM 把查询结果写进这里。

因为当前查询的是窗口边界，所以结果类型是：

```
NativeRect
```

例如：

```
Left   = 500
Top    = 200
Right  = 1260
Bottom = 660
```

这里是屏幕坐标，不是从 `(0, 0)` 开始的客户区坐标。

需要注意：原生函数的第三个参数是通用的 `PVOID`，不同属性可能返回不同类型。HimeBot 这个声明将它专门化成了 `NativeRect`，所以适合 `DWMWA_EXTENDED_FRAME_BOUNDS`，不能随意拿来读取返回布尔值或整数的其他 DWM 属性。

------

```
int attributeSize
```

告诉 Windows 输出空间有多大。

通常这样传入：

```
Marshal.SizeOf<NativeRect>()
```

也就是：

> 我准备的结果区域大小等于一个 `NativeRect`。

Windows 可以借此确认调用方提供的结构体大小是否正确。

### `PreserveSig = true` 是什么？

`DwmGetWindowAttribute` 返回的不是普通布尔值，而是 `HRESULT`。

```
HRESULT
```

是一种 Windows 常用的状态码：

- `0`，即 `S_OK`：调用成功；
- 其他值：可能表示失败原因。

C# 中使用：

```
PreserveSig = true
```

表示：

> 保留原生函数的返回形式，把 `HRESULT` 原样作为 `int` 返回，不自动转换成异常。

因此可以这样检查：

```
int result = NativeMethods.DwmGetWindowAttribute(
    windowHandle,
    NativeMethods.DwmwaExtendedFrameBounds,
    out NativeRect windowBounds,
    Marshal.SizeOf<NativeRect>());

if (result != 0)
{
    // 查询失败
}
```

`PreserveSig` 的意思就是“保留原始签名”。对于 `DllImport`，它本来通常就是 `true`，这里明确写出来，是为了让读代码的人知道：项目希望自己检查 `HRESULT`。

### 为什么截图时需要 DWM 边界？

`GetClientRect` 只能告诉我们客户区有多大：

```
客户区：(0, 0) 到 (746, 420)
```

`ClientToScreen` 可以告诉我们客户区左上角位于屏幕哪里：

```
客户区屏幕原点：(508, 231)
```

而 `DwmGetWindowAttribute` 可以告诉我们完整可见窗口帧从哪里开始：

```
可见窗口原点：(500, 200)
```

两者相减：

```
裁剪起点 X = 508 - 500 = 8
裁剪起点 Y = 231 - 200 = 31
```

也就是说，在完整窗口截图中：

- 从左边裁掉 8 像素边框；
- 从上边裁掉 31 像素标题栏和边框；
- 再按照 `746 × 420` 截取客户区。

因此，在 HimeBot 中，这个 API 的主要用途是：

> 动态计算游戏客户区在 WGC 完整窗口帧中的位置，从截图中排除标题栏和边框，而不是硬编码边框像素

