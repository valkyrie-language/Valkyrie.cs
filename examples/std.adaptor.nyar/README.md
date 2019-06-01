# ⚡ std.adaptor.nyar

Valkyrie NyarVM 平台 SDK — 提供 NyarVM 指令集绑定，编译为 `.nyar` 字节码。

## 目标三元组

| 目标三元组 | 架构 | 说明 |
|:---|:---|:---|
| `nyar-unknown-unknown` | nyar32 | NyarVM 通用运行时 |

## 包内容

```
std.adaptor.nyar/
├── legion.von          # 包清单
└── source/
    ├── arith.v         # 算术运算 [vm] 绑定
    ├── builtin.v       # VM 内置函数 [vm] 绑定
    ├── cmp.v           # 比较运算 [vm] 绑定
    ├── control.v       # 控制流 [vm] 绑定
    ├── conv.v          # 类型转换 [vm] 绑定
    ├── memory.v        # 内存操作 [vm] 绑定
    ├── object.v        # 对象操作 [vm] 绑定
    └── string.v        # 字符串操作 [vm] 绑定
```

## 绑定概览

| 文件 | 绑定类型 | 说明 |
|:---|:---|:---|
| `arith.v` | `[vm("i32_add/...")]` | i32/i64/f64 算术与位运算 opcode 绑定 |
| `builtin.v` | `[vm("println/...")]` | NyarVM 内置函数绑定 |
| `cmp.v` | `[vm("i32_eq/...")]` | i32 比较 opcode 绑定 |
| `control.v` | `[vm("nop")]` | 控制流 opcode 绑定 |
| `conv.v` | `[vm("i32_to_...")]` | 类型转换 opcode 绑定 |
| `memory.v` | `[vm("alloc/...")]` | 内存管理 opcode 绑定 |
| `object.v` | `[vm("obj_.../closure_...")]` | 对象与闭包 opcode 绑定 |
| `string.v` | `[vm("str_...")]` | 字符串操作 opcode 绑定 |

## 与其他适配器的关系

`std.adaptor.nyar` 是 NyarVM 平台的适配层，与其他平台适配器并列：

| 适配器 | 平台 | 绑定属性 |
|:---|:---|:---|
| `std.adaptor.nyar` | NyarVM | `[vm]` |
| `std.adaptor.wasm` | WebAssembly | `[js_builtin]` |
| `std.adaptor.dotnet` | .NET CLR | `[dotnet_builtin]` |
| `std.adaptor.jvm` | JVM | `[jvm_builtin]` |
| ... | ... | ... |

`valkyrie-core` 提供与运行时无关的纯 GGScript 实现和抽象声明，
各平台适配器提供对应运行时的具体绑定。

## 编译命令

```bash
# 编译为 NyarVM 字节码
vcc build --target nyar

# 编译为 NyarVM 并运行
vcc run --target nyar
```

## 状态

✅ NyarVM 指令集绑定已覆盖 8 个模块，包含 50+ opcode 绑定。