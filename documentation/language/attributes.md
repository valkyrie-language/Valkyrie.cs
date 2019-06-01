# 特性标注（Attributes）

特性标注为声明附加元数据，影响编译行为或运行时行为。

## 语法

特性标注放在声明之前，使用方括号包裹：

```v
[attribute_name]
structure Tag {
    value: bool;
}
```

带参数的特性：

```v
[range(0, 100)]
structure Score {
    value: f32;
}
```

多个特性标注分行显示：

```v
[serializable]
[range(0, 100)]
structure Score {
    value: f32;
}
```

## 内置特性

### serializable

标记结构体可序列化：

```v
[serializable]
structure Position {
    x: f32;
    y: f32;
}
```

### range

限制数值字段或参数的范围：

```v
[range(0, 100)]
structure Score {
    value: f32;
}
```

### pure

标记函数为纯函数（无副作用，相同输入始终产生相同输出）。标注 `pure` 的函数可被编译器优化（DCE、常量折叠、公共子表达式消除）：

```v
[clr("System.Math", "Sin"), pure]
micro clr_math_sin(x: f64): f64
```

`pure` 可与任何 FFI 注解叠加使用。

## FFI 注解体系

Valkyrie 的 FFI 注解按**调用约定语义**精确分类，抵制模糊的 `[native]`。每个注解必须精确表达目标平台的调用机制。

> **设计原则**：模糊语义是 Valkyrie 所抵制的。`[native]` 不区分 C 调用约定、COM vtable、系统调用、CLR 方法、JVM 方法等，因此被废弃。每个 FFI 注解必须精确表达调用约定。

### 外部调用注解

按目标平台和调用约定分类：

| 注解 | 语义 | 适用平台 | 示例 |
|:---|:---|:---|:---|
| `[c("lib", "func")]` | C 调用约定（cdecl/stdcall），动态链接库函数 | Windows / Linux / macOS | `[c("libc", "write")]` |
| `[com("Interface", "Method")]` | COM vtable 调用 | Windows | `[com("IUnknown", "Release")]` |
| `[syscall(number)]` | 直接系统调用（绕过 libc/Win32） | Windows / Linux / macOS | `[syscall(1)]` |
| `[clr("Type", "Method")]` | .NET CLR 静态方法/属性 | CLR | `[clr("System.Console", "WriteLine")]` |
| `[dlr]` | DLR 动态调用（运行时解析成员） | CLR | `[dlr]` |
| `[jvm("class", "method")]` | JVM 类方法/字段（invokestatic/getstatic） | JVM | `[jvm("java/lang/Math", "sin")]` |
| `[js_builtin("path")]` | JS 内置全局对象方法 | Web | `[js_builtin("console.log")]` |
| `[wasi]` | WASI 系统接口 | Web (WASI) | `[wasi]` |

#### `[c("lib", "func")]` — C 调用约定

绑定 C 动态链接库导出函数。第一个参数为库名，第二个为函数名。

```v
# Linux/macOS libc
[c("libc", "write")]
micro posix_write(fd: i32, buf: c_str, len: i32): i32

# Windows DLL
[c("user32", "MessageBoxW")]
micro win_message_box(title: utf16, text: utf16): i32

# macOS Framework
[c("Foundation", "NSLog")]
micro darwin_ns_log(format: utf8): i32
```

字符串编码通过参数类型标注：`c_str`（C 字符串，UTF-8）、`utf16`（UTF-16）、`utf8`（显式 UTF-8）。

#### `[com("Interface", "Method")]` — COM vtable 调用

绑定 Windows COM 接口方法，通过 vtable 偏移调用。第一个参数为接口名，第二个为方法名。

```v
[com("IUnknown", "QueryInterface")]
micro com_query_interface(this: i32, riid: i32, out: i32): i32

[com("IUnknown", "AddRef")]
micro com_add_ref(this: i32): i32

[com("IUnknown", "Release")]
micro com_release(this: i32): i32

[com("IDispatch", "Invoke")]
micro com_invoke(this: i32, id: i32, riid: i32, lcid: i32, flags: i16, params: i32, result: i32, excep: i32, arg_err: i32): i32
```

COM 初始化/工厂函数仍使用 `[c]`（如 `CoCreateInstance`、`CoInitializeEx`），因为它们是普通 DLL 导出函数。

#### `[syscall(number)]` — 直接系统调用

