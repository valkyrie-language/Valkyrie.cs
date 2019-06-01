# Legion 包管理器

Legion 是 Nyar 组织的通用包管理器，提供依赖管理、版本控制、多目标编译、包发布、安全审计和脚本执行能力。

> 军团，为秩序而战。

## 核心概念

Legion 管理的是**程序集**的依赖关系图，而不仅仅是包。包是程序集的打包容器，真正分发给消费者的是编译好的程序集。

| 概念 | 说明 |
|:---|:---|
| **程序集** | 编译后的分发单元（`.dll`、`.wasm`、`.jar` 等），是真正被链接和加载的实体 |
| **包** | 程序集的打包容器，包含元数据（版本、依赖声明）和程序集文件 |
| **micro 函数** | 标记 `[main]` 的入口函数，每个 `micro` 编译为独立二进制产出 |
| **注册表** | 程序集的来源，Legion 适配 npm、jsr、conda、NuGet、Maven 等多种注册表 |
| **vendors** | 本地依赖存放目录，VCC 直接从此目录读取依赖 |

## 三种工作模式

| 模式 | 说明 | 配置文件 |
|:---|:---|:---|
| **Workspace** | 多包工作区，统一管理相互依赖的多个子包 | `voa.workspace.v` |
| **Package** | 单包开发，独立库或应用 | `legion.von` |
| **Script** | 一次性脚本执行，无需配置文件 | 无 |

## 快速开始

```bash
legion init my_project
cd my_project
legion add some-package
legion build
legion run hello
```

## 配置文件格式

Legion 使用 `.von` 格式（Valkyrie Object Notation），一种 JSON 兼容的结构化文本格式：

```
name = "my-package"
version = "0.0.0"

[dependencies]
some-package = "^1.0"

[scripts]
build = "vcc compile source/main.v"
test = "vcc test source/"
```

## 清单文件（legion.von）

### 完整字段

```
name = "my-package"
version = "2025.1.0.0"
description = "我的包"
license = "MIT"
author = "作者名"
homepage = "https://example.com"
repository = "https://github.com/user/repo"

main = "source/main.v"

# 构建目标列表，每个元素指定一个 canonical triple
[build]
{target: "wasm32-unknown-browser"}
{target: "wasm32-unknown-wasi-wasip1"}

[dependencies]
some-package = "^1.0"

[devDependencies]
test-framework = "latest"

[scripts]
build = "vcc compile source/main.v"
test = "vcc test source/"

# 生命周期钩子
[hooks]
[hooks.preBuild]
command = "echo 开始构建"
failOnError = false

[hooks.postBuild]
command = "legion verify"
shell = "pwsh"

# 发布配置
[publishConfig]
registry = "valhalla"
access = "public"
tag = "latest"
```

### 构建目标（build 字段）

`legion.von` 的 `build` 字段定义了**多目标编译**的 canonical triple 列表。运行 `legion build` 时，Legion 自动读取此列表，为每个目标独立编译。

目标使用**目标三元组**（Target Triple）格式：`arch-vendor-os[-abi]`，完整定义见[目标三元组规范](target-triples.md)。

同时也支持短别名，VCC 会自动展开为对应的三元组：

| 短别名 | 展开为 | 说明 |
|:---|:---|:---|
| `nyar` | `nyar-unknown-native` | NyarVM 字节码（默认） |
| `gnosis` | `gnosis-unknown-native` | GnosisVM 字节码 |
| `wasm` | `wasm32-unknown-browser` | WebAssembly（浏览器） |
| `node` | `wasm32-unknown-node` | WebAssembly（Node.js） |
| `deno` | `wasm32-unknown-deno` | WebAssembly（Deno） |
| `bun` | `wasm32-unknown-bun` | WebAssembly（Bun） |
| `wasip1` | `wasm32-unknown-wasi-wasip1` | WASI Preview 1 |
| `wasip2` | `wasm32-unknown-wasi-wasip2` | WASI Preview 2 |
| `clr` | `clr-microsoft-windows` | .NET CLR 程序集 |
| `jvm` | `jvm-openjdk-linux` | JVM 类文件 |
| `native` | 宿主平台对应的 canonical triple | 原生二进制 |

未指定 `build` 字段时默认编译目标为 `nyar`。

示例——使用完整三元组：

```von
[build]
{target: "wasm32-unknown-browser"}
{target: "wasm32-unknown-node"}
{target: "wasm32-unknown-wasi-wasip1"}
{target: "clr-unity-windows-il2cpp"}
```

示例——使用短别名：

```von
[build]
{target: "nyar"}
{target: "wasm"}
```

### 入口函数（micro + [main]）

源代码中标记 `[main]` 的 `micro` 函数被视为入口函数，每个函数编译为独立二进制：

```v
#? ./hello_world.exe
[main]
micro hello_world() {
    print("Hello, World!")
}

#? ./hello_world_utf8.exe
[main]
micro hello_world_utf8() {
    print("你好，世界！")
}
```

`#?` 注释指令指定输出文件名。无 `#?` 指令时以函数名作为输出文件名。

