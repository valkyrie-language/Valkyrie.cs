# 编译管线详解

## 完整路径

Valkyrie 的稳定主线应固定为：

`源码 -> AST -> HIR -> MIR(EGraph) -> LIR(NyarVM Standard IR) -> 目标数据结构 -> 编码 -> 交付`

这条主线的核心约束如下：

- 所有 target 共享同一条语义主线
- 语义分叉只能发生在 `LIR -> backend -> packaging` 之后
- 不允许 `AST` 直接进入 backend
- 即使当前不跑任何优化，也必须经过 `MIR(EGraph) -> Extractor`

## 阶段一：文本解码（Oak）

`Oak.Valkyrie` 负责源码文本到 AST 的转换，并保留完整 `TextSpan` 与语法诊断：

```csharp
var tokens = ValkyrieLexer.Tokenize(sourceText);
var ast = ValkyrieParser.Parse(tokens);
```

这一层只做文本编解码，不做语义分析、优化或目标特化。

## 阶段二：语义分析（Semantics）

语义层负责：

- 名称解析
- 类型检查
- 属性解释
- 模块导入绑定
- 逻辑入口识别

输出必须是稳定的 `SemanticModel`，而不是单纯布尔成败：

```csharp
var typeChecker = new Valkyrie.TypeChecker.TypeChecker();
SemanticModel semantics = typeChecker.Analyze(ast);
```

## 阶段三：构建 HIR（Resolved Symbols）

`HIR` 是带已解析符号与类型信息的高层中间表示。

它的职责是把 AST 中的语法结构转换成稳定语义结构，同时显式保留：

- 已绑定的符号引用
- 已绑定的类型引用
- 逻辑入口
- 标准库统一语义调用
- 显式控制流与作用域

```csharp
var hir = HirBuilder.Build(ast, semantics);
```

### HIR 的边界

`HIR` 不应该：

- 表达 `WASM` / `JVM` / `CLR` 的 ABI
- 生成 `_start`、`main(String[] args)`、`Main`
- 拼接 `ptr + len`、`iovec`、JS glue
- 直接写 `CgInstruction`

## 阶段四：降级到 MIR（EGraph）

`MIR` 是优化主战场，推荐直接以 `EGraph<IKun>` 作为承载。

```csharp
var mir = HirToMirLowerer.Lower(hir);
```

这一层负责：

- 消除语法糖
- 统一表达式求值顺序
- 统一控制流与返回语义
- 把标准库调用降为稳定的统一语义节点
- 为 PE、重写规则和成本模型提取准备等价表示

## 阶段五：MIR 优化与提取（Nyar）

`Nyar.Optimizer` 在 `MIR` 上执行：

- 方言降级
- 部分求值
- 规则重写
- E-Graph 饱和
- 成本模型提取

```csharp
var lowered = LoweringPass.Run(mir.EGraph);
var optimized = PartialEvaluator.Run(lowered);
var tree = Extractor.Extract(optimized, mir.RootId, new DefaultCostModel());
```

即使当前没有任何优化规则，也必须保持这条形态：

`HIR -> MIR(EGraph) -> Extractor -> IKunTree`

这能保证未来加入 `PE`、常量折叠、标准库降级、target-specific lowering 时不会再次退回 `AST -> backend` 的短路结构。

## 阶段六：降级到 LIR（NyarVM Standard IR）

`LIR` 是统一 codegen IR，建议正式定义为 `NyarVM Standard IR`。

当前最现实的承载结构是 `Nyar.Assembler.CgModule`，但语义上应视为 `LIR`，而不是“让前端直接写 backend 指令”。

```csharp
var lir = IkunTreeToLirLowerer.Lower(tree);
```

这一层负责：

- 把 `IKunTree` 变成稳定的 codegen IR
- 统一多后端共享的调用、控制流和数据布局语义
- 为后续 target-specific backend 提供单一输入

### LIR 的边界

`LIR` 不应该：

- 直接表达 `.class`、`.wasm`、`.dll` 文件格式
- 直接决定宿主入口包装
- 直接生成 `.js`、`.d.ts`、launcher、manifest

## 阶段七：后端生成（Nyar.Assembler）

后端只消费统一 `LIR`，生成目标数据结构：

```csharp
var backend = BackendSelector.Select(targetProfile);
var targetData = backend.Generate(lir);
```

