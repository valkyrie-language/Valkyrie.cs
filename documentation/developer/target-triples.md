# Valkyrie Canonical Target 规范

## 1. 目的

本文档定义 Valkyrie 编译器的唯一目标标识 **CanonicalTarget**，说明它的格式、语义、归一化规则及注册表。

核心原则

- 编译器内部只认 CanonicalTarget
- CanonicalTarget 是唯一目标键，不存在 ProfileId、HostKind 等并行身份
- 一切运行时约定、生成策略都从 CanonicalTarget 派生，而非另建主键
- 所有组件统一使用这一套注册表

---

## 2. 术语

### 2.1 Target

用户在配置文件、命令行等处输入的原始目标字符串，可能是完整写法、短别名或其他便利形式。

### 2.2 CanonicalTarget

编译器解析、归一化后得到的唯一目标标识，固定为四段式：

```
arch-impl-spec[-abi]
```

### 2.3 目标三元组

出于工程习惯，本文仍将 CanonicalTarget 称为“目标三元组”，但其真实结构是**四段语义**：

- `arch` — 执行模型
- `impl` — 运行时实现
- `spec` — 接口规范
- `abi` — 调用约定 / 输出格式

当 `abi` 省略时，表示取该目标族的默认 ABI（内部仍会补全为完整四段式）。

---

## 3. 格式定义

### 3.1 基础格式

```
arch-impl-spec[-abi]
```

| 段 | 必填 | 含义 |
|:---|:---:|:---|
| `arch` | 是 | 执行模型或代码生成后端族 |
| `impl` | 是 | 该规范的具体实现者、运行时提供者 |
| `spec` | 是 | 目标遵循的接口标准或宿主环境规范 |
| `abi` | 否 | 调用约定、输出格式或编译策略 |

### 3.2 归一化规则

任何输入目标在进入编译器内部前，必须经过以下步骤得到 CanonicalTarget：

1. 若为短别名，展开为完整四段式
2. 若目标族存在默认 ABI 且输入省略，则补全该默认 ABI
3. 最终结果不再保留任何省略或别名痕迹，所有组件均以此为准

---

## 4. 各段语义

### 4.1 `arch` — 执行模型

`arch` 表示**代码将在哪种执行引擎上运行**，而不是 CPU 指令集品牌。

| 值 | 含义 | 典型产物 |
|:---|:---|:---|
| `x86_64` | 64 位 x86 原生执行 | `.exe` / ELF / `.dylib` |
| `aarch64` | 64 位 ARM 原生执行 | ELF / Mach-O |
| `wasm32` | 32 位 WebAssembly | `.wasm` |
| `wasm64` | 64 位 WebAssembly | `.wasm` |
| `nyar` | NyarVM 字节码 | `.nyar` |
| `gnosis` | GnosisVM 字节码 | `.gnosis` |
| `clr` | CLR 托管执行模型（IL + 运行时） | `.dll` / `.exe` |
| `jvm` | JVM 托管执行模型 | `.class` / `.jar` |
| `spirv` | SPIR-V 着色器执行模型 | `.spv` |
| `native` | 按当前宿主解析为本机 `arch` | 平台相关 |

### 4.2 `impl` — 运行时实现

`impl` 表示**接口规范的具体实现方**，当规范只有一个权威实现或实现与开发者无关时，可使用 `unknown`。

| 值 | 含义 |
|:---|:---|
| `unknown` | 未指定特定实现，通用 |
| `microsoft` | Microsoft .NET 运行时 |
| `unity` | Unity Mono / IL2CPP |
| `mono` | 独立 Mono |
| `openjdk` | OpenJDK / HotSpot |
| `android` | Android ART / Dalvik |
| `graalvm` | GraalVM |
| `apple` | Apple 工具链与运行时 |
| `pc` | GNU 兼容 PC 工具链 |
| `vulkan` | Vulkan 运行时（实际规范见 `spec`） |
| `node` | Node.js 提供的运行时实现 |
| `deno` | Deno 提供的运行时实现 |
| `bun` | Bun 提供的运行时实现 |

### 4.3 `spec` — 接口规范

`spec` 表示**目标代码所遵循的接口标准或宿主环境规范**。它回答：我的代码期待调用怎样的 API，受怎样的契约约束。

