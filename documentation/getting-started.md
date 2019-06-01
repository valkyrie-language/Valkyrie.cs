# 快速开始

## 环境要求

- .NET SDK 9.0+
- Git
- 同级目录需有 Oak.cs、Acorn.cs、NyarVM.cs

## 构建

```bash
git clone <repo>
cd Valkyrie.cs
dotnet build
dotnet test
```

## 三个入口

Valkyrie 提供三个命令行工具，按需选择：

| 工具 | 用途 | 命令 |
|:---|:---|:---|
| `vcc` | 编译与运行 Valkyrie 代码 | `vcc compile src/main.v` |
| `legion` | 包管理与依赖管理 | `legion add some-package` |
| `voa` | VOA 全栈框架 | `voa dev` |

三者的关系：`legion` 和 `voa` 内部调用 `vcc`（或 ValkyrieRuntime API）进行编译。

## 第一个 Valkyrie 程序

### 函数与变量

```valkyrie
micro fibonacci(n: i32) -> i32 {
    if n <= 1 {
        return n
    }
    return fibonacci(n - 1) + fibonacci(n - 2)
}

let result = fibonacci(10)
println("fib(10) = {result}")
```

### 控制流

```valkyrie
let mut count = 0
loop i in 0..10 {
    if i % 2 == 0 {
        count += 1
    }
}
println("偶数个数: {count}")
```

### 模式匹配

```valkyrie
match value {
    case 0: "zero"
    case x if x > 0: "positive"
    else: "negative"
}
```

### ECS 组件与系统

```valkyrie
component Position {
    x: f32;
    y: f32;
}

component Velocity {
    dx: f32;
    dy: f32;
}

system MovementSystem {
    query all = Query.all(Position, Velocity);

    on_update(frame: Frame) {
        loop entity in query.all {
            entity.position.x += entity.velocity.dx * frame.dt;
            entity.position.y += entity.velocity.dy * frame.dt
        }
    }
}
```

## 使用编译器和包管理器

### 快速执行脚本

```bash
legion exec "vcc compile -e 'print(\"Hello\")'"
```

### 初始化项目

```bash
legion init my_project
cd my_project
```

这会在当前目录创建 `legion.von` 和必要的目录结构。

### 添加依赖

```bash
legion add voa-core
```

Legion 自动解析依赖、下载程序集、放入 `vendors/` 目录。

### 运行脚本

在 `legion.von` 中定义脚本：

```
[scripts]
build = "vcc compile src/main.v"
test = "vcc test src/"
```

然后：

```bash
legion run build
legion run test
```

## 使用 VOA 全栈框架

### 创建 VOA 项目

```bash
mkdir my_app && cd my_app
voa init
```

### 启动开发服务器

```bash
voa dev
```

开发服务器提供热重载、错误覆盖层和 WASM 按需编译。

### 构建生产版本

```bash
voa build --env=production
```

## 语言基础速览

| 概念 | 语法 | 说明 |
|:---|:---|:---|
| 不可变变量 | `let x = 1` | 默认不可变 |
| 可变变量 | `let mut x = 1` | 显式声明可变 |
| 函数 | `micro name() { }` | 轻量级函数 |
| 结构体 | `structure Name { }` | 不可变值类型 |
| 类 | `class Name { }` | 类类型，不意味着装箱 |
| 组件 | `component Name { }` | ECS 纯数据容器 |
| 系统 | `system Name { }` | ECS 逻辑处理器 |
| 遍历 | `loop x in xs { }` | 统一迭代语法 |
| 条件 | `if cond { } else { }` | 条件分支 |
| 模式匹配 | `match val { case ... }` | 结构化匹配 |
| 特性标注 | `[attr]` | 元数据标注 |
| 异步 | `expr.await` | 后缀风格 |
| 注释 | `# 行注释` / `<# 块注释 #>` | 支持嵌套块注释 |
| 元代码 | `<% code %>` | 编译期代码生成 |

## 下一步

- 深入语言参考 → [语言参考](language/index.md)
- 了解工具链全貌 → [工具链总览](toolchain/index.md)
- 查看完整项目结构 → [架构详解](contributing/architecture.md)
