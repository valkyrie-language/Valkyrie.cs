# 依赖规则

Valkyrie 严格遵守 Nyar 组织架构中的依赖方向规则。

## 允许的依赖方向

```
Oak.cs ──────→ 无外部依赖（纯文本编解码）
Acorn.cs ────→ 无外部依赖（纯二进制编解码）
NyarVM.cs ───→ Oak.cs + Acorn.cs
Valkyrie.cs ─→ Oak.cs + NyarVM.cs + Acorn.cs
```

## 禁止的依赖方向

- Oak.cs 不能依赖 NyarVM / Acorn / Valkyrie
- Acorn.cs 不能依赖 NyarVM / Oak / Valkyrie
- NyarVM 不能自建文本编解码（用 Oak）
- NyarVM 不能自建二进制编解码（用 Acorn）
- VCC 不能依赖 Legion / VOA / Valhalla
- 任何项目不能在 Oak 之外新建文本编解码器
- 任何项目不能在 Acorn 之外新建二进制编解码器

## Valkyrie 内部依赖

```
Valkyrie（CLI）
  ↓
Valkyrie.Runtime（管线编排）
  ↓
Oak.Valkyrie + Nyar.Core + Nyar.Optimizer + Acorn.Nyar

Valkyrie.TypeChecker → Oak.Valkyrie（不依赖 Nyar）
Valkyrie.Formatter → Oak.Valkyrie AST

Legion → Valkyrie.Runtime（编译）
Legion.Registry.Valhalla → Legion + Valhalla

Valhalla → Acorn.Nyar
Valhalla.Client → Valhalla
Valhalla.Config → Valhalla
Valhalla.Server → Valhalla + ASP.NET Core
```

## 编译器与包管理严格分离

这是最核心的依赖规则：

- **VCC** 不依赖任何包管理器
- **Legion** 调用 VCC，VCC 不知道 Legion 存在
- 替换包管理器不影响编译行为

两者通过 `vendors/` 目录通信，`vendors/` 格式是唯一的共享接口。