| 值 | 含义 |
|:---|:---|
| `unknown` | 不绑定特定规范 |
| `windows` | Windows API 规范 |
| `linux` | Linux syscall / ABI 规范 |
| `darwin` | macOS / Darwin 规范 |
| `ios` | iOS 规范 |
| `android` | Android 平台规范 |
| `browser` | Web 浏览器宿主 API 规范 |
| `node` | Node.js API 规范 |
| `deno` | Deno API 规范 |
| `bun` | Bun API 规范 |
| `wasi` | WASI 系统接口规范 |
| `vulkan` | Vulkan 着色器接口规范 |
| `native` | 字节码虚拟机自身的内部规范 |
| `none` | 无宿主规范（如着色器底层） |

### 4.4 `abi` — 调用约定与输出格式

`abi` 表示**产物的具体格式、调用约定及编译策略**。

| 值 | 含义 | 适用 `arch` |
|:---|:---|:---|
| `gnu` | GNU ABI（Linux 等） | `x86_64`, `aarch64` |
| `msvc` | Microsoft ABI（Windows 原生） | `x86_64`, `aarch64` |
| `wasm` | 标准 WebAssembly 二进制模块 | `wasm32`, `wasm64` |
| `wasip1` | WASI Preview 1 | `wasm32` |
| `wasip2` | WASI Preview 2 / Component Model | `wasm32` |
| `managed` | 标准托管执行（JIT 加载） | `clr`, `jvm` |
| `nativeaot` | .NET NativeAOT 预编译 | `clr` |
| `il2cpp` | Unity IL2CPP 转译 C++ | `clr` |
| `dex` | Android DEX 格式 | `jvm` |
| `native` | GraalVM Native Image 原生可执行 | `jvm` |

---

## 5. 默认 ABI 与唯一化

以下目标族在用户输入时允许省略 ABI，编译器内部将自动补全。

| 允许输入 | 归一化 CanonicalTarget |
|:---|:---|
| `wasm32-unknown-browser` | `wasm32-unknown-browser-wasm` |
| `wasm32-node-unknown` | `wasm32-node-unknown-wasm` |
| `wasm32-deno-unknown` | `wasm32-deno-unknown-wasm` |
| `wasm32-bun-unknown` | `wasm32-bun-unknown-wasm` |
| `clr-microsoft-windows` | `clr-microsoft-windows-managed` |
| `jvm-openjdk-linux` | `jvm-openjdk-linux-managed` |

以下目标无 ABI 维度，四段式直接定形：

- `nyar-unknown-native`
- `gnosis-unknown-native`
- `spirv-unknown-vulkan` （`vulkan` 是规范，而非实现）

---

## 6. CanonicalTarget 注册表

### 6.1 NyarVM / GnosisVM

| CanonicalTarget | 含义 | 常用运行约定 |
|:---|:---|:---|
| `nyar-unknown-native` | NyarVM 字节码 | 由 NyarVM 直接加载执行 |
| `gnosis-unknown-native` | GnosisVM 字节码 | 由 GnosisVM 直接加载执行 |

### 6.2 JVM

| CanonicalTarget | 含义 | 常用运行约定 |
|:---|:---|:---|
| `jvm-openjdk-linux-managed` | OpenJDK 标准托管执行 (Linux) | 入口 `main(String[])`，产物 `.class`/`.jar` |
| `jvm-openjdk-windows-managed` | OpenJDK 标准托管执行 (Windows) | 同上 |
| `jvm-android-android-dex` | Android DEX | Android 应用模型，产物 `.dex`/`.apk` |
| `jvm-graalvm-linux-native` | GraalVM Native Image (Linux) | 原生可执行文件 |
| `jvm-graalvm-windows-native` | GraalVM Native Image (Windows) | 原生可执行文件 |

### 6.3 CLR / .NET

| CanonicalTarget | 含义 | 常用运行约定 |
|:---|:---|:---|
| `clr-microsoft-windows-managed` | Microsoft .NET 标准托管 (Windows) | 入口 `Main`，产物 `.dll`/`.exe`，附属 `.runtimeconfig.json` 等 |
| `clr-microsoft-linux-managed` | Microsoft .NET 标准托管 (Linux) | 同上 |
| `clr-microsoft-darwin-managed` | Microsoft .NET 标准托管 (macOS) | 同上 |
| `clr-microsoft-windows-nativeaot` | .NET NativeAOT (Windows) | 原生可执行文件 |
| `clr-microsoft-linux-nativeaot` | .NET NativeAOT (Linux) | 原生可执行文件 |
| `clr-unity-windows-managed` | Unity Mono 托管执行 | 受 Unity 运行时约束 |
| `clr-unity-windows-il2cpp` | Unity IL2CPP 转译 | 由 Unity IL2CPP 管线继续处理 |

