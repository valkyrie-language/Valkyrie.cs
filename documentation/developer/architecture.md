# 架构详解

## 设计目标

Valkyrie 的稳定架构目标如下：

- `Oak` 只负责文本解码与编码，输出带完整 `TextSpan` 的 AST
- `Valkyrie` 只负责语言前端、语义分析、前端 lowering 与编译管线编排
- `Nyar` 只负责分析优化、方言降级、最优程序提取与目标数据结构生成
- `Acorn` 只负责目标格式编码，不重复实现语言语义
- `Legion` 只负责 workspace、build graph、缓存、dist 与日志汇总
- `NyarVM`、`JVM`、`CLR`、`WASM Browser/Node/Deno/Bun`、`WASI P1`、`WASI P2` 共用同一条语义主线，只在 ABI、入口包装和交付层分叉

## 现状问题

当前实现与目标结构存在以下偏差：

- `ValkyrieRuntime` 同时承担了编排、AST lowering、标准库特判、后端调用和产物拼装
- `Valkyrie.Runtime.csproj` 把 `Converter/**` 从编译中移除，直接破坏了 “Valkyrie 持有前端转换职责” 的边界
- `WasmBackend` 已经开始承载入口、imports、memory 和 JS glue 压力，继续叠加语言语义会让回归风险快速上升
- “生成文件” 仍然被当作成功条件，而不是“宿主可以正确加载、启动、调用和调试”

## 分层总览

Valkyrie 的主线应固定为：

`AST -> HIR -> MIR -> LIR -> multi target`

其中：

- `HIR` 表示带已解析符号与类型信息的高层语义表示
- `MIR` 表示面向优化的 `IKun/EGraph` 中间表示
- `LIR` 表示面向后端的 `NyarVM Standard IR`，当前可落在 `Nyar.Assembler.CgModule`

### 新项目切分图

```text
                           Valkyrie.cs
┌──────────────────────────────────────────────────────────────────────┐
│ Legion                                                              │
│  CanonicalTriple / Workspace / BuildGraph / Cache / Dist            │
└───────────────────────────────┬──────────────────────────────────────┘
                                │ BuildPlan
                                ▼
┌──────────────────────────────────────────────────────────────────────┐
│ Valkyrie.Compiler                                                   │
│  Parse -> Semantics -> HIR -> MIR(EGraph) -> Extract -> LIR         │
│  CanonicalTripleRegistry -> Backend Select -> Backend Generate      │
│  Packaging -> ArtifactSet                                            │
└───────────────┬──────────────────────────────────────┬───────────────┘
                │ LIR / Nyar Standard IR               │ ArtifactSet
                ▼                                      ▼
┌──────────────────────────────┐      ┌────────────────────────────────┐
│ Valkyrie.Runtime             │      │ Host Contract                  │
│  NyarStandardRuntime         │      │ Browser / Node / Deno / Bun    │
│  Load(.nyar / LIR)           │      │ JVM / CLR / WASI P1 / WASI P2  │
│  Run(module, function, args) │      │ launcher / sidecar / manifest  │
└───────────────┬──────────────┘      └────────────────────────────────┘
                │
                ▼
┌──────────────────────────────────────────────────────────────────────┐
│ NyarVM                                                              │
│  Nyar Standard IR / NyarModule / VM / JIT / Runtime Builtins        │
└──────────────────────────────────────────────────────────────────────┘
```

| 层级 | 组件 | 唯一职责 |
|:---|:---|:---|
| 工程层 | `Legion` | 工程发现、workspace 解析、build graph、缓存、target 选择、dist 落盘 |
| 文本层 | `Oak.Valkyrie` | 源码 `->` Token `->` AST，保留 `TextSpan` 与语法诊断 |
| 语义层 | `Valkyrie.TypeChecker` + `Valkyrie.Analyzer` | 名称解析、类型检查、属性解释、入口识别、模块依赖语义，构建 `SemanticModel` |
| `HIR` 层 | `Valkyrie.Compiler.Hir` | `CompilationUnit + SemanticModel -> Resolved HIR` |
| `MIR` 层 | `Valkyrie.Compiler.Mir` + `Nyar.Optimizer` | `HIR -> EGraph<IKun>`，执行方言降级、PE、重写、提取 |
| `LIR` 层 | `Valkyrie.Compiler.Lir` + `Nyar.Assembler` | `IKunTree -> CgModule`，形成统一 `NyarVM Standard IR` |
| 后端层 | `Nyar.Assembler.*` | `LIR ->` 目标数据结构 |
| 编码层 | `Acorn.*` | 目标数据结构 `->` 二进制编码 |
| 交付层 | `Packaging` | sidecar、launcher、manifest、调试资产、宿主桥接 |
| 运行时层 | `Valkyrie.Runtime` + `NyarVM` | 加载 `Nyar Standard IR` / `.nyar`，执行模块与宿主内建 |

