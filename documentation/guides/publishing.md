# 包发布

## 发布流程

```
源代码 → 构建 → 打包（legion pack） → 发布（legion publish） → 注册表
```

## 准备工作

### 确保清单完整

```
name = "my-package"
version = "2025.1.0.0"
description = "我的包"
license = "MIT"
author = "作者名"
```

### 配置 files 字段

控制包含在包中的文件：

```
files = [
    "dist/",
    "README.md",
    "LICENSE"
]
```

### 执行构建

```bash
legion run build
```

## 认证

```bash
legion login                          # 登录默认注册表
legion vendor login npm               # 登录指定 Vendor
legion vendor login npm --token xxx   # 传入令牌
legion whoami                         # 验证登录状态
```

## 打包

```bash
legion pack
```

## 模拟发布

```bash
legion publish --dry-run
```

```
📦 即将发布 my-package@2025.1.0.0
  包含 5 个文件:
    dist/index.js (12.3 KB)
    legion.von (342 B)
    README.md (856 B)
    LICENSE (1.1 KB)
  注册表: npm
  总大小: 14.5 KB
```

## 发布

```bash
legion publish                          # 发布到默认注册表
legion publish --registry npm           # 指定注册表
legion publish --tag beta               # 添加发布标签
legion publish --access public          # 公开访问
```

| 选项 | 说明 |
|:---|:---|
| `--registry <name>` | 指定目标注册表 |
| `--tag <tag>` | 发布标签（如 `beta`、`next`） |
| `--dry-run` | 模拟发布 |
| `--access public` | 公开访问 |
| `--access restricted` | 受限访问 |

## 版本管理

```bash
legion version patch        # 2025.1.0.0 → 2025.1.0.1
legion version minor        # 2025.1.0.1 → 2025.1.1.0
legion version major        # 2025.1.1.0 → 2025.2.0.0
legion publish
```

## 撤回与弃用

```bash
legion unpublish my-package@2025.1.0.0
legion deprecate my-package@2025.1.0.0 "请使用 >=2025.2.0.0"
```

> 建议使用 `--deprecate` 替代直接撤回，避免破坏下游。
