# VOA 1.0 发布清单

> 版本：1.0.0
> 发布日期：2027 年 5 月（提前于 2026-05-04 完成开发准备）

## 发布检查清单

### 功能完备性

- [x] **AWSL 编译器** — `AwslReactiveCompiler` 完整实现（Signal/Memo/Effect/Event/Island/Scoped CSS）
- [x] **SSR 渲染器** — `AwslSsrRenderer`（Head/Script/Suspense/Hydration）
- [x] **VOA 编译器** — `VoaCompiler`（AWSL + GGScript → WASM 全链路）
- [x] **WASM Target Builder** — `WasmTargetBuilder`（.wasm + Bridge JS + HTML）
- [x] **PWA 生成器** — `PwaGenerator`（SW + Manifest + 注册脚本 + 离线页）
- [x] **开发服务器** — `VoaDevServer`（HTTP + WebSocket HMR + FileWatcher + Bridge）
- [x] **Error Overlay** — `VoaErrorOverlay`（结构化错误 + 源码上下文 + 堆栈跟踪 + 修复建议）
- [x] **文件路由** — `VoaRouter`（文件路由 + 动态参数 + Layout）
- [x] **CLI 命令** — 16 条命令全闭环
- [x] **voa-core** — 7 个标准组件 + 11 个标准函数
- [x] **voa-effect** — EffectStore 完整生命周期

### 测试验证

- [x] 测试文件 9 个
- [x] 测试用例 108 个
- [x] AWSL 编译器测试 ≥ 30 ✅
- [x] 组件交互测试 ≥ 20 ✅
- [x] Effect 测试 ≥ 15 ✅
- [ ] SSR CSR 对比测试 ≥ 20（17/20）
- [ ] PWA 离线测试 ≥ 10（4/10）
- [ ] HMR 延迟实测 < 50ms（测量就绪，待实测）
- [ ] DevServer 24h 稳定性测试

### 文档

- [x] 工具链概览文档
- [x] AWSL 语言扩展文档
- [x] Getting Started 快速开始
- [x] API Reference（内嵌于 Getting Started）
- [x] RHI WASM 接口契约
- [x] 年度覆盖率报告
- [ ] API Routes 后端开发指南
- [ ] 贡献者指南

### NuGet 包发布

| 包 | 版本 | 状态 |
|:---|:---|:---:|
| `voa-core` | 1.0.0 | ✅ 项目就绪 |
| `voa-router` | 1.0.0 | ✅ 项目就绪 |
| `voa-effect` | 1.0.0 | ✅ 项目就绪 |
| `voa-cli` | 1.0.0 | ⚠️ 需打包配置 |

### 示例项目

- [x] examples/voa-core — 组件库使用示例
- [x] examples/voa-router — 路由使用示例
- [x] examples/voa-effect — Effect 系统示例
- [x] examples/fullstack — 全栈前后端示例

## 发布阻塞项

| 阻塞项 | 影响 | 缓解措施 |
|:---|:---|:---|
| NuGet 包实际测试 | 中 | 本地 `dotnet pack` 验证可先行 |
| RHI 实际联调 | 高 | 已定义明确契约，等待 07-Gnosis Render 就绪 |
| HMR 延迟实测 | 低 | 测量基础设施已就绪，不阻塞发布 |
| SSR CSR 对比 3 个缺口 | 低 | 不阻塞发布，M13 补充 |

## 版本声明

VOA 1.0.0 是 Valkyrie of Asgard 全栈开发框架的首个稳定版本，标志着纯 GGScript/AWSL 全栈开发方案的正式可用。框架提供对标 Next.js 的全栈体验，完全移除 JavaScript 生态依赖。

### 技术栈

- **语言**：GGScript + AWSL（100% Valkyrie 生态）
- **编译目标**：WASM + WASI
- **运行时**：ValkyrieRuntime（GGScript） + WebDialect（AWSL → JS 桥接）
- **渲染**：Islands 架构 + SSR/SSG + Hydration
- **工具链**：voa CLI（16 条命令）
- **基础设施**：Oak（文本编解码） + Acorn（二进制编解码） + Nyar（分析优化）