## 标准编译流程

### `1. BuildPlan`

`Legion` 读取 `workspace`、`legion.von`、依赖图、target triple、优化级别和调试选项，产出统一的 `BuildPlan`。

### `2. Parse`

`Oak.Valkyrie` 负责词法与语法分析，输出 `CompilationUnit` 与文本诊断。

### `3. Semantics`

语义层执行符号收集、类型检查、属性解释、入口分析和模块导入绑定，输出 `SemanticModel`。

### `4. Build HIR`

`HIR` 只表达语言语义，不表达 target ABI，不直接拼装 backend 指令。

`HIR` 至少应具备：

- 已解析符号引用
- 已绑定类型信息
- 逻辑入口信息
- 标准库语义调用
- 显式控制流结构

### `5. Lower To MIR`

`HIR` 降级为 `MIR`，推荐直接落在 `EGraph<IKun>`。

这一层负责：

- 消除语法糖
- 统一表达式与控制流语义
- 为 PE 与 E-Graph 优化准备等价表示

### `6. Optimize MIR`

`Nyar.Optimizer` 在 `MIR` 上执行：

- 方言降级
- 部分求值
- 规则重写
- 成本模型提取

即使当前没有优化规则，也必须经过 `EGraph -> Extractor` 这条主线，不能直接从 AST 进入后端。

### `7. Lower To LIR`

从 `IKunTree` 降级到统一 `LIR`。当前建议把 `Nyar.Assembler.CgModule` 正式定义为 `NyarVM Standard IR` 的承载结构。

这一层负责：

- 把 `IKunTree` 变成稳定的 codegen IR
- 保持对多后端共享
- 不掺入宿主 packaging 细节

### `8. Backend Select`

根据 `CanonicalTriple` 解析出的目标契约选择 `NyarVM`、`JVM`、`CLR` 或 `WASM` 后端。

### `9. Backend Generate`

`Nyar.Assembler.*` 把统一 `LIR` 生成为 `NyarModuleData`、`JvmClassFileData`、`ClrModuleData`、`WasmModuleData` 等目标数据结构。

### `10. Encode`

编码阶段统一委托 `Acorn`，不得在 `Valkyrie` 或 `Nyar` 内重复实现目标格式编码器。

### `11. Package`

交付层按 profile 生成 `.js`、`.d.ts`、`.map`、launcher、manifest 等 sidecar 资产。

### `12. Validate`

验证阶段执行目标相关校验，例如 JVM 类合法性、CLR IL 有效性、WASM imports/exports/memory 约束和入口签名检查。

### `13. Dist`

最终由 `Legion` 按 `ArtifactSet` 输出 `dist` 目录，并汇总调试资产、校验记录与运行契约。

## 层间输入输出

| 上游 | 下游 | 数据 |
|:---|:---|:---|
| `Oak` | `Semantics` | `CompilationUnit + TextSpan` |
| `Semantics` | `HIR` | `CompilationUnit + SemanticModel` |
| `HIR` | `MIR` | `Resolved HIR` |
| `MIR` | `Optimizer` | `EGraph<IKun>` |
| `Optimizer` | `LIR` | `IKunTree` |
| `LIR` | `Backend` | `CgModule` / `NyarVM Standard IR` |
| `Backend` | `Acorn` | 目标数据结构 |
| `Acorn` | `Packaging` | 主二进制字节 + 元数据 |
| `Packaging` | `Legion` | `ArtifactSet` |

## `CanonicalTriple` 原则

target 身份必须只围绕 `CanonicalTriple` 建模，不再引入额外的内部目标身份层。

因此 target 选择不能继续仅依赖 `Arch`，必须至少经过两步：

1. `Legion` / 工程层解析为 `CanonicalTriple`
2. `Valkyrie.Compiler` / `Packaging` 从 triple 注册表查出目标契约

围绕 `CanonicalTriple` 的目标契约至少需要以下维度：

