# VOA — Valkyrie of Asgard 全栈开发框架

VOA 是 Valkyrie 生态的全栈开发框架，对标 Next.js 的全栈体验但拿掉一切 JavaScript。

## 核心特性

| 特性 | 说明 |
|:---|:---|
| **Islands 架构** | 前端使用 AWSL 模板语言，编译到细粒度响应式 JS |
| **SSR / SSG** | 服务端渲染引擎 + 编译时静态生成 |
| **文件路由** | `pages/` 目录自动路由 + 动态参数 + 嵌套 Layout |
| **WASM + WASI** | 全栈编译到 WebAssembly，不依赖浏览器 API |
| **PWA** | Service Worker + Manifest + 离线回退 |
| **DevServer** | 开发服务器 + WebSocket HMR + Error Overlay |
| **CLI** | 16 条 `voa` 命令全闭环 |
| **纯 Valkyrie 生态** | 100% GGScript + AWSL，无 JavaScript 依赖 |

---

## 快速开始

### 安装

```bash
# 通过 Legion 安装 VOA CLI
legion install voa-cli
```

### 创建项目

```bash
# 使用模板创建新项目
voa new my-app --template webapp

# 可选模板: webapp, api, dashboard, fullstack
```

### 项目结构

```
my-app/
├── voa.config.v          # 项目配置
├── source/
│   ├── main.v             # 入口 GGScript 代码
│   └── Hello.awsl          # AWSL 模板组件
├── pages/                 # 文件路由目录
│   ├── index.awsl          # 首页 → /
│   ├── about.awsl          # 关于页 → /about
│   └── user/
│       └── [id].awsl       # 动态路由 → /user/:id
├── components/            # 共享组件
├── styles/                # 全局样式
└── public/                # 静态资源
```

### 核心配置

```v
# voa.config.v

struct: {
    project: "my-app"
    version: "0.1.0"
    type: "frontend"
    target: "wasm"

    build: {
        output: "dist"
        compress: true
        source_map: false
    }

    server: {
        port: 8080
        open: true
    }

    pwa: {
        enabled: false
        cache_strategy: "network-first"
    }
}
```

---

## CLI 命令参考

| 命令 | 说明 | 示例 |
|:---|:---|:---|
| `voa new` | 创建新项目 | `voa new my-app` |
| `voa init` | 初始化项目 | `voa init --type frontend` |
| `voa dev` | 启动开发服务器 | `voa dev --open` |
| `voa build` | 生产构建 | `voa build --ssr --pwa` |
| `voa start` | 启动生产服务器 | `voa start --port 3000` |
| `voa run` | 运行项目 | `voa run` |
| `voa test` | 运行测试 | `voa test --filter MyTests` |
| `voa check` | 代码检查 | `voa check` |
| `voa fmt` | 代码格式化 | `voa fmt --check` |
| `voa clean` | 清理构建产物 | `voa clean` |
| `voa publish` | 发布包 | `voa publish` |
| `voa add` | 添加依赖 | `voa add voa-core` |
| `voa remove` | 移除依赖 | `voa remove voa-core` |
| `voa install` | 安装依赖 | `voa install` |
| `voa benchmark` | 运行基准测试 | `voa benchmark` |
| `voa coverage` | 代码覆盖率 | `voa coverage` |

---

## AWSL 组件开发

### 基本语法

```awsl
<widget name="MyComponent">
    <div class="container">
        <h1>{title}</h1>
        <p>{message}</p>
        <if condition={loading}>
            <span>Loading...</span>
        <else/>
            <span>{data}</span>
        </if>
    </div>
</widget>

<script>
    let title: string = "Hello VOA"
    let message: string = "Welcome to AWSL"
    let loading: bool = false
    let data: string = ""
</script>

<style>
    .container {
        padding: 16px;
        max-width: 800px;
        margin: 0 auto;
    }
</style>
```

### 响应式状态

```awsl
<script>
    let count: i32 = 0
</script>

<button on:click={() => count = count + 1}>
    Clicked {count} times
</button>
```

编译时自动转换为 `createSignal` 响应式状态。

### 列表渲染

```awsl
<script>
    let users: list = ["Alice", "Bob", "Charlie"]
</script>

<loop user in {users}>
    <div class="user-card">{user}</div>
</loop>
```

### 动态路由

```
pages/
├── index.awsl          → /
├── about.awsl          → /about
├── blog/
│   └── [slug].awsl     → /blog/:slug
└── user/
    └── [id]/
        └── index.awsl  → /user/:id
```

---

## voa-core 标准函数

| 模块 | 可用函数 |
|:---|:---|
| `console` | `log` / `warn` / `error` / `debug` / `info` |
| `fetch` | `fetch(url)` / `fetch_with_options(url, options)` |
| `json` | `parse_json(raw)` / `stringify_json(value)` |
| `storage` | `get_local(key)` / `set_local(key, value)` / `remove_local(key)` / `clear_local()` |
| `url` | `parse_url(url)` / `encode_uri_component(value)` |
| `dom` | `query_selector(selector)` / `set_element_text(id, text)` / `add_class(id, class)` / `remove_class(id, class)` |
| `math` | `abs` / `ceil` / `floor` / `round` / `sqrt` / `random` |
| `timer` | `set_timeout(delay_ms)` / `set_interval(interval_ms)` / `clear_timer(id)` / `now_ms()` |
| `crypto` | `uuid()` / `sha256(data)` / `encode_base64(data)` / `decode_base64(encoded)` |
| `performance` | `mark(name)` / `measure(name, start)` / `now_perf()` |
| `types` | `type_of(value)` / `is_string` / `is_number` / `is_bool` / `is_array` / `is_map` / `parse_int` / `parse_float` / `to_string` |

---

## SSR 渲染

```v
import voa-core.ssr

micro main() {
    let ssrResult = render_page_ssr(MyComponent, {
        title: "Welcome",
        data: loadDataFromApi()
    })

    return HttpResult {
        status: 200
        headers: { "Content-Type": "text/html; charset=utf-8" }
        body: ssrResult.html
    }
}
```

SSR 渲染器支持：
- `<Head>` 标签注入 → 自动提取到 HTML `<head>`
- `<Script>` 标签注入 → 自动注入到 `</body>` 前
- `<Suspense>` → 异步组件加载 + Fallback 显示 + MutationObserver 客户端挂起解决方案

---

## Effect 副作用系统

```v
import voa-effect

micro loadUserData(userId: string) {
    let result = effect("fetchUser", [userId])

    if is_pending(result) {
        return "loading"
    }

    if is_resolved(result) {
        return result.data
    }

    return "error"
}
```

---

## 部署

```bash
# 构建生产产物
voa build --ssr --pwa

# 产物输出到 dist/ 目录，包含:
# dist/
# ├── index.html          # 入口 HTML
# ├── main.wasm            # WASM 二进制
# ├── main.js              # JS 桥接
# ├── voa-runtime.js       # VOA 运行时
# ├── sw.js                # Service Worker (--pwa)
# ├── manifest.json        # Web App Manifest (--pwa)
# └── offline.html         # 离线回退页 (--pwa)

# 启动生产服务器
voa start --port 3000
```

---

## 版本

- VOA 当前版本：0.1.0
- 首版稳定发布目标：1.0.0（2027 年 5 月）
- AWSL 语言版本：1.0
- RHI 接口契约版本：1.0
