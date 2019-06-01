# Valkyrie.Compiler

`Valkyrie.Compiler` 是 Valkyrie 的新编译器骨架项目。

它直接采用正确的主线：

`AST -> HIR(Resolved Symbols) -> MIR(EGraph) -> LIR(Nyar Standard IR) -> multi target`

## 项目定位

- 负责 `Oak.Valkyrie` AST 到多目标交付产物的完整编译流程
- 负责 `CanonicalTripleRegistry`、target contract、packaging 与 `ArtifactSet`
- 不负责运行时执行，不直接承担 `NyarVM` 会话管理

## 目录蓝图

```text
Valkyrie.Compiler/
├── Pipeline/          # BuildPlan、CompilationContext、CompilerPipeline、ArtifactSet
├── Hir/               # AST + SemanticModel -> Resolved HIR
├── Mir/               # HIR -> EGraph<IKun> -> Extract
├── Lir/               # IKunTree -> Nyar Standard IR (CgModule)
├── Targets/           # CanonicalTripleRegistry、TargetContract、EntryPolicy
├── Packaging/         # sidecar、launcher、manifest、RunContract
└── Valkyrie.Compiler.csproj
```

## 编译主线

| 阶段 | 输入 | 输出 |
|:---|:---|:---|
| Parse | 源码 | `CompilationUnit` |
| Semantics | `CompilationUnit` | `SemanticModel` |
| Build HIR | `CompilationUnit + SemanticModel` | `Resolved HIR` |
| Build MIR | `HIR` | `EGraph<IKun>` |
| Optimize MIR | `EGraph<IKun>` | `IKunTree` |
| Build LIR | `IKunTree` | `Nyar Standard IR` |
| Backend Generate | `LIR + CanonicalTriple` | 目标数据结构 |
| Encode | 目标数据结构 | 主产物字节 |
| Package | 主产物 + metadata | `ArtifactSet` |

## 与 `Valkyrie.Runtime` 的边界

| 项目 | 只做什么 |
|:---|:---|
| `Valkyrie.Compiler` | 编译、交付、target contract |
| `Valkyrie.Runtime` | 加载、运行、宿主内建 |

## 当前迁移策略

- 新实现全部进入 `Valkyrie.Compiler`
- `Valkyrie.Runtime` 中的历史编译代码仅保留兼容层
- 旧的 `AST -> Asm` 直连路径逐步下线