| 字段 | 说明 |
|:---|:---|
| `BackendFamily` | `NyarVM` / `JVM` / `CLR` / `WASM` |
| `HostKind` | `nyarvm` / `jdk` / `dotnet` / `browser` / `node` / `deno` / `bun` / `wasi-p1` / `wasi-p2` |
| `AbiProfile` | 目标 ABI 或组件模型 |
| `OutputKind` | `Module` / `Executable` / `Library` / `Component` |
| `EntryPolicy` | 逻辑入口到物理入口的生成规则 |
| `StdLibBindingPolicy` | 标准库语义到宿主 API 的绑定规则 |
| `DebugArtifactPolicy` | 调试资产输出策略 |

典型目标如下：

| `CanonicalTriple` | `BackendFamily` | `HostKind` |
|:---|:---|:---|
| `nyarvm-standard` | `NyarVM` | `nyarvm` |
| `jvm-openjdk-linux` | `JVM` | `jdk` |
| `clr-microsoft-windows` | `CLR` | `dotnet` |
| `wasm32-unknown-browser` | `WASM` | `browser` |
| `wasm32-unknown-node` | `WASM` | `node` |
| `wasm32-unknown-deno` | `WASM` | `deno` |
| `wasm32-unknown-bun` | `WASM` | `bun` |
| `wasm32-unknown-wasi-wasip1` | `WASM` | `wasi-p1` |
| `wasm32-unknown-wasi-wasip2` | `WASM` | `wasi-p2` |

## 入口与标准库绑定

### 入口点模型

入口需要分成两层：

1. 语义层识别逻辑入口，例如 `[main]`、显式导出、模块启动约定
2. target contract 层根据 `EntryPolicy` 生成物理入口

因此：

- `JVM` 的 `main(String[] args)` 属于 packaging / ABI 层
- `CLR` 的 `Main` 属于 packaging / ABI 层
- `WASM Browser/Node/Deno/Bun` 默认导出逻辑入口，不自动变成 `_start`
- `WASI P1` 的 `_start` 属于包装规则，不属于 AST lowering
- `WASI P2` 需要按 component/world 约定生成入口

### 标准库绑定层

前端只表达统一语义，例如 `std.io.print`、`std.fs.read_all_text`、`std.time.now`。

这些调用应在 `HIR` 中成为稳定语义节点，在 `MIR/LIR` 中继续保持“统一语义名”，直到 `StdLibBindingPolicy` 才绑定到宿主 API。

绑定策略由独立策略对象提供：

| 统一语义 | 绑定位置 |
|:---|:---|
| `std.io.print` | `StdLibBindingPolicy` |
| `std.fs.read_all_text` | `StdLibBindingPolicy` |
| `std.time.now` | `StdLibBindingPolicy` |

禁止在各 backend 内长期维护零散的 `if (print)`、`if (wasi)`、`if (web)` 特判。

## `ArtifactSet` 原则

`ValkyrieRuntime` 应该只接收统一的 `ArtifactSet`，而不是自己拼装文件列表。

建议字段如下：

| 字段 | 说明 |
|:---|:---|
| `PrimaryArtifact` | 主产物 |
| `SidecarArtifacts` | sidecar 资产 |
| `DebugArtifacts` | 调试资产 |
| `ValidationRecords` | 目标验证结果 |
| `RunContract` | 宿主加载和运行契约 |

## `Valkyrie.Compiler` 类级蓝图

### 门面层

`ValkyrieCompiler` 作为新的编译门面，内部阶段固定为 `HIR/MIR/LIR`：

```csharp
public sealed class ValkyrieCompiler
{
    public IReadOnlyList<GreenLeafNode> Lex(string source);
    public CompilationUnit Parse(IReadOnlyList<GreenLeafNode> tokens);
    public SemanticModel Analyze(CompilationUnit ast, BuildPlan plan);
    public HirModule BuildHir(CompilationUnit ast, SemanticModel semantics, BuildPlan plan);
    public MirModule BuildMir(HirModule hir, BuildPlan plan);
    public LirModule BuildLir(MirModule mir, BuildPlan plan);
    public ArtifactSet CompileToTarget(string source, BuildPlan plan);
}
```

### `Pipeline/`

`Pipeline/` 负责组织编译过程，不包含语言级 lowering 实现：

