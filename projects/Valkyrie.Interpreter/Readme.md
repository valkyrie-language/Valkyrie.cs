# Valkyrie.Runtime

`Valkyrie.Runtime` 的新定位是运行时层，而不是新的编译主线。

它负责封装 `NyarVM`，加载并执行 `Nyar Standard IR` / `.nyar` 模块；新的 `AST -> HIR -> MIR -> LIR -> multi target` 编译职责迁入 `Valkyrie.Compiler`。

## 项目定位

| 项目 | 职责 |
|:---|:---|
| `Valkyrie.Compiler` | `AST -> HIR -> MIR -> LIR -> multi target` |
| `Valkyrie.Runtime` | `Nyar Standard IR` / `.nyar` 的装载、运行和宿主内建 |

## 运行时结构

```text
Valkyrie.Runtime/
├── ValkyrieRuntime.cs        # 迁移期兼容门面
├── Runtime/                  # 执行门面与会话管理
├── Loader/                   # `CgModule` / `.nyar` 装载与校验
├── Host/                     # 运行时内建与宿主对象桥接
└── Valkyrie.Runtime.csproj
```

## 运行时职责

| 能力 | 说明 |
|:---|:---|
| 模块装载 | 接收 `CgModule`、`NyarModule` 或 `.nyar` 字节码 |
| 模块校验 | 在装载前执行最小运行时约束验证 |
| 函数调用 | 通过 `module + function + args` 执行逻辑入口或显式导出 |
| 宿主内建 | 提供标准运行时 intrinsic 与宿主对象桥接 |
| 会话管理 | 管理执行期模块注册、生命周期与上下文 |

## 核心类

### `NyarStandardRuntime`

```csharp
var runtime = new NyarStandardRuntime();
runtime.Load(module);
var result = runtime.Run("demo", "main");
```

### `ValkyrieRuntime`

迁移期兼容门面。短期内保留，以承接旧调用方；长期应由 `Valkyrie.Compiler` 和 `NyarStandardRuntime` 取代。

## 严格边界

- `Valkyrie.Runtime` 不再承担 `AST -> HIR -> MIR -> LIR`
- `Valkyrie.Runtime` 不再负责 multi target packaging
- `Valkyrie.Runtime` 只服务 `Nyar Standard IR` / `.nyar` 的加载与执行
- 历史编译入口保留仅用于兼容迁移，逐步淘汰

## 依赖

| 依赖 | 用途 | 来源 |
|:---|:---|:---|
| `Nyar.VM` | `NyarVM`、`NyarStandardVM`、执行时类型 | NyarVM.cs |
| `Nyar.Assembler` | `CgModule` / `Nyar Standard IR` 承载结构 | NyarVM.cs |
| `Acorn.Nyar` | `.nyar` 编码与解码 | Acorn.cs |

## 详细文档

- [架构详解](../../documentation/contributing/architecture.md)
- [编译管线详解](../../documentation/internals/compilation.md)