### 四种依赖类型

| 类型 | 字段 | 说明 |
|:---|:---|:---|
| 运行时依赖 | `dependencies` | 生产环境必需的依赖 |
| 开发依赖 | `devDependencies` | 仅开发时需要 |
| 对等依赖 | `peerDependencies` | 宿主项目提供的依赖 |
| 可选依赖 | `optionalDependencies` | 可选功能依赖 |

## 命令参考

### 构建

| 命令 | 说明 |
|:---|:---|
| `legion build` | 从 `legion.von` 读取 `build` 字段，执行多目标编译 |
| `legion build <path>` | 编译指定项目 |
| `legion build <path> --target wasm` | 编译指定目标 |
| `legion build --incremental` | 增量编译（仅编译变更文件） |
| `legion build --verbose` | 详细输出 |

`legion build`（无参数）的行为：
1. 查找当前目录的 `legion.von`
2. 读取 `build` 字段中的目标列表
3. 为每个目标调用编译器，输出到 `dist/<target>/` 目录
4. 产物包含所有 `[main] micro` 函数的编译结果

### 运行

| 命令 | 说明 |
|:---|:---|
| `legion run <script>` | 执行清单中定义的脚本 |
| `legion run <micro>` | 编译并运行 `[main] micro` 入口函数 |
| `legion run --nyar <file>` | 运行 .nyar 字节码文件 |
| `legion run --list` | 列出所有可用脚本和入口函数 |
| `legion run --hook <name>` | 执行生命周期钩子 |
| `legion run --env KEY1=VALUE1,KEY2=VALUE2` | 带环境变量执行 |

**执行优先级**：脚本 > micro 入口函数。当名称同时匹配脚本和 micro 函数时，优先执行脚本。

### 脚本快捷方式

```bash
# 定义在 script/ 目录或 legion.von scripts 字段的脚本可直接调用
legion hello          # 执行 script/hello.v 或 scripts.hello
legion build          # 与内置命令同名时，脚本优先
legion test           # 执行 scripts.test
```

**`legion <script>` 优先级高于内置命令**。如果定义了名为 `build` 的脚本，`legion build` 将执行脚本而非内置构建命令。此时需使用 `legion build .` 显式调用内置构建。

### 脚本自动发现

`script/` 目录下的 `.v` 文件自动注册为脚本，文件名（不含扩展名）即为脚本名：

```
my-package/
├── legion.von
├── script/
│   ├── hello.v       # → legion hello
│   ├── deploy.v      # → legion deploy
│   └── ci/
│       └── lint.v    # → 不会自动发现（仅扫描一级）
└── source/
    └── main.v
```

手动在 `legion.von` 的 `scripts` 字段定义的脚本优先级高于自动发现。

### 依赖管理

| 命令 | 说明 |
|:---|:---|
| `legion install` | 安装所有依赖 |
| `legion install <pkg>` | 安装指定包并添加到 `legion.von` |
| `legion install <pkg> --dev` | 安装为开发依赖 |
| `legion install --frozen-lockfile` | 严格按锁文件安装（CI 模式） |
| `legion remove <pkg>` | 移除依赖 |
| `legion update` | 更新依赖到最新兼容版本 |
| `legion update <pkg>` | 更新指定包 |
| `legion update --interactive` | 交互式选择更新 |
| `legion outdated` | 检查过期依赖 |
| `legion info <pkg>` | 查看包详细信息 |

### 工作区

| 命令 | 说明 |
|:---|:---|
| `legion workspace list` | 列出所有成员 |
| `legion workspace info` | 查看工作区详细信息 |
| `legion workspace add <path>` | 添加成员 |
| `legion workspace remove <path>` | 移除成员 |
| `legion workspace graph` | 显示依赖拓扑图 |

工作区内部依赖使用 `workspace:*` 协议，通过符号链接而非下载安装。

### 安全与完整性

| 命令 | 说明 |
|:---|:---|
| `legion verify` | 验证已安装包的完整性（SHA256 校验） |
| `legion verify --fix` | 验证并自动修复缺失/损坏的包 |
| `legion audit` | 审计所有依赖的安全性（漏洞 + 许可证） |
| `legion audit --fix` | 自动修复可修复的漏洞 |
| `legion audit --json` | JSON 格式输出审计报告 |

### 包发布

| 命令 | 说明 |
|:---|:---|
| `legion publish` | 发布包到默认注册表 |
| `legion publish --registry <name>` | 发布到指定注册表 |
| `legion publish --dry-run` | 模拟发布 |
| `legion publish --bump patch` | 发布时自动递增版本号（patch / minor / major） |

### 配置管理

| 命令 | 说明 |
|:---|:---|
| `legion config list` | 列出所有配置项 |
| `legion config get <key>` | 获取配置项 |
| `legion config set <key> <value>` | 设置配置项 |
| `legion config delete <key>` | 删除配置项 |

### Vendor 认证

