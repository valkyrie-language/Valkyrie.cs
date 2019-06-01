# AWSL → GGScript 中间表示设计

> 版本：1.0-draft
> 日期：2026-05-04
> 目标：消除 JavaScript 依赖，实现 AWSL 编译到纯 GGScript + WASM

## 1. 动机

当前 `AwslReactiveCompiler` 输出 JavaScript，依赖 `voa-runtime.js`。这与 VOA "纯 GGScript" 的设计原则偏离。需要设计一个 GGScript 中间表示 (AwslIr)，使 AWSL 编译为 GGScript 抽象语法树，再由 ValkyrieRuntime + Nyar 优化为 WASM。

## 2. 核心映射

### 2.1 响应式原语

| AWSL 概念 | GGScript IR 表示 | 说明 |
|:---|:---|:---|
| `let x = 0` (mutable) | `Signal<i32>` | 可写响应式值 |
| `let x = 0` (immutable) | `let x: i32 = 0` | 不可变值 |
| `{expr}` 插值 | `Signal.bind(expr)` | 响应式绑定 |
| `memo(fn)` | `Computed<T>` | 派生状态 |
| `effect(fn)` | `Effect<()>` | 副作用 |
| `on:click={fn}` | `EventListener<Click>` | 事件处理器 |

### 2.2 模板结构

| AWSL 节点 | GGScript IR 表示 |
|:---|:---|
| `<div class="c">` | `ElementNode { tag: "div", attrs: { "class": String("c") } }` |
| `{variable}` 插值 | `InterpolationNode { expr: ExprId(variable) }` |
| `<if condition={expr}>` | `ConditionalNode { cond: expr, then: nodes[], else: nodes[] }` |
| `<loop item in {list}>` | `ForNode { item: Sym, iterable: expr, body: nodes[] }` |
| `<Client>` / `<Server>` | `IslandNode { kind: Client | Server, body: nodes[] }` |
| `<Head>` / `<Script>` | `MetaNode { kind: Head | Script, content: nodes[] }` |
| `<Suspense>` | `SuspenseNode { fallback: nodes[], content: nodes[] }` |

### 2.3 生命周期

| AWSL | GGScript IR |
|:---|:---|
| `onMount(fn)` | `on_mount(self, fn)` |
| `onDestroy(fn)` | `on_destroy(self, fn)` |
| `beforeUpdate(fn)` | `before_update(self, fn)` |
| `afterUpdate(fn)` | `after_update(self, fn)` |

## 3. IR 树结构

```
CompilationUnit
├── ComponentDecl(name, props, irNodes, css, islands)
│   ├── PropsDecl { fields: [{name, type, default}] }
│   ├── IRNode[]
│   │   ├── ElementNode(tag, attrs, children, id)
│   │   │   ├── AttrNode[]
│   │   │   │   ├── StaticAttr(name, value)
│   │   │   │   ├── DynamicAttr(name, exprId)
│   │   │   │   └── EventAttr(event, handlerId)
│   │   │   └── IRNode[]
│   │   ├── TextNode(text)
│   │   ├── InterpolationNode(exprId)
│   │   ├── ConditionalNode(condExprId, thenNodes, elseNodes)
│   │   ├── ForNode(varName, iterableExprId, bodyNodes, keyExprId?)
│   │   ├── IslandNode(kind, componentRef, props)
│   │   ├── MetaNode(kind, contentNodes)
│   │   └── SuspenseNode(fallbackNodes, contentNodes)
│   ├── StyledCss[] { scope, css }
│   └── SignalDecl[] { name, type, initialValue }
│
├── ConfigDecl { pwa?, hmr?, ssr? }
└── RouteManifest { entries: [{path, component}] }
```

## 4. 转换管道

```
  AWSL Source (.awsl)
        │
        ▼
  Oak.WidgetParser  ──── 文本解码
        │
        ▼
  WidgetParseResult (AWSL AST)
        │
        ▼
  AwslIrBuilder      ──── AST → AwslIr 转换  [NEW]
        │
        ▼
  AwslIr / IKunTree (GGScript IR)
        │
        ▼
  Dialect.Web        ──── 方言降级  [Nyar.Optimizer]
        │
        ▼
  WasmBackend        ──── 代码生成
        │
        ▼
  Acorn.Wasm.Encode  ──── 二进制编码
        │
        ▼
  .wasm
```

## 5. WebDialect 桥接设计

不再生成 JS 的 `addEventListener` 等调用，改为通过 GGScript 的 `[wasm_import]` 声明桥接到浏览器 API：

```v
# WebDialect — GGScript 到浏览器 API 的桥接层

[wasm_import(module = "voa_web")]
extern micro voa_document_query(selector: string): i32

[wasm_import(module = "voa_web")]
extern micro voa_element_set_text(handle: i32, text: string): void

[wasm_import(module = "voa_web")]
extern micro voa_element_add_event_listener(handle: i32, event_type: string, callback: fn): void

[wasm_import(module = "voa_web")]
extern micro voa_dom_create_element(tag: string): i32

[wasm_import(module = "voa_web")]
extern micro voa_dom_append_child(parent: i32, child: i32): void

[wasm_import(module = "voa_web")]
extern micro voa_dom_set_attribute(element: i32, name: string, value: string): void

[wasm_import(module = "voa_web")]
extern micro voa_dom_set_class_list(element: i32, class_name: string): void

[wasm_import(module = "voa_web")]
extern micro voa_dom_remove_child(parent: i32, child: i32): void
```

## 6. 编译用例示例

### 输入 (AWSL)

```awsl
<widget name="Counter">
    <div class="counter">
        <button on:click={() => count = count + 1}>
            +1
        </button>
        <span>Count: {count}</span>
    </div>
</widget>

<script>
    let count: i32 = 0
</script>
```

### 输出 (GGScript IR → 目标 GGScript)

```v
component Counter {
    signal count: i32 = 0

    fn handle_increment() {
        count = count + 1
    }

    fn render(self: Counter): ElementNode {
        return ElementNode("div", { class: "counter" }, [
            ElementNode("button", { click: self.handle_increment }, [
                TextNode("+1")
            ]),
            ElementNode("span", {}, [
                TextNode("Count: "),
                InterpolationNode(bind(count))
            ])
        ])
    }
}
```

## 7. Islands 架构映射

```
<Client>  →  ClientIsland(componentRef, hydrationStrategy)
<Server>  →  ServerOnly(componentRef)

Hydration 策略:
- "eager"   → 立即 hydrate
- "lazy"    → IntersectionObserver 进入视口时 hydrate
- "idle"    → requestIdleCallback 时 hydrate
- "none"    → 纯 SSR，不 hydrate
```

## 8. SSR 集成

GGScript 版本 SSR 通过 `ValkyrieRuntime` 的 `evaluate` 能力：

```v
micro render_page_ssr(component: Component, props: map): string {
    let runtime = ValkyrieRuntime.create()
    let instance = runtime.instantiate(component, props)
    return runtime.render_to_string(instance)
}
```

## 9. 迁移计划

| 阶段 | 内容 | 预计时间 |
|:---|:---|:---|
| Phase 1 | AwslIr 数据结构定义 | M14 |
| Phase 2 | AwslIrBuilder 实现（WidgetParseResult → AwslIr） | M15 |
| Phase 3 | AwslIr → GGScript AST 降级器 | M15 |
| Phase 4 | WebDialect wasm_import 声明 | M16 |
| Phase 5 | voa-runtime.js → voa-runtime.v 重写 | M16 |
| Phase 6 | 集成测试 + 旧 JS 路径废弃 | M17 |
