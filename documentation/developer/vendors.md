# Vendors 规范

`vendors/` 目录是 Legion 与 VCC 之间唯一的共享接口，两者通过它解耦。

## 路径格式

```
vendors/<registry>@<endpoint>/<org>.<package>@<version>/
```

| 元素 | 说明 | 示例 |
|:---|:---|:---|
| `registry` | 注册表名称 | `npm`、`maven`、`valhalla` |
| `endpoint` | 注册表端点 | `registry.npmjs.org`、`search.maven.org` |
| `org` | 组织名（无组织用 `_`） | `_`、`google`、`microsoft` |
| `package` | 包名（作用域 `@` 替换为 `.`） | `eslint`、`react`（原 `@types/react` → `types.react`） |
| `version` | 包版本 | `2025.1.0.0`、`1.0.0` |

## 路径示例

| 来源 | vendors 路径 |
|:---|:---|
| `npm install eslint` | `vendors/npm@registry.npmjs.org/_.eslint@9.0.0/` |
| `npm install @types/react` | `vendors/npm@registry.npmjs.org/types.react@18.0.0/` |
| `npm install @google/model-viewer` | `vendors/npm@registry.npmjs.org/google.model-viewer@3.5.0/` |
| `maven install com.google.guava` | `vendors/maven@search.maven.org/com.google.guava@33.0.0-jre/` |

## 目录结构

```json
{
    "name": "eslint",
    "version": "9.0.0",
    "registry": "npm",
    "resources": [
        { "type": "assembly", "path": "index.js" },
        { "type": "source", "path": "src/" }
    ]
}
```

## 不嵌套

VCC 只在 `vendors/` 的直接子目录中查找依赖，不递归。每个包只看到自己直接声明的依赖，不支持嵌套 `vendors/`。

## 优先级

VCC 按以下顺序查找依赖：

| 优先级 | 来源 | 说明 |
|:---|:---|:---|
| 1 | 当前项目 | 同项目中的其他模块 |
| 2 | `vendors/` | Legion 管理的依赖 |
| 3 | 全局 `vendors/` | 系统级全局依赖 |

## 设计原理

| 规则 | 理由 |
|:---|:---|
| `default()` 初始化 | `required` 字段需要语法支持的编译期强制赋值，不如直接赋默认值灵活 |
| 不递归 vendors | 避免嵌套，保持依赖扁平化 |
| 无组织名用 `_` | 区分作用域 `@` 替换为 `.` |
