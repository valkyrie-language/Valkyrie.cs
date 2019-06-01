# VCC 编译器

VCC（Valkyrie Compiler Collection）是 Valkyrie 语言编译器，将 `.v` 源码编译为多种目标平台的交付产物。

## 命令行用法

```bash
vcc                       # 启动 REPL
vcc compile <file>        # 编译文件
vcc run <file>            # 编译并运行
vcc test <file>           # 运行测试
vcc repl                  # 交互式环境
vcc format <dir>          # 格式化代码
vcc check <dir>           # 运行代码检查
```

## REPL 交互式环境

```valkyrie
vcc> let x = 42
vcc> x + 1
43
vcc> micro greet(name) { println("Hello, {name}") }
vcc> greet("Valkyrie")
Hello, Valkyrie
```

## 编译流程

```
源码 .v → Oak.Valkyrie Lexer / Parser → AST
→ Semantics → HIR(Resolved Symbols)
→ MIR(EGraph<IKun>) → Extractor → IKunTree
→ LIR(Nyar Standard IR) → 目标后端代码生成
→ Acorn 编码 → Packaging → ArtifactSet
```

## 多后端代码生成

VCC 支持多种目标平台：

| 后端 | Vendor | 输出格式 | 适用场景 |
|:---|:---|:---|:---|
| NyarVM | unknown | `.nyar` 字节码 | 通用计算、开发调试 |
| WASM | unknown | `.wasm` | 浏览器前端、Edge 计算 |
| JVM | openjdk / android | `.class` | Java 生态集成 |
| CLR | microsoft / unity | `.dll` / `.exe` | .NET 生态集成 |
| Native | pc / apple | `.elf` / `.exe` / `.dylib` | 原生高性能 |

详细的目标三元组定义见[目标三元组规范](toolchain/target-triples.md)。

所有后端共享同一条 `HIR -> MIR -> LIR` 主线，后端只负责目标数据结构生成，不识别标准库语义。

## 模块系统

### 导入模块

```valkyrie
import std.io
import game::physics
import package::module
```

### 模块路径解析

VCC 按以下顺序查找模块：

1. 当前文件所在目录
2. 项目根目录
3. `vendors/` 目录（非递归）
4. 全局 `vendors/` 目录

## vendors 不递归

`vendors/` 目录中的依赖不会递归查找其自身的 `vendors/`。每个包只看到自己直接声明的依赖。

## 配置传递

VCC 不直接读取配置文件。配置通过命令行参数和环境变量传递。

## 与 Legion 的协作

VCC 不知道 Legion 的存在。Legion 负责填充 `vendors/`，VCC 从 `vendors/` 读取依赖编译。两者通过 `vendors/` 目录解耦。

```
Legion: 解析依赖 → 下载程序集 → 放入 vendors/
VCC:    读取 vendors/ → 编译源码 → 输出字节码
```

## 使用 `Valkyrie.Compiler` API

下游开发者应使用 `Valkyrie.Compiler` 嵌入编译能力，`Valkyrie.Runtime` 只负责运行时执行：

```csharp
using Valkyrie.Compiler;

var compiler = new ValkyrieCompiler();
var result = compiler.CompileToTarget(sourceCode, buildPlan);
if (result.Success)
{
    // result.ArtifactSet / output files
}
```

## 使用 `Valkyrie.Runtime` 执行 `.nyar`

对 `nyarvm-standard` 目标，运行阶段再交给 `Valkyrie.Runtime`：

```csharp
using Valkyrie.Runtime;

var bytes = File.ReadAllBytes("module.nyar");
var runtime = new NyarStandardRuntime();
runtime.LoadBytecode(bytes);
var returnValue = runtime.Run("module", "main");
```
