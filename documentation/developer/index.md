# 参与贡献

## 项目结构

```
Valkyrie.cs/
├── projects/
│   ├── Valkyrie/                  # CLI 入口
│   ├── Valkyrie.Compiler/        # 正确的新编译器骨架：HIR/MIR/LIR + multi target
│   ├── Valkyrie.Runtime/         # NyarVM 执行封装与兼容层
│   ├── Valkyrie.ToolChains/      # 工具链统一入口
│   ├── Valkyrie.TypeChecker/     # 类型检查
│   ├── Valkyrie.Formatter/       # 代码格式化
│   ├── Legion/                   # 包管理器核心
│   ├── Legion.Registry.Valhalla/ # Valhalla 注册表适配器
│   ├── Valhalla/                 # 注册表共享核心
│   ├── Valhalla.Client/          # 注册表客户端
│   ├── Valhalla.Config/          # 注册表配置
│   ├── Valhalla.Server/          # 注册表服务端
│   ├── Valkyrie.Tests/           # 核心测试
│   ├── Asgard.Tests/             # VOA 测试
│   ├── Legion.Tests/             # 包管理测试
│   └── Valhalla.Tests/           # 注册表测试
├── examples/
├── runtime/
│   └── voa-runtime.js
├── documentation/
└── Valkyrie.slnx
```

## 15 个项目的职责

| 项目 | 职责 | 依赖 |
|:---|:---|:---|
| **Valkyrie** | CLI 入口 | Compiler + Runtime + ToolChains |
| **Valkyrie.Compiler** | 新编译主线：`AST -> HIR -> MIR -> LIR -> multi target` | Oak + Nyar + Acorn + TypeChecker |
| **Valkyrie.Runtime** | `NyarVM` 执行封装、`.nyar` / `Nyar Standard IR` 装载与运行 | NyarVM + Acorn.Nyar |
| **Valkyrie.ToolChains** | 工具链统一入口，分发到子工具 | Compiler + Runtime |
| **Valkyrie.TypeChecker** | 类型检查 | Oak |
| **Valkyrie.Formatter** | 代码格式化 | Oak AST |
| **Legion** | 包管理器核心 | Runtime |
| **Legion.Registry.Valhalla** | Valhalla IRegistry 适配器 | Legion + Valhalla |
| **Valhalla** | 注册表共享核心库 | Acorn.Nyar |
| **Valhalla.Client** | 下载、安装、锁文件校验 | Valhalla |
| **Valhalla.Config** | 配置模型 | Valhalla |
| **Valhalla.Server** | HTTP 服务端 | Valhalla + ASP.NET |
| **Valkyrie.Tests** | 语言/编译器测试 | Compiler + Runtime + TypeChecker |
| **Asgard.Tests** | VOA 框架测试 | Compiler + Runtime + ToolChains |
| **Legion.Tests** | 包管理器测试 | Legion |
| **Valhalla.Tests** | 注册表测试 | Valhalla |

## 构建

```bash
dotnet build
dotnet test
```

## 关键设计文档

- [架构详解](architecture.md)
- [Target Contract Spec](target-contract-spec.md)

## 开发环境

- .NET SDK 9.0+
- Rider 或 VS Code
- 同级目录需有 Oak.cs、Acorn.cs、NyarVM.cs

## 代码审查清单

- [ ] 遵循 [编码规范](coding-conventions.md)
- [ ] 遵循 [依赖规则](dependency-rules.md)
- [ ] 编写测试覆盖新功能
- [ ] 全部现有测试通过
- [ ] 文档注释完整（中文，XML 文档注释格式）
- [ ] 无 console.log / TODO 残留
