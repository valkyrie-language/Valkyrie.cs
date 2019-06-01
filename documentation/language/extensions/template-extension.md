# Template 扩展

Valkyrie 语言的 Template 扩展提供元代码块语法，将 Valkyrie 语句模板化用于编译期代码生成和模板渲染。

## 设计理念

Valkyrie 核心语言提供 `<% ... %>` 元代码块，将已有的语句语法模板化：

| Valkyrie 语句                 | 模板化                                     | 说明                             |
| :-------------------------- | :-------------------------------------- | :----------------------------- |
| `if xxx { ... }`            | `<% if xxx %> ... <% end %>`            | if 语句 → if template node       |
| `loop item in list { ... }` | `<% loop item in list %> ... <% end %>` | loop 语句 → loop template node   |
| `match expr { ... }`        | `<% match expr %> ... <% end %>`        | match 语句 → match template node |
| `<% expr %>`                | 表达式求值                                   | 编译期求值并输出结果                     |

## 解析规则

### 词法分析

遇到 `<%` 后，查看下一个 token：

1. 如果是 keyword → 模板化语句（MetaBlockStart）
2. 如果不是 keyword → 表达式（MetaExpression）

```
<% if x > 0 %>          → keyword "if" → 模板化 if 语句
<% loop item in list %>  → keyword "loop" → 模板化 loop 语句
<% x + y %>             → 不是 keyword → 表达式求值
```

> **注意**：Valkyrie 不支持 `<%=`、`<%-`、`<%_` 等变体。因为 Valkyrie 不是空格敏感的语言，最终输出的是指令而非文本，空格控制没有意义。

### 节点类型

| 概念                    | 语法                              | 说明          |
| :-------------------- | :------------------------------ | :---------- |
| **if statement node** | `if xxx { ... }`                | Valkyrie 语句 |
| **if fragment node**  | `<% if xxx %>`                  | 模板化语句的开头片段  |
| **if template node**  | `<% if xxx %> ... <% end if %>` | 完整的模板化 if 块 |

`end` 是 template 模式下的关键词，用于闭合模板化语句块：

```
<% if condition %>
    ...
<% end %>

<% loop item in list %>
    ...
<% end %>

<% match expr %>
    ...
<% end %>
```

### end 闭合规则

默认使用 `<% end %>` 栈匹配（LIFO，最近优先），必要时使用显式 `<% end if %>` 消除歧义：

| 语法                | 行为                        |
| :---------------- | :------------------------ |
| `<% end %>`       | 闭合最近的未闭合模板化语句             |
| `<% end if %>`    | 显式闭合 if（验证栈顶是否为 if）       |
| `<% end loop %>`  | 显式闭合 loop（验证栈顶是否为 loop）   |
| `<% end match %>` | 显式闭合 match（验证栈顶是否为 match） |

**无歧义时用** **`<% end %>`**：

```
<% if condition %>
    ...
<% end %>              ← 闭合 if，无歧义
```

**嵌套歧义时用显式** **`<% end if %>`**：

```
<% loop item in list %>
    <% if condition %>
        ...
    <% else %>          ← if 的 else
        ...
    <% end if %>         ← 显式闭合 if，避免与 loop 混淆
<% end %>               ← 闭合 loop
```

**显式闭合类型不匹配时报错**：

```
<% if condition %>
    ...
<% end loop %>          ← 错误：栈顶是 if，不是 loop
```



## 语言变体

| 变体       | 分隔符         | 文件扩展名   |
| :------- | :---------- | :------ |
| **Dora** | `<% ... %>` | `.dora` |
| **Doki** | `{% ... %}` | `.doki` |

## 衍生项目：DejavuEngine

[DejavuEngine](../../DejavuEngine) 项目复用 Valkyrie 的 `<% ... %>` 元代码块语法，并在此基础上扩展了模板引擎功能：

| 来源              | 语法                        | 说明                      |
| :-------------- | :------------------------ | :---------------------- |
| Valkyrie 模板化    | `<% if condition %>`      | Valkyrie `if` 语句模板化     |
| Valkyrie 模板化    | `<% loop item in list %>` | Valkyrie `loop` 语句模板化   |
| Valkyrie 模板化    | `<% match expr %>`        | Valkyrie `match` 语句模板化  |
| DejavuEngine 扩展 | `<% block name %>`        | 块定义（DejavuEngine 自行扩展）  |
| DejavuEngine 扩展 | `<% end block %>`         | 块结束（DejavuEngine 自行扩展）  |
| DejavuEngine 扩展 | `<% extends 'path' %>`    | 模板继承（DejavuEngine 自行扩展） |
| DejavuEngine 扩展 | `<% include 'path' %>`    | 模板包含（DejavuEngine 自行扩展） |

**依赖关系**：DejavuEngine ← Valkyrie `<% ... %>` 语法 ← Oak.DejaVu 渲染引擎

### DejavuEngine 框架

| 框架            | 说明      |
| :------------ | :------ |
| Dejavu.Site   | 静态站点生成器 |
| Dejavu.Book   | 书籍生成器   |
| Dejavu.Cms    | 内容管理系统  |
| Dejavu.Config | 配置渲染器   |
| Dejavu.Mailer | 邮件模板引擎  |
| Dejavu.Report | 报告生成器   |

## 与 Oak.DejaVu 的关系

Valkyrie 的元代码块语法由 Oak.DejaVu 提供词法分析和解析支持：

```
Oak.DejaVu
    │
    ├── ValkyrieLexer：元代码块 Token（MetaBlockStart / MetaExpression）
    │
    └── DejavuEngine：DejaVuParser 完整模板渲染引擎
```

## 与 Gnosis 的映射

| Valkyrie Template | Gnosis                | 说明    |
| :---------------- | :-------------------- | :---- |
| `<% ... %>` 元代码块  | Gnosis.Toolchain 代码生成 | 编译期生成 |
| 元表达式              | Gnosis.Asset 资源模板     | 资源生成  |

## 开发状态

| 功能                | 状态               |
| :---------------- | :--------------- |
| `<% ... %>` 词法分析  | ✅ Oak.DejaVu 已实现 |
| DejaVuParser 模板解析 | ✅ Oak.DejaVu 已实现 |
| Valkyrie 编译期执行    | 📋 计划中           |

