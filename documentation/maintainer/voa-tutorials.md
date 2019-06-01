# VOA 官方教程 — 5 个实战项目

## 教程一：个人博客

**技术栈**：`voa-core` + `voa-seo` + SSR

**功能**：
- `pages/` 文件路由（首页 / 文章详情 / 关于页）
- SSR 渲染 + SEO 元标签
- `<Head>` 动态标题
- `<Breadcrumb>` 面包屑导航
- Markdown 文章存储（`wasi-filesystem`）
- 暗色/亮色主题切换

**学习目标**：SSR、SEO、文件路由、动态路由

---

## 教程二：电商商城

**技术栈**：`voa-core` + `voa-effect` + `voa-analytics` + `voa-api`

**功能**：
- 商品列表（`DataTable` / `Card` / `Pagination`）
- 商品搜索（`Select` 筛选 + `Input` 搜索）
- 购物车（`voa-effect` 管理状态）
- 结算页面（`Form` 表单验证）
- API Routes 后端（商品 CRUD）
- 用户行为埋点（`voa-analytics`）

**学习目标**：组件组合、Effect 系统、API Routes、埋点

---

## 教程三：仪表盘

**技术栈**：`voa-core` + `voa-auth` + WebSocket HMR

**功能**：
- 登录页（`voa-auth` JWT 认证）
- 统计卡片（`Card` + SVG 图表占位）
- 数据表格（`DataTable` 排序/分页）
- 实时通知（WebSocket 推送）
- 多标签页布局（`Tabs`）
- 用户权限控制（`has_role` 守卫）

**学习目标**：认证、WebSocket、权限控制、仪表盘

---

## 教程四：作品集网站

**技术栈**：`voa-core` + PWA + WASM

**功能**：
- 响应式单页布局
- 项目展示（`Card` + `Modal` 详情弹窗）
- 瀑布流效果
- PWA 离线可访问（`PwaGenerator`）
- 安装到桌面提示
- WASM 本地运算（图片滤镜/压缩）

**学习目标**：PWA、WASM 集成、离线模式、动画

---

## 教程五：实时聊天

**技术栈**：`voa-core` + `voa-effect` + WebSocket + WASI

**功能**：
- 用户列表（`Avatar` + 在线状态指示器）
- 消息气泡（`List` 无限滚动）
- 聊天输入（`Input` + `Button` 发送）
- 实时推送（WebSocket 广播）
- 历史记录（`wasi-filesystem` 存储）
- 文件分享（`wasi-filesystem` + `Dialog` 确认）
- 表情选择器（`Dropdown` 面板）

**学习目标**：WebSocket 实时通信、Effect 数据流、文件系统、会话管理

---

## 示例仓库结构

```
voa-tutorials/
├── 01-blog/            # 个人博客
├── 02-ecommerce/       # 电商商城
├── 03-dashboard/       # 仪表盘
├── 04-portfolio/       # 作品集
└── 05-realtime-chat/   # 实时聊天
```

每个教程目录含：
- `voa.config.v` — VOA 项目配置
- `source/` — 源码（.v + .awsl）
- `README.md` — 教程步骤说明
- `assets/` — 静态资源