### 6.4 WASM — 浏览器与 JS 宿主

| CanonicalTarget | 含义 | 常用运行约定 |
|:---|:---|:---|
| `wasm32-unknown-browser-wasm` | 通用浏览器宿主标准 WASM 模块 | 导出逻辑入口，不产生 `_start`，可附带 JS glue |
| `wasm32-node-unknown-wasm` | Node.js 宿主标准 WASM 模块 | Node 宿主 glue 与验证脚本 |
| `wasm32-deno-unknown-wasm` | Deno 宿主标准 WASM 模块 | Deno 宿主 glue 与权限模型 |
| `wasm32-bun-unknown-wasm` | Bun 宿主标准 WASM 模块 | Bun 宿主 glue 与运行脚本 |

### 6.5 WASI

| CanonicalTarget | 含义 | 常用运行约定 |
|:---|:---|:---|
| `wasm32-unknown-wasi-wasip1` | WASI Preview 1 | 入口 `_start` |
| `wasm32-unknown-wasi-wasip2` | WASI Preview 2 / Component Model | 按 component model 生成入口 |

### 6.6 原生平台

| CanonicalTarget | 含义 |
|:---|:---|
| `x86_64-pc-windows-msvc` | Windows x64 MSVC |
| `x86_64-pc-windows-gnu` | Windows x64 MinGW/GNU |
| `x86_64-unknown-linux-gnu` | Linux x64 GNU |
| `aarch64-unknown-linux-gnu` | Linux ARM64 GNU |
| `aarch64-apple-darwin` | macOS ARM64 |

### 6.7 SPIR-V

| CanonicalTarget | 含义 |
|:---|:---|
| `spirv-unknown-vulkan` | Vulkan SPIR‑V 着色器模块 |

---

## 7. 运行时常用约定（派生属性）

### 7.1 JVM 族

- 标准 ABI `managed` → 输出 `.class` 或 `.jar`，入口 `public static void main(String[])`
- 标准库映射：`std.io.print` → `java.io.PrintStream.println`
- `dex` ABI → Android 专用打包
- `native` ABI → GraalVM 原生映像

### 7.2 .NET 族

- 标准 ABI `managed` → 输出 `.dll`/`.exe`，入口合法 `Main`
- 常见 sidecar：`.runtimeconfig.json`、`.deps.json`、`.pdb`
- 标准库映射：`std.io.print` → `System.Console.WriteLine`
- `nativeaot` → 生成原生可执行文件，无 JIT
- `il2cpp` → 交由 Unity IL2CPP 转换成 C++ 再编译

### 7.3 WASM 浏览器与 JS 宿主

- 标准 ABI `wasm` → 输出 `.wasm` 二进制模块，导出入口
- 不自动生成 `_start`（除非 WASI）
- JS glue 由 build 系统根据 `spec` 和 `impl` 决定，是 CanonicalTarget 的派生行为
- `browser` 规范 → 浏览器 Web API
- `node` / `deno` / `bun` 规范 → 各自 API 集合与加载方式

### 7.4 WASI

- `wasip1` 入口 `_start`，`std.io.print` 绑定 `fd_write`
- `wasip2` 采用 component model 入口，不能假定 `wasi_snapshot_preview1`

---

## 8. 短别名注册表

短别名仅为用户输入便捷，内部立即展开为 CanonicalTarget，不拥有独立身份。

| 短别名 | 展开为 CanonicalTarget |
|:---|:---|
| `nyar` | `nyar-unknown-native` |
| `gnosis` | `gnosis-unknown-native` |
| `wasm` | `wasm32-unknown-browser-wasm` |
| `node` | `wasm32-node-unknown-wasm` |
| `deno` | `wasm32-deno-unknown-wasm` |
| `bun` | `wasm32-bun-unknown-wasm` |
| `wasip1` | `wasm32-unknown-wasi-wasip1` |
| `wasip2` | `wasm32-unknown-wasi-wasip2` |
| `clr` | `clr-microsoft-windows-managed` |
| `jvm` | `jvm-openjdk-linux-managed` |

