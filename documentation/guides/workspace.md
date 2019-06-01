# 工作区

工作区（Workspace）是多项目管理机制，使用 `legions.von` 统一管理相互依赖的多个子包。

## 配置文件

```
version = "1"
name = "my-workspace"

members = [
    "packages/core",
    "packages/utils",
    "packages/web",
]

[dependencies]
common-lib = "2025.*"

[scripts]
build = "legion run build --all"
test = "legion run test --all"
```

## 字段说明

| 字段 | 类型 | 必填 | 说明 |
|:---|:---|:---|:---|
| `version` | `string` | 是 | 工作区配置格式版本 |
| `name` | `string` | 否 | 工作区名称 |
| `members` | `string[]` | 否 | 子包路径列表 |
| `dependencies` | `dict` | 否 | 工作区级别共享依赖 |
| `scripts` | `dict` | 否 | 工作区级别脚本 |

## 自动成员发现

Legion 自动扫描 `packages/` 和 `projects/` 目录，发现包含 `legion.von` 的子目录并注册为工作区成员。无需手动维护 `members` 列表。

## 目录结构

```
my_workspace/
├── legions.von
├── packages/
│   ├── core/
│   │   ├── legion.von
│   │   └── src/
│   ├── utils/
│   │   ├── legion.von
│   │   └── src/
│   └── web/
│       ├── legion.von
│       └── src/
├── vendors/
└── lock.von
```

## 命令

```bash
legion workspace list          # 列出所有成员
legion workspace add <path>    # 添加成员
legion workspace remove <path> # 移除成员
legion workspace build-order   # 显示拓扑排序的构建顺序
```

### 构建顺序

```bash
legion workspace build-order
```

```
1. core          （无内部依赖）
2. utils         （依赖 core）
3. web           （依赖 core、utils）
```

## 共享依赖

工作区声明的共享依赖自动被所有子包继承：

```
# legions.von
[dependencies]
common-lib = "2025.*"
```

## 脚本传播

```bash
legion run test --all    # 在所有子包中执行 test 脚本
legion run build --all   # 在所有子包中执行 build 脚本
```

## VOA 工作区

VOA 使用 `voa.workspace.v` 配置工作区：

```v
workspace {
    name = "my-workspace"
    projects = ["project-1", "project-2", "shared"]

    shared {
        toolchain {
            valkyrie_version = "^1.0.0"
            oak_version = "^1.0.0"
            acorn_version = "^1.0.0"
        }
    }
}
```