| 类型 | 职责 |
|:---|:---|
| `BuildPlan` | 输入的工程、target、优化级别、调试选项 |
| `CompilationContext` | 当前编译共享状态与诊断聚合 |
| `CompilerPipeline` | 串联 parse、semantics、HIR、MIR、LIR、backend、packaging |
| `ArtifactSet` | 统一交付结果 |
| `RunContract` | 宿主加载、入口和验证命令 |

### `Hir/`

`Hir/` 承接 `AST + SemanticModel -> Resolved HIR`：

| 类型 | 职责 |
|:---|:---|
| `HirModule` | 模块级高层语义表示 |
| `HirFunction` | 带解析符号和类型的函数 |
| `HirSymbolRef` | 已绑定的符号引用 |
| `HirTypeRef` | 已绑定的类型引用 |
| `HirBuilder` | AST 到 HIR 的构建器 |

### `Mir/`

`Mir/` 承接 `HIR -> EGraph<IKun>` 与优化结果：

| 类型 | 职责 |
|:---|:---|
| `MirModule` | `EGraph<IKun>` 与根节点、优化元数据 |
| `HirToMirLowerer` | `HIR -> EGraph<IKun>` |
| `MirOptimizationPipeline` | 封装 `LoweringPass`、PE、重写、提取 |
| `MirExtractionResult` | `IKunTree` 与优化统计信息 |

### `Lir/`

`Lir/` 承接 `IKunTree -> CgModule`：

| 类型 | 职责 |
|:---|:---|
| `LirModule` | `CgModule` 的运行时语义包装 |
| `IkunTreeToLirLowerer` | `IKunTree -> CgModule` |
| `LirFunctionBuilder` | 函数级 codegen IR 构建 |
| `LirIntrinsicResolver` | 统一语义到 `LIR` intrinsic 的解析 |

### `Targets/`

`Targets/` 统一承载 target 差异：

| 类型 | 职责 |
|:---|:---|
| `TargetContract` | 围绕 `CanonicalTriple` 的完整目标契约 |
| `CanonicalTripleRegistry` | triple 或别名到目标契约的解析 |
| `EntryPolicy` | 逻辑入口到物理入口的包装 |
| `StdLibBindingPolicy` | 统一语义到宿主 API 的绑定 |
| `DebugArtifactPolicy` | 调试资产选择 |

### `Packaging/`

`Packaging/` 专门负责 sidecar 和宿主胶水生成：

| 类型 | 职责 |
|:---|:---|
| `ITargetPackager` | target packaging 抽象 |
| `NyarVmPackager` | `.nyar` 的 manifest、symbols、运行契约 |
| `JvmPackager` | `MANIFEST.MF`、launcher、入口包装 |
| `ClrPackager` | `runtimeconfig`、`deps`、入口包装 |
| `BrowserWasmPackager` | `.js`、`.d.ts`、Browser imports 清单 |
| `NodeWasmPackager` | Node 宿主胶水与 imports 清单 |
| `DenoWasmPackager` | Deno 宿主胶水与 imports 清单 |
| `BunWasmPackager` | Bun 宿主胶水与 imports 清单 |
| `WasiP1Packager` | `_start` 契约、WASI P1 sidecar |
| `WasiP2Packager` | component/world 产物与 metadata |

## `Valkyrie.Runtime` 运行时蓝图

`Valkyrie.Runtime` 不再承担新的编译主线，定位收敛为 `NyarVM` 执行封装层。

### 门面层

```csharp
public sealed class NyarStandardRuntime
{
    public void Load(CgModule module);
    public void LoadBytecode(byte[] bytes);
    public Value Run(string moduleName, string functionName, params Value[] args);
}
```

### 运行时职责

| 类型 | 职责 |
|:---|:---|
| `NyarStandardRuntime` | 面向 Valkyrie 的运行时门面 |
| `ModuleLoader` | `CgModule` / `.nyar` 加载与校验 |
| `RuntimeHost` | 内建函数、宿主对象与标准运行时能力 |
| `ExecutionSession` | 模块装载、调用与生命周期 |

### 严格边界

- `Valkyrie.Runtime` 不再承担 `AST -> HIR -> MIR -> LIR`
- `Valkyrie.Runtime` 不再负责 multi target packaging
- `Valkyrie.Runtime` 只服务 `Nyar Standard IR` / `.nyar` 的加载与执行
- 历史编译入口保留仅用于兼容迁移，逐步淘汰

