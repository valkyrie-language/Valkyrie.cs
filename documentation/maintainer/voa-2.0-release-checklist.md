# VOA 2.0 发布清单

> 目标版本：VOA 2.0
> 发布日期：提前就绪（2026-05-05）
> 对标：纯 GGScript 全栈框架，5 目标编译平台

## 一、功能完备性 ✅

- [x] **AWSL → GGScript IR 编译** — AwslIr 22 类型 + AwslIrBuilder 完整转换器
- [x] **SSR/SSG** — 20 个 SSR 对比测试通过（含 Head/Script/Suspense 注入）
- [x] **HMR 热重载** — < 50ms 目标，10 个基准测试 + HmrTiming 测量链
- [x] **文件路由** — VoaRouter 引擎（动态路由 / 嵌套 Layout / `[param]` 语法）
- [x] **API Routes** — voa-api 包（10 响应类型 + 中间件链）
- [x] **PWA** — 离线可用（SW / Manifest / 安装提示 / 10 离线场景测试）
- [x] **WASI 集成** — 25 函数 bridge + 5 模块（filesystem/sockets/cli/runtime）
- [x] **多目标编译** — 5 平台（wasm/wasi/clr/jvm/native）
- [x] **组件库** — 17 个组件（含 DataTable/Form/Dialog/Drawer 等）
- [x] **生态包** — 4 个（auth/i18n/analytics/seo）

## 二、测试验证 ✅

- [x] **单元测试** — 179 个测试用例（13 文件）
- [x] **集成测试** — WASM 编译链 + WASI 集成 + 多目标
- [x] **性能测试** — DevServer 基准（HMR / 内存 / 响应分位数）
- [x] **稳定性测试** — 模拟 1h 运行（无泄漏，增长 < 10MB）
- [x] **并发测试** — 1000 并发连接 + WebSocket 广播
- [x] **边界测试** — 空输入 / null 路径 / 不支持的平台

## 三、NuGet 包准备 ✅

| 包名 | 项目路径 | 产出 |
|:---|:---|:---|
| voa-core | projects/VoaCore/ | .nupkg |
| voa-router | projects/VoaRouter/ | .nupkg |
| voa-effect | projects/VoaEffect/ | .nupkg |
| voa-api | projects/VoaApi/ | .nupkg |
| voa-auth | projects/VoaAuth/ | .nupkg |
| voa-i18n | projects/VoaI18n/ | .nupkg |
| voa-analytics | projects/VoaAnalytics/ | .nupkg |
| voa-seo | projects/VoaSeo/ | .nupkg |

## 四、文档完备性 ✅

- [x] Getting Started（快速开始 / CLI 参考）
- [x] 贡献者指南（环境 / 规范 / 流程）
- [x] 插件系统设计（5 类 / 8 钩子）
- [x] 5 个官方教程（Blog / E-commerce / Dashboard / Portfolio / Chat）
- [x] 覆盖率报告（179 测试 / 8 包 / 17 组件）

## 五、示例项目 ✅

- [x] voa-core 示例
- [x] voa-router 示例
- [x] voa-effect 示例
- [x] fullstack 示例（前端 + 后端）

## 六、发布步骤

### 1. 版本号更新
```
voa-core: 0.2.0
其他包: 0.1.0 → 2.0.0 (首个正式版)
```

### 2. NuGet 发布
```bash
dotnet pack projects/VoaCore/ -c Release -o ./nupkgs/
dotnet nuget push ./nupkgs/*.nupkg -s https://api.nuget.org/v3/index.json -k $NUGET_KEY
```

### 3. annoucement 模板
```
🚀 VOA 2.0 正式发布 — 纯 GGScript 全栈框架

✨ 亮点：
- AWSL → GGScript IR 编译（22 类型完整 IR）
- 5 目标编译平台（WASM / WASI / CLR / JVM / Native）
- WASI 深度集成（25+ 系统调用桥接）
- 17 个生产就绪组件
- 8 个 NuGet 生态包
- 179 个测试覆盖（13 文件）

🛠️ 快速开始：
dotnet new --install voa-templates
voa new my-app
voa dev

📖 文档：https://docs.voa.dev
```

## 七、验收标准 ✅

| 条件 | 状态 |
|:---|:---:|
| 所有功能模块就绪 | ✅ |
| 179 测试全部通过 | ✅ |
| 8 个 NuGet 包可打包 | ✅ |
| 文档树完整（7 份） | ✅ |
| 多目标平台定义（5 个） | ✅ |
| AWSL → GGScript 编译器就绪 | ✅ |
| WASI 深度集成完成 | ✅ |
| DevServer 生产加固 | ✅ |

**VOA 2.0 发布就绪 🚀**
