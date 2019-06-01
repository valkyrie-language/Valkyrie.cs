# 项目配置

VOA 采用约定优于配置，大多数行为通过命名和目录结构自动推断。需要自定义时使用 `voa.config.v`。

## voa.config.v

```v
define_config(voa) {
    // 项目类型：frontend / backend / library
    project_type = "frontend"

    // 编译目标：wasm / clr / jvm / native / lib
    target = "wasm"

    // 服务配置（后端项目）
    server {
        host = "localhost"
        port = 3000
        workers = 0   // 0 = auto
        timeout = 30
    }

    // 构建配置
    build {
        output = "dist"
        minify = true
        sourcemap = true
    }

    // 热重载
    hot_reload {
        enabled = true
        watch = ["source/", "assets/"]
        ignore = [".git/", "node_modules/"]
        debounce = 100
    }

    // 语言配置
    language {
        awsl {
            strict_mode = false
            allow_template_tag = true
        }
    }
}
```

## 项目类型

| 值 | 说明 | 编译目标 |
|:---|:---|:---|
| `frontend` | 前端项目 | `wasm` |
| `backend` | 后端项目 | `clr` / `jvm` / `native` |
| `library` | 共享库 | `lib` |

## 编译目标

目标使用**目标三元组**（Target Triple）格式：`arch-vendor-os[-abi]`，完整定义见[目标三元组规范](../toolchain/target-triples.md)。

| 目标三元组 | 说明 | 用途 |
|:---|:---|:---|
| `wasm32-unknown-browser` | WebAssembly（浏览器） | 前端（默认） |
| `wasm32-unknown-node` | WebAssembly（Node.js） | 服务器侧 JS 宿主 |
| `wasm32-unknown-deno` | WebAssembly（Deno） | Deno 宿主 |
| `wasm32-unknown-bun` | WebAssembly（Bun） | Bun 宿主 |
| `clr-microsoft-windows` | .NET CLR（Windows） | 后端（优先） |
| `clr-unity-windows-il2cpp` | .NET CLR（Unity IL2CPP） | Unity |
| `jvm-openjdk-linux` | Java JVM | 后端（可选） |
| `x86_64-unknown-linux-gnu` | 原生二进制（Linux） | 后端（可选） |
| `lib` | 库 | 共享依赖 |

配置示例：

```v
target = "wasm32-unknown-browser"    // 前端
target = "clr-microsoft-windows" // 后端
target = "clr-unity-windows-il2cpp" // Unity
```

## 环境覆盖

```
voa.config.v              # 基础配置
voa.config.development.v  # 开发环境
voa.config.test.v         # 测试环境
voa.config.production.v   # 生产环境
```

## 环境变量

配置值可通过环境变量覆盖：

| 环境变量 | 对应配置 |
|:---|:---|
| `VOA_PROJECT_TYPE` | `project_type` |
| `VOA_TARGET` | `target` |
| `VOA_SERVER_PORT` | `server.port` |
| `VOA_SERVER_HOST` | `server.host` |
| `VOA_BUILD_OUTPUT` | `build.output` |
| `VOA_ENV` | 当前环境 |

优先级：环境变量 > 环境配置文件 > 基础配置 > 默认值。

## 全栈示例

### 前端

```v
define_config(voa) {
    project_type = "frontend"
    target = "wasm"
    build { output = "dist"; minify = true; sourcemap = true }
    hot_reload { enabled = true; watch = ["source/"] }
}
```

### 后端

```v
define_config(voa) {
    project_type = "backend"
    target = "clr"
    server { host = "0.0.0.0"; port = 8080; workers = 4 }
    build { output = "bin"; minify = true; sourcemap = false }
}
```

### 共享库

```v
define_config(voa) {
    project_type = "library"
    target = "lib"
    build { output = "lib"; minify = true; sourcemap = false }
}
```