## 现有职责到新结构的映射

| 当前位置 | 当前职责 | 新归属 |
|:---|:---|:---|
| `ValkyrieRuntime.Lex/Parse/CheckTypes` | 历史编译门面 | 迁入 `Valkyrie.Compiler`，运行时侧仅保留兼容层 |
| `ValkyrieRuntime.ConvertAstToAsm` | 直接 AST 到 backend IR | 删除，改为 `AST -> HIR -> MIR -> LIR` |
| `ProcessStatement` / `EmitExpression` | 语言级 lowering 细节 | 迁入 `Valkyrie.Compiler.Hir` / `Mir` / `Lir` |
| `EmitPrintCall` | 标准库特判 | 先转为 `HIR` 统一语义调用，再由 `StdLibBindingPolicy` 绑定 |
| `CompileToTarget` 内的 `if/else` 产物拼装 | 交付组装 | 迁入 `Valkyrie.Compiler.Packaging` |
| `SelectBackend(target.Arch)` | 粗粒度 target 路由 | 升级为 `CanonicalTripleRegistry` |
| `Valkyrie.Runtime.csproj` 排除 legacy `Converter/**` | 历史遗留原型 | 新实现不再放入 `Runtime`，逐步迁出到 `Compiler` |

## 与其他项目的边界

### `Valkyrie.TypeChecker`

建议从“仅类型检查器”升级为语义分析中心，与 `Valkyrie.Analyzer` 一起稳定输出 `SemanticModel`。

### `Valkyrie.Compiler`

承担新的正确编译主线，是 `AST -> HIR -> MIR -> LIR -> multi target` 的唯一门面。

### `Valkyrie.Runtime`

收敛为 `NyarVM` 执行封装层，负责 `Nyar Standard IR` / `.nyar` 模块的装载、运行和宿主内建管理。

### `Nyar.Assembler.*`

保持“目标数据结构生成”职责，不直接写文件，不直接处理完整 Web/WASI/JDK/BCL 的 packaging 细节。
其中 `CgModule` 应被明确视为 `LIR` 承载结构，而不是让前端直接把 AST 编成后端指令。

### `Acorn.*`

保持“唯一编码源”职责。任何 `.class`、`.wasm`、`.nyar`、`.dll` 编码都必须委托给 `Acorn`。

### `Legion`

只处理 workspace、project、canonical triple、cache、dist，不参与前端 lowering，也不推断目标入口逻辑。

## 分阶段重构计划

| 阶段 | 目标 | 验收标准 |
|:---|:---|:---|
| `Phase 0` | 冻结交付契约 | 每个 target 都有明确的入口、imports、sidecar、验证命令 |
| `Phase 1` | 新建 `Valkyrie.Compiler` 空骨架 | 架构图、目录与门面 API 固定 |
| `Phase 2` | 建立 `SemanticModel` 与 `HIR` | 前端不再直接猜参数与返回类型 |
| `Phase 3` | 引入 `MIR(EGraph)` 主线 | 所有 target 至少共享 `HIR -> MIR -> Extract` |
| `Phase 4` | 建立 `LIR(CgModule)` | backend 不再直接吃 AST 或半成品 lowering |
| `Phase 5` | 引入 `CanonicalTripleRegistry` 与标准库绑定 | `Browser/Node/Deno/Bun`、`WASI P1`、`WASI P2` 明确分离，绑定不再散落 |
| `Phase 6` | 收缩 `Valkyrie.Runtime` 为 `NyarVM` 封装 | 旧编译入口保留兼容层，新实现不再落在 `Runtime` |
| `Phase 7` | 后端瘦身与验证闭环 | backend 返回统一 `ArtifactSet` 或等价结构，并有真实可运行样例 |

## 严格禁止的方向

- 禁止继续在 `ValkyrieRuntime` 中堆叠更多表达式 lowering 分支
- 禁止在 `WasmBackend` 内长期保留语言级 `print`、入口和宿主 glue 特判
- 禁止把 `WASI P1` 和 `WASI P2` 视为“只差 import 名”的同类目标
- 禁止让 `Legion` 越过 `Valkyrie/Nyar/Acorn` 自行推断代码生成行为
- 禁止每个 backend 各自维护一套标准库语义

## 相关文档

- 目标交付契约详见 [Target Contract Spec](target-contract-spec.md)
- 依赖方向详见 [依赖规则](dependency-rules.md)