---

## 9. 设计原则

### 9.1 `arch`-`impl`-`spec`-`abi` 的职责分离

- **`spec` 是规范，`impl` 是实现**。大多数规范只有一个主流实现，因此日常使用中可以不刻意区分它们，但字段保留区分是为了未来的多实现共存场景。
- **`arch` 是执行模型，不是 CPU 名**。这样才能统一容纳原生、托管、WASM、着色器等不同后端。
- **`abi` 是产物与调用契约，不是文本表示**。WAT、MSIL 汇编等属于开发者工具，不占用目标身份。

### 9.2 消灭第二身份

目标差异必须直接体现在 CanonicalTarget 的四个字段上，不再允许出现 `ProfileId`、`HostKind`、`RuntimeProfile` 等并列键。唯一的例外是派生属性（如入口约定），它们只读地从 CanonicalTarget 计算得来。

### 9.3 `managed` 而不是 `il` 或重复 `arch` 名

- 产物不是文本 IL，而是二进制程序集
- `managed` 清晰表达“标准托管执行”，与 `nativeaot`、`dex` 直接对比
- 彻底消除 `clr-microsoft-windows-clr` 式的同字段重名

### 9.4 `wasm` 而不是 `webassembly` 作为标准 ABI

- `wasm` 精确指代 `.wasm` 二进制模块
- 文本格式 WAT、JS glue、WASI 接口都由其他维度处理，不堆积在 ABI 一词上
- `wasip1`、`wasip2` 作为更具体的 ABI 继续使用

---

## 10. 常见问题 (FAQ)

**Q: 为什么还叫“目标三元组”？不是四段吗？**
A: 出于历史习惯，我们仍将整体标识称为三元组。实际的语义结构是 `arch-impl-spec-abi` 四段，其中 `abi` 在多数情况下有默认值，可以省略输入，但内部总是补全。

**Q: 为什么 `arch` 可以是 `clr` 或 `jvm`？这不是 CPU 架构。**
A: `arch` 代表执行模型，即代码将在哪种引擎上运行。原生 CPU、CLR、JVM、WebAssembly 都是同等地位的执行后端。

**Q: `managed` 和 `il` 有什么区别？为什么不用 `il`？**
A: 标准托管模式下，产物是 `.dll`/`.exe` 或 `.class` 等二进制容器，而不是人类可读的 IL 汇编。`managed` 描述的就是这种标准 JIT 执行模式。如果将来需要输出文本 IL，会使用编译器选项（如 `--emit-msil`），而不会混入目标标识。

**Q: `node`、`deno`、`bun` 为什么是 `spec` 而不是 `impl`？**
A: 它们各自定义了开发者可见的 API 规范（全局对象、模块加载等），因此适合作为接口规范（`spec`）。如果一个规范有多个兼容实现，`impl` 字段就会派上用场；否则 `impl` 填 `unknown` 即可。

**Q: DLR (Dynamic Language Runtime) 需要新 `arch` 吗？**
A: 不需要。DLR 是运行在 CLR 之上的库，产物仍是 IL，仍属于 `clr` 执行模型。同理，所有基于现有执行模型的框架都无需新增 `arch`，只需通过 `spec` 或编译策略表达差异。

**Q: SPIR-V 目标为什么写成 `spirv-unknown-vulkan`？**
A: `vulkan` 是着色器接口规范，放在 `spec` 位置；`impl` 当前无具体实现区分，用 `unknown`。这与 `spec` = 规范、`impl` = 实现的原则一致。

**Q: 原生平台为什么还有 `impl` 比如 `pc`、`apple`？**
A: 原生平台的工具链与运行时事实上有不同的提供者（GNU 兼容工具链、Apple 平台 SDK 等），所以 `impl` 信息有意义。当不需要区分时，可使用 `unknown`（如 `x86_64-unknown-linux-gnu`）。

**Q: `native` 作为 `arch` 是什么意思？**
A: `arch=native` 表示编译时自动检测宿主机执行模型，主要用于开发阶段的快速迭代或工具链脚本，只在 `--target native` 时可用，编译器内部并不存在该枚举。