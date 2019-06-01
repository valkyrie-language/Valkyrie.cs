# VOA 插件系统设计

> VOA 插件系统允许第三方扩展框架能力，对标 Next.js 插件生态。

## 插件类型

| 类型 | 说明 | 示例 |
|:---|:---|:---|
| **Compiler Plugin** | 编译管线钩子 | AWSL 预处理器、自定义优化 |
| **DevServer Plugin** | DevServer 中间件 | 代理、Mock、日志增强 |
| **Renderer Plugin** | 渲染扩展 | 自定义 `<Head>` 注入、Script 管理 |
| **Route Plugin** | 路由中间件 | 认证守卫、重定向、A/B 测试 |
| **Build Plugin** | 构建后处理 | 压缩、CDN 上传、SourceMap 处理 |

## 插件接口 (GGScript)

```ggscript
# 插件配置
struct VoaPlugin {
    name: utf8,
    version: utf8,
    hooks: [VoaPluginHook],
}

# 插件钩子
struct VoaPluginHook {
    event: utf8,        # before_compile / after_build / on_request
    handler: utf8,      # 函数名
    priority: i32,      # 执行优先级（越小越先）
}

# 插件注册
pub fn register_plugin(plugin: VoaPlugin) {
}
```

## 内置钩子列表

| 钩子名 | 触发时机 | 参数 |
|:---|:---|:---|
| `before_compile` | 编译开始前 | source_files, target |
| `after_compile` | 编译完成后 | build_result |
| `on_request` | HTTP 请求到达 | request, response |
| `before_render` | AWSL 渲染前 | component, props |
| `after_render` | AWSL 渲染后 | html |
| `on_hmr_update` | HMR 更新广播前 | changed_files |
| `before_build` | 生产构建前 | project_config |
| `after_build` | 生产构建后 | output_files |

## 配置文件定义 (voa.config.v)

```ggscript
# 插件声明
let plugins: [VoaPlugin] = [
    VoaPlugin {
        name: "voa-plugin-compress",
        version: "0.1.0",
        hooks: [
            VoaPluginHook { event: "after_build", handler: "compress_output", priority: 10 },
        ],
    },
    VoaPlugin {
        name: "voa-plugin-auth-guard",
        version: "0.1.0",
        hooks: [
            VoaPluginHook { event: "on_request", handler: "check_authentication", priority: 1 },
        ],
    },
]
```

## 插件发现机制

1. Legion 包管理系统扫描 `voa-plugin-*` 依赖
2. 读取插件的 `legion.von` 中的 `plugin` 段
3. 按 `priority` 排序执行钩子

## 安全模型

- 插件在沙箱瓦片内运行
- 文件系统访问通过 WASI 接口受限
- 网络请求通过 `[wasm_import]` 声明的白名单
