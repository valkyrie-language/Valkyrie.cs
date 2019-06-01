# 依赖管理

## 依赖声明

在 `legion.von` 中声明依赖：

```
[dependencies]
some-lib = "2025.*"
another = "^1.2.3"
exact-lib = "1.0.0"
```

## 版本约束语法

| 语法 | 示例 | 说明 |
|:---|:---|:---|
| 精确版本 | `"1.2.3"` | 精确匹配 |
| 通配符 | `"2025.*"` | 匹配 2025 年任意版本 |
| 通配符 | `"1.*"` | 匹配 1.x 任意版本 |
| 插入符 | `"^1.2.3"` | `>=1.2.3 <2.0.0` |
| 波浪线 | `"~1.2.3"` | `>=1.2.3 <1.3.0` |
| 大于等于 | `">=1.2.3"` | 大于等于指定版本 |
| 小于 | `"<2.0.0"` | 小于指定版本 |
| 范围 | `">=1.0.0 <2.0.0"` | 版本范围 |
| 最新 | `"latest"` | 总是使用最新版本 |

## 依赖解析流程

```
legion resolve
  → 读取 legion.von
  → 构建依赖图（DependencyGraph）
  → 递归解析每个依赖
       ├── 查询注册表（IRegistry.GetPackageAsync）
       ├── 解析版本约束
       ├── 检测版本冲突
       └── 递归解析传递依赖
  → 下载并缓存程序集（PackageCache）
  → 安装到 vendors/ 目录
  → 生成锁文件（lock.von）
```

## 锁文件（lock.von）

记录依赖解析的精确结果，确保构建可重现。应提交到版本控制系统。

```
version = "1"

[packages]
[packages."some-lib@2025.1.0.0"]
name = "some-lib"
version = "2025.1.0.0"
registry = "npm"
resolved = "https://registry.npmjs.org/some-lib/-/some-lib-2025.1.0.0.tgz"
integrity = "sha512-..."
dependencies = ["another-lib@2.0.0"]
```

## 传递依赖

Legion 递归解析所有传递依赖，共享相同版本：

```
my-package
├── dep-a@1.0.0
│   ├── dep-c@2.0.0
│   └── dep-d@1.0.0
└── dep-b@1.0.0
    ├── dep-c@2.0.0  ← 共享，不重复安装
    └── dep-e@3.0.0
```

## 版本冲突处理

1. 收集所有对同一包的版本请求
2. 尝试找到满足所有约束的版本
3. 若无法满足，报告冲突并提供诊断信息

## 依赖类型

| 类型 | 安装时机 | 传递性 |
|:---|:---|:---|
| `dependencies` | 生产 + 开发 | 传递给消费者 |
| `devDependencies` | 仅开发 | 不传递 |
| `peerDependencies` | 由宿主提供 | 不自动安装 |
| `optionalDependencies` | 可选安装 | 安装失败不阻塞 |

## vendors 目录规则

- 不递归：vendors 中的依赖不会自动查找其自身的 vendors
- 路径格式：`vendors/<registry>@<endpoint>/<org>.<package>@<version>/`
- 无组织包使用 `_` 作为组织名
- 作用域包的 `@` 替换为 `.`