| 命令 | 说明 |
|:---|:---|
| `legion vendor login <name>` | 登录到指定 Vendor |
| `legion vendor logout <name>` | 退出登录 |
| `legion vendor list` | 列出所有已配置 Vendor |
| `legion vendor whoami <name>` | 查看当前登录用户 |

### 注册表管理

| 命令 | 说明 |
|:---|:---|
| `legion registry list` | 列出注册表 |
| `legion registry add <type> <url>` | 添加注册表 |
| `legion registry remove <type>` | 移除注册表 |
| `legion registry info <type>` | 查看注册表详情 |

### 缓存管理

| 命令 | 说明 |
|:---|:---|
| `legion cache list` | 列出缓存包 |
| `legion cache clean` | 清理过期缓存 |
| `legion cache verify` | 验证缓存完整性 |
| `legion cache size` | 查看缓存占用空间 |
| `legion cache path` | 显示缓存路径 |

### 代码质量

| 命令 | 说明 |
|:---|:---|
| `legion fmt` | 格式化代码 |
| `legion fmt --check` | 仅检查格式 |
| `legion check` | 类型检查 |

## 生命周期钩子

`legion.von` 的 `hooks` 字段定义在特定事件前后执行的命令：

| 钩子 | 触发时机 |
|:---|:---|
| `preBuild` / `postBuild` | `legion build` 前后 |
| `prePublish` / `postPublish` | `legion publish` 前后 |
| `preInstall` / `postInstall` | `legion install` 前后 |
| `prePack` / `postPack` | `legion pack` 前后 |
| `preTest` / `postTest` | 测试前后 |

钩子定义格式：

```von
[hooks.prePublish]
command = "legion test"
failOnError = true          # 失败时中断流程
shell = "pwsh"              # 使用的 Shell（默认系统 Shell）
condition = "!linux"        # 平台条件：仅非 Linux 执行
description = "发布前运行测试"
```

## 支持注册表

| 注册表 | 协议 |
|:---|:---|
| npm | `npm@registry.npmjs.org` |
| jsr | `jsr@jsr.io` |
| conda | `conda@repo.anaconda.com` |
| NuGet | `nuget@api.nuget.org` |
| Maven Central | `maven@search.maven.org` |
| Valhalla | `valhalla@valhalla.nyar.dev` |

## 锁文件（legion-lock.von）

锁文件记录精确的依赖关系和完整性哈希：

```von
[pkg.some-package@2.1.0]
name = "some-package"
version = "2.1.0"
registry = "npm"
resolved = "https://registry.npmjs.org/some-package/-/some-package-2.1.0.tgz"
integrity = "sha512-..."
dependencies = {dep-a = "^1.0"}

[pkg.workspace-pkg@0.0.0]
name = "workspace-pkg"
version = "0.0.0"
registry = "workspace"
resolved = "packages/my-lib"
isWorkspace = true
installPath = "workspace-pkg"
```

锁文件功能：
- `legion verify` — 验证所有包的 SHA256 完整性
- `legion install --frozen-lockfile` — CI 模式下锁文件不匹配时报错
- 版本漂移检测 — 检测 `legion.von` 约束与锁文件不一致

## 配置（~/.valkyrie/config.von）

全局配置项：

| 配置 | 说明 | 默认值 |
|:---|:---|:---|
| `registry` | 默认注册表 | `npm` |
| `proxy` | HTTP 代理 | 无 |
| `offline` | 离线模式 | `false` |
| `timeout` | 请求超时（秒） | `30` |
| `maxRetries` | 最大重试次数 | `3` |
| `strictSsl` | 严格 SSL 验证 | `true` |
| `verifyIntegrity` | 安装后验证完整性 | `true` |
| `maxParallelDownloads` | 最大并行下载数 | `8` |
| `cacheDirectory` | 自定义缓存目录 | `~/.valkyrie/cache` |

镜像源配置（`config.von` 中的 `mirrors` 字段）：
```von
[mirrors]
"registry.npmjs.org" = "https://registry.npmmirror.com"
```

## 与 VCC 的关系

Legion 调用 VCC 作为编译后端。VCC 不知道 Legion 的存在——VCC 只需要 `vendors` 目录中有依赖即可编译。

1. Legion 解析依赖声明，从注册表获取程序集
2. Legion 将程序集放入 `vendors` 目录
3. VCC 从 `vendors` 目录读取依赖进行编译

VCC 可独立使用而无需 Legion，这种设计确保了编译器与包管理器的严格分离。

## 环境变量

| 变量 | 说明 | 默认值 |
|:---|:---|:---|
| `VALKYRIE_HOME` | Legion 全局目录 | `~/.valkyrie` |
| `VALKYRIE_REGISTRY` | 默认注册表 | `npm` |
| `VALKYRIE_CACHE` | 缓存目录 | `$VALKYRIE_HOME/cache` |
| `LEGION_HOOK_NAME` | 钩子执行时注入的钩子名称 | 当前执行的钩子名 |
| `LEGION_HOOK_SHELL` | 钩子执行时注入的 Shell 配置 | 钩子定义的 Shell |
