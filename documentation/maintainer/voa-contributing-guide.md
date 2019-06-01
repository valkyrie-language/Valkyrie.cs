# VOA 贡献者指南

> VOA = Valkyrie of Asgard — 纯 GGScript 全栈框架

## 项目结构

```
Valkyrie.cs/
├── projects/
│   ├── VoaCore/              # 核心库（17 组件 + 12 标准函数 + 5 WASI 模块）
│   ├── VoaRouter/            # 文件路由引擎
│   ├── VoaEffect/            # Effect 系统
│   ├── VoaApi/               # API Routes 后端
│   ├── VoaAuth/              # 认证包
│   ├── VoaI18n/              # 国际化包
│   ├── VoaAnalytics/         # 埋点包
│   ├── VoaSeo/               # SEO 包
│   ├── Valkyrie.ToolChains/  # CLI / DevServer / Compiler / PWA 生成器
│   ├── Asgard.Tests/         # 测试套件（150+ 测试）
│   └── Valkyrie/             # CLI 入口
├── documentation/
│   ├── toolchain/            # VOA 工具链文档
│   └── language/extensions/  # AWSL 语言文档
└── examples/
    ├── voa-core/             # 核心库示例
    ├── voa-router/           # 路由器示例
    ├── voa-effect/           # Effect 示例
    └── fullstack/            # 全栈示例
```

## 开发流程

### 环境配置

```bash
# 安装 .NET 9 SDK
winget install Microsoft.DotNet.SDK.9
```

### 常用命令

| 命令 | 说明 |
|:---|:---|
| `dotnet build Valkyrie.cs/projects/Valkyrie.ToolChains/` | 构建工具链 |
| `dotnet test Valkyrie.cs/projects/Asgard.Tests/` | 运行测试 |
| `dotnet run --project Valkyrie.cs/projects/Valkyrie/ -- voa dev` | 启动 DevServer |

### 代码规范

- 所有注释使用中文
- 以上方注释为主，禁止行尾注释
- `if`/`for`/`while` 后必须使用大括号（Allman 风格）
- 字符串优先使用 `$""` 插值
- 不捕获通用 `Exception`

### GGScript 规范

- 文件名使用 kebab-case（`web-dialect.v`）
- 函数名使用 snake_case（`handle_click`）
- 结构体名使用 PascalCase（`VoaApiRequest`）
- `pub` 函数需要文档注释

### AWSL 规范

- 组件 Props 添加类型标注
- `<style scoped>` 使用 CSS 变量（`var(--voa-xxx)`）
- 组件目录：`widget` / `script` / `style` 三段式

## 提交规范

使用 Git Emoji 规范：
```
✨ 新增 DataTable 组件
- 支持排序/加载/空状态
- 响应式尺寸 small/medium/large
```

## 测试要求

- 新组件提交需附带交互测试（≥ 3 个）
- 新函数需含签名验证测试
- 修改渲染器需更新 SSR 测试

## 上下游职责边界

| 层级 | 团队 | 职责 | 本团队禁止 |
|:---|:---|:---|:---|
| 文本编解码 | 02-Oak | Lexer/Parser/AST | ❌ 自建 Parser |
| 二进制编解码 | 01-Acorn | Encoder/Decoder | ❌ 自建二进制读写 |
| 分析优化 | 03-Nyar | EGraph/CodeGen/VM | ❌ 自建优化器 |
| **全栈框架** | **11-VOA** | **CLI/DevServer/组件/路由/API** | ❌ 不做后端框架 |