| 后端 | 输入 | 生成 | 输出 |
|:---|:---|:---|:---|
| `NyarVM` | `LIR` | `NyarModuleData` | `.nyar` |
| `WASM` | `LIR` | `WasmModuleData` | `.wasm` |
| `JVM` | `LIR` | `JvmClassFileData` | `.class` |
| `CLR` | `LIR` | `ClrModuleData` | `.dll/.exe` |
| `Native` | `LIR` | Native 目标数据结构 | `.elf/.exe/.dylib` |

## 阶段八：编码（Acorn）

编码阶段统一委托 `Acorn`：

```csharp
byte[] bytes = encoder.Encode(targetData);
```

禁止在 `Valkyrie` 或 `Nyar` 内重复实现 `.class`、`.wasm`、`.nyar` 等目标格式编码逻辑。

## 阶段九：Packaging 与交付

主产物编码完成后，由 target packaging 层负责：

- 入口包装
- imports 契约
- sidecar 资产
- 调试产物
- 运行契约

```csharp
ArtifactSet artifacts = packager.Package(primaryBytes, metadata, canonicalTriple);
```

这一步之后才允许按 `CanonicalTriple` 分叉成：

- `nyarvm-standard`
- `jvm-openjdk-*`
- `clr-microsoft-*`
- `wasm32-unknown-browser`
- `wasm32-unknown-node`
- `wasm32-unknown-deno`
- `wasm32-unknown-bun`
- `wasm32-unknown-wasi-wasip1`
- `wasm32-unknown-wasi-wasip2`

完整定义见 [目标三元组规范](../toolchain/target-triples.md) 与 [Target Contract Spec](../contributing/target-contract-spec.md)。

## HIR / MIR / LIR 的职责划分

| 层级 | 核心内容 | 允许知道的事情 | 严格禁止 |
|:---|:---|:---|:---|
| `HIR` | 语言语义 | 符号、类型、入口、统一标准库语义 | ABI、宿主入口、文件格式 |
| `MIR` | `EGraph<IKun>` | 等价变换、PE、优化、提取 | target-specific imports、packaging |
| `LIR` | `NyarVM Standard IR` | 调用约定、控制流、codegen 结构 | `.wasm/.class/.dll` 编码、sidecar |

## 典型例子：`print("hello")`

### AST

源码中的函数调用节点仍是语言语法形式。

### HIR

变成统一语义调用：

```text
StdCall("std.io.print", ["hello"])
```

### MIR

进入 `IKun/EGraph`，可参与等价重写、部分求值和提取。

### LIR

变成统一的 `CallIntrinsic("std.io.print")` 或等价结构。

### Packaging / Binding

直到 target packaging / `StdLibBindingPolicy` 才绑定为：

- `NyarVM` -> VM builtin
- `JVM` -> `PrintStream.println`
- `CLR` -> `System.Console.WriteLine`
- `WASM Browser/Node/Deno/Bun` -> `env.console_log`
- `WASI P1` -> `fd_write`
- `WASI P2` -> component stdout 接口

## 入口点模型

入口必须拆成两层：

1. 语义层识别逻辑入口
2. packaging 层根据 `EntryPolicy` 生成物理入口

因此：

- `HIR/MIR/LIR` 只保留逻辑入口
- `JVM` 的 `main(String[] args)` 由 packaging 层生成
- `CLR` 的 `Main` 由 packaging 层生成
- `WASM Browser/Node/Deno/Bun` 默认导出逻辑入口，不自动变成 `_start`
- `WASI P1` 的 `_start` 属于 ABI 包装
- `WASI P2` 按 component/world 约定生成入口

## 当前代码迁移原则

现有代码如果仍存在 `AST -> Asm` 直连，只能视为临时过渡，不应成为最终结构。

正确的收敛方向是：

1. `AST -> SemanticModel -> HIR`
2. `HIR -> MIR(EGraph)`
3. `MIR -> Extractor -> IKunTree`
4. `IKunTree -> LIR(CgModule)`
5. `LIR -> multi target`

## Web 方言

Web 相关能力仍然应通过 `Nyar Web` 方言进入 `MIR`，而不是在 backend 中直接写语言级分支。

例如：

- `IKunCreateElement`
- `IKunSetAttribute`
- `IKunListener`
- `IKunLocalStorageGet`
- `IKunLocalStorageSet`

这些节点先作为统一语义节点进入 `MIR`，再在 `LIR -> backend -> packaging` 阶段按 `CanonicalTriple` 契约绑定和交付。
