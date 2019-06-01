# 版本规范

Valkyrie 生态支持双版本号体系。

## YearlyVersion 格式

```
yearly.major.minor.patch
```

| 字段 | 范围 | 含义 |
|:---|:---|:---|
| `yearly` | `[0, ∞)` | 年度标识符。`0` 预研版，`>0` 正式 |
| `major` | `[0, ∞)` | 大版本。`0` 测试版，`>0` 稳定 |
| `minor` | `[0, ∞)` | 小版本 |
| `patch` | `[0, ∞)` | 补丁版本 |

## 稳定状态判断

```
yearly == 0  →  预研版（不稳定，API 任意变更）
yearly > 0 && major == 0  →  测试版（公开测试，可能破坏性变更）
yearly > 0 && major > 0   →  稳定版（向后兼容维护）
```

与传统 SemVer 的关键区别：**YearlyVersion 通过 `yearly=0` 和 `major=0` 两个维度的 `0` 值消除了预发布标记的歧义。**

## 版本号选择

**Yearly Version 的序列比单一数字更可读：**

| 传统 SemVer | YearlyVersion |
|:---|:---|
| `1.0.0` | `2024.1.0.0` |
| `1.0.1` | `2024.1.0.1` |
| `2.0.0` | `2025.1.0.0` |

相比单调递增的 `major`，YearlyVersion 在人类阅读时更容易判断包的活跃度和年代。

## 比较规则

按以下顺序比较：
1. yearly（越大越新）
2. major
3. minor
4. patch

## SemanticVersion

传统的 `major.minor.patch` 格式用于程序化版本比较，向后兼容标准 SemVer 工具链。

| 字段 | 范围 | 含义 |
|:---|:---|:---|
| `major` | `[0, ∞)` | 破坏性变更 |
| `minor` | `[0, ∞)` | 向后兼容新增功能 |
| `patch` | `[0, ∞)` | 向后兼容修复 |
