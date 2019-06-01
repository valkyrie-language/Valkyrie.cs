# 内部原理总览

## 分层架构

Valkyrie 编译管线遵循 Nyar 组织标准分层架构：

```
Layer 1: 文本编解码 (Oak)
    ↓ AST
Layer 2: 语义分析 (Valkyrie.TypeChecker + Valkyrie.Analyzer)
    ↓ SemanticModel
Layer 3: HIR 构建 (Valkyrie.Compiler.Hir)
    ↓ Resolved HIR
Layer 4: MIR 构建 (Valkyrie.Compiler.Mir)
    ↓ EGraph<IKun>
Layer 5: 分析与优化 (Nyar)
    ↓ IKunTree
Layer 6: LIR 构建 (Valkyrie.Compiler.Lir)
    ↓ Nyar Standard IR
Layer 7: 代码生成 (Nyar.Assembler)
    ↓ 目标数据结构
Layer 8: 二进制编码 (Acorn)
    ↓ 主产物
Layer 9: Packaging (Valkyrie.Compiler.Packaging)
    ↓ ArtifactSet
Layer 10: 执行 (Valkyrie.Runtime + NyarVM / JVM / CLR / WASM 宿主)
```

## 编译管线全景

```
源码 .v
  │
  → Oak.Valkyrie Lexer / Parser
  → AST (Oak.Valkyrie.AST.CompilationUnit)
  │
  → Valkyrie.TypeChecker + Valkyrie.Analyzer
  → SemanticModel
  │
  → Valkyrie.Compiler.Hir.HirBuilder
  → Resolved HIR
  │
  → Valkyrie.Compiler.Mir.HirToMirLowerer
  → EGraph<IKun>
  │
  → Nyar.Optimizer.LoweringPass / PE / Extractor
  → IKunTree
  │
  → Valkyrie.Compiler.Lir.IkunTreeToLirLowerer
  → Nyar Standard IR (CgModule)
  │
  → Nyar.Assembler.* Backend
  │   ├─ NyarVM Backend → NyarModuleData  → Acorn.Nyar  → .nyar
  │   ├─ WASM Backend   → WasmModuleData  → Acorn.Wasm  → .wasm
  │   ├─ JVM Backend    → JvmClassData    → Acorn.Jvm   → .class
  │   ├─ CLR Backend    → ClrModuleData   → Acorn.Clr   → .dll
  │   └─ Native Backend → Native Data     → Acorn.Elf/Pe/MachO
  │
  → Valkyrie.Compiler.Packaging
  → ArtifactSet
  │
  → Valkyrie.Runtime / Host Contract Load & Run
```

## 三层角色

每层只做自己擅长的事：

| 层 | 角色 | 关键实现 |
|:---|:---|:---|
| **Valkyrie 语言** | 用户可见的语言表面 | 语法设计、类型检查、AWSL 组件 |
| **Valkyrie.Compiler** | 语言到多目标交付的编译门面 | `HIR/MIR/LIR`、`CanonicalTripleRegistry`、Packaging |
| **Valkyrie.Runtime** | `NyarVM` 执行封装 | `Nyar Standard IR` / `.nyar` 装载与运行 |
| **Nyar 优化器** | 通用优化引擎 | EGraph 饱和、方言降级、最优程序提取 |
| **代码生成后端** | IR → 目标平台 | NyarVM/WASM/JVM/CLR/Native Backend |

## 增量编译

编译器支持增量编译，只重新编译变更的模块：

```
IncrementalCompiler
  ├── DependencyGraph — 模块依赖关系图
  ├── 脏标记传播 — 模块变更 → 依赖者标记为脏
  └── CompilationStats — 编译统计信息
```

- 模块变更时，只重编译该模块及其下游依赖
- 未变更模块从缓存加载
- 输出 `CompilationStats` 显示增量编译收益

## 测试覆盖

```
Valkyrie.Tests/          # 核心语言 + 编译器测试
  ├── E2ETests/          # 端到端 Native 目标测试
  ├── LexerTests/        # 词法分析单元测试
  ├── ParserTests/       # 语法分析单元测试
  ├── TypeCheckerTests/  # 类型检查单元测试
  ├── FormatterTests/    # 格式化器集成测试
  └── PackageManagerTests/ # 包管理器单元测试

Asgard.Tests/            # VOA 框架测试
  ├── AwslReactiveCompilerTests
  ├── AwslSsrRendererTests
  ├── ModuleDceTests
  ├── PwaGeneratorTests
  └── VoaCompilerTests

Legion.Tests/            # 包管理器测试
  ├── 各注册表适配器测试
  ├── PackageCacheTests
  ├── VendorAuthStoreTests
  └── VendorManagerTests

Valhalla.Tests/          # 注册表测试
  ├── Ed25519AuthMiddlewareTests
  ├── ValhallaClientTests
  ├── ValhallaConfigTests
  ├── ValhallaDigestTests
  ├── ValhallaE2ETests
  ├── ValhallaIncarnationTests
  ├── ValhallaInstallerTests
  ├── ValhallaLockFileTests
  └── ValhallaLockFileValidationTests
```

详见 [测试指南](../contributing/testing.md)。