绕过 libc/Win32 子系统，直接发起内核系统调用。参数为系统调用号。

```v
# Linux x86_64
[syscall(1)]
micro sys_write(fd: i32, buf: c_str, len: i32): i64

# Windows NT
[syscall("NtClose")]
micro nt_close(handle: i32): i32

# Darwin/Mach（负数表示 Mach trap）
[syscall(-26)]
micro sys_darwin_mach_reply_port(): i32
```

- Linux：使用 x86_64 syscall 编号（整数）
- Windows：使用 NT 函数名（字符串，编译器映射为 syscall 编号）
- macOS：正数为 BSD syscall，负数为 Mach trap

#### `[clr("Type", "Method")]` — .NET CLR 方法

绑定 .NET CLR 静态方法或属性。第一个参数为完整类型名，第二个为方法/属性名。

```v
[clr("System.Console", "WriteLine")]
micro clr_console_write_line(value: string)

[clr("System.Math", "Sin"), pure]
micro clr_math_sin(x: f64): f64

[clr("System.Environment", "get_CurrentDirectory")]
micro clr_get_cwd(): string
```

属性 getter 使用 `get_` 前缀，setter 使用 `set_` 前缀（遵循 CLR 内部命名）。

#### `[dlr]` — DLR 动态调用

绑定 .NET DLR（动态语言运行时）调用，运行时解析成员。

```v
[dlr]
micro dlr_new(type_name: string): i32

[dlr]
micro dlr_get(obj: i32, member: string): i32

[dlr]
micro dlr_invoke(obj: i32, method: string): i32

[dlr]
micro dlr_cast(obj: i32, type_name: string): i32
```

#### `[jvm("class", "method")]` — JVM 方法

绑定 JVM 类方法或字段。第一个参数为内部类名（`/` 分隔），第二个为方法/字段名。

```v
[jvm("java/lang/System", "currentTimeMillis")]
micro jvm_current_time_millis(): i64

[jvm("java/lang/Math", "sin"), pure]
micro jvm_math_sin(x: f64): f64

[jvm("java/lang/String", "length"), pure]
micro jvm_string_length(handle: i32): i32

[jvm("java/lang/StringBuilder", "<init>")]
micro jvm_sb_new(): i32
```

构造函数使用 `<init>`，类初始化器使用 `<clinit>`。

#### `[js_builtin("path")]` — JS 内置对象

直接映射 JS 内置全局对象方法。参数为 JS 属性访问路径。

```v
[js_builtin("console.log")]
micro console_log(msg: string)

[js_builtin("Math.sin"), pure]
micro math_sin(x: f64): f64

[js_builtin("document.getElementById")]
micro dom_get_by_id(id: string): i32
```

#### `[wasi]` — WASI 系统接口

绑定 WASI（WebAssembly System Interface）系统调用。

```v
[wasi]
micro wasi_fd_write(fd: i32, iovs: i32, iovs_len: i32, nwritten: i32): i32

[wasi]
micro wasi_clock_time_get(clock_id: i32, precision: i64, time: i32): i32
```

### 库链接注解

| 注解 | 语义 | 适用平台 | 示例 |
|:---|:---|:---|:---|
| `[import("module")]` | 动态导入 `.nyar` 库 | NyarVM | `[import("std.math")]` |
| `[export]` | 导出为 `.nyar` 库符号 | NyarVM | `[export]` |

#### `[import("module")]` — 动态导入 .nyar 库

在运行时动态加载 `.nyar` 模块，获取其导出符号。参数为模块名。

```v
[import("std.math")]
micro math_module: i32

[import("game.physics")]
micro physics_module: i32
```

#### `[export]` — 导出为 .nyar 库符号

标记函数或声明为公开导出，编译到 `.nyar` 库时其他模块可通过 `[import]` 引用。

```v
[export]
micro greet(name: string): string {
    return "Hello, " + name
}

[export]
micro calculate(x: f64, y: f64): f64 {
    return x + y
}
```

## 自定义特性

Valkyrie 支持自定义特性标注，用于 Plugin 系统和编译期宏：

```v
[my_custom_attribute]
micro myFunction() {
    # ...
}
```

自定义特性的语义由 Plugin 和编译器扩展定义。

## 与 Gnosis 的关系

特性标注在 AST→IKun 转换时映射为 IKun 元数据节点，最终影响：

- **编译期**：宏展开、条件编译
- **运行期**：序列化、反射、ECS 查询优化
