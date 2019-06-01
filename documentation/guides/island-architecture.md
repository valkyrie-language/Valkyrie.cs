# Island 架构

VOA 支持 **Hybrid Island** 架构，灵感来自 Astro。页面由多种独立岛屿组成，每种岛屿可以使用不同的渲染引擎。

## 五种岛屿类型

| 类型 | 说明 | 适用场景 |
|:---|:---|:---|
| `static` | 纯静态 HTML，零 JS | 文档、静态内容 |
| `hydrated` | AWSL 组件，懒水化 | 交互 UI 组件 |
| `wasm` | WASM 重计算组件 | 复杂计算、游戏逻辑 |
| `vue` | Vue 3 生态组件 | 复用 Vue UI 库 |
| `react` | React 生态组件 | 复用 React UI 库 |

## 架构原理

```
┌─────────────────────────────────────────────┐
│                  index.html                  │
│                                              │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  │
│  │  static  │  │ hydrated │  │   vue    │  │
│  │ 岛屿 A   │  │ 岛屿 B   │  │ 岛屿 C   │  │
│  │ (SSR)   │  │ (AWSL)   │  │ (Vue 3)  │  │
│  └──────────┘  └──────────┘  └──────────┘  │
│                                              │
│  HydrationScheduler 统一调度水化时机         │
└─────────────────────────────────────────────┘
```

每个岛屿是独立的渲染单元。`static` 岛屿在服务端直接渲染 HTML，零 JS 体积。`hydrated` 岛屿输出 AWSL 组件并在客户端按需水化。`vue`/`react` 岛屿加载对应框架运行时，但与 VOA 响应式内核隔离。

## 水化策略

`HydrationScheduler` 按优先级调度岛屿水化：

1. **视口内岛屿** — 优先水化
2. **即将进入视口** — 预水化
3. **远离视口** — 延迟水化
4. **静态岛屿** — 永不水化

这种策略确保首屏加载最小化，用户只为自己看到的内容支付 JS 成本。

## 定义岛屿

AWSL 组件自动成为 `hydrated` 岛屿：

```awsl
<island:counter>
  <script>
    let count = 0
  </script>
  <button @click="count++">点击: {count}</button>
</island:counter>
```

Vue/React 岛屿通过在配置中声明：

```v
define_config(voa) {
    islands {
        vue_components = ["components/chart.vue"]
        react_components = ["components/map.jsx"]
    }
}
```
