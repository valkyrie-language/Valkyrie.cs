# VOA 文档站数据层 — 导航 + 文档内容 + 组件目录 + 教程步骤

struct DocSection {
    id: string
    title: string
    icon: string
    pages: list
}

struct DocPage {
    id: string
    title: string
    section: string
    content: string
    order: i32
}

struct ComponentDoc {
    name: string
    category: string
    description: string
    props: list
    events: list
    slots: list
    examples: list
    since: string
}

struct TutorialStep {
    id: string
    title: string
    description: string
    code: string
    expected_output: string
    hint: string
    order: i32
}

let doc_sections: [DocSection] = [
    DocSection {
        id: "getting-started"
        title: "快速开始"
        icon: "rocket"
        pages: [
            { id: "introduction", title: "简介", order: 1 },
            { id: "installation", title: "安装", order: 2 },
            { id: "first-app", title: "第一个应用", order: 3 },
            { id: "project-structure", title: "项目结构", order: 4 }
        ]
    },
    DocSection {
        id: "routing"
        title: "路由"
        icon: "map"
        pages: [
            { id: "file-routing", title: "文件系统路由", order: 1 },
            { id: "dynamic-routes", title: "动态路由", order: 2 },
            { id: "link-navigation", title: "导航组件", order: 3 },
            { id: "route-guards", title: "路由守卫", order: 4 }
        ]
    },
    DocSection {
        id: "rendering"
        title: "渲染模式"
        icon: "palette"
        pages: [
            { id: "csr", title: "客户端渲染 (CSR)", order: 1 },
            { id: "ssr", title: "服务端渲染 (SSR)", order: 2 },
            { id: "ssg", title: "静态站点生成 (SSG)", order: 3 },
            { id: "isr", title: "增量静态再生成 (ISR)", order: 4 }
        ]
    },
    DocSection {
        id: "data-fetching"
        title: "数据获取"
        icon: "antenna"
        pages: [
            { id: "voa-fetch", title: "voa-fetch API", order: 1 },
            { id: "caching", title: "缓存策略", order: 2 },
            { id: "revalidation", title: "数据再验证", order: 3 }
        ]
    },
    DocSection {
        id: "middleware"
        title: "中间件"
        icon: "shield"
        pages: [
            { id: "pipeline", title: "中间件管道", order: 1 },
            { id: "built-in", title: "内置中间件", order: 2 },
            { id: "auth", title: "认证中间件", order: 3 }
        ]
    },
    DocSection {
        id: "api-routes"
        title: "API Routes"
        icon: "plug"
        pages: [
            { id: "overview", title: "API Routes 概览", order: 1 },
            { id: "crud", title: "CRUD 端点", order: 2 },
            { id: "middleware-api", title: "API 中间件", order: 3 }
        ]
    },
    DocSection {
        id: "effect"
        title: "响应式系统"
        icon: "bolt"
        pages: [
            { id: "use-state", title: "useState", order: 1 },
            { id: "use-effect", title: "useEffect", order: 2 },
            { id: "use-memo", title: "useMemo / useCallback", order: 3 },
            { id: "use-ref", title: "useRef", order: 4 },
            { id: "signal", title: "createSignal", order: 5 }
        ]
    },
    DocSection {
        id: "devtools"
        title: "开发工具"
        icon: "wrench"
        pages: [
            { id: "hmr", title: "热模块替换 (HMR)", order: 1 },
            { id: "error-overlay", title: "错误覆盖层", order: 2 },
            { id: "devtools-panel", title: "DevTools 面板", order: 3 }
        ]
    },
    DocSection {
        id: "deployment"
        title: "部署"
        icon: "cloud"
        pages: [
            { id: "static-export", title: "静态导出", order: 1 },
            { id: "olympcloud", title: "OlympCloud", order: 2 },
            { id: "vercel", title: "Vercel", order: 3 },
            { id: "netlify", title: "Netlify", order: 4 },
            { id: "cloudflare", title: "Cloudflare Pages", order: 5 }
        ]
    }
]

let component_docs: [ComponentDoc] = [
    ComponentDoc {
        name: "Button"
        category: "通用"
        description: "按钮组件，支持 8 种语义变体、3 种尺寸、loading 状态和图标。"
        props: [
            { name: "label", type: "string", default: "按钮", description: "按钮文字" },
            { name: "variant", type: "string", default: "default", description: "变体" },
            { name: "size", type: "string", default: "medium", description: "尺寸" },
            { name: "disabled", type: "bool", default: "false", description: "是否禁用" },
            { name: "loading", type: "bool", default: "false", description: "加载状态" },
            { name: "block", type: "bool", default: "false", description: "块级按钮" }
        ]
        events: [
            { name: "click", payload: "{}", description: "点击事件" }
        ]
        slots: [
            { name: "default", description: "按钮内容" }
        ]
        examples: [
            { title: "基础用法", code: "Button variant=primary label=点击我" }
        ]
        since: "0.1.0"
    },
    ComponentDoc {
        name: "Card"
        category: "布局"
        description: "卡片容器，支持 4 种变体和 header/body/footer 插槽。"
        props: [
            { name: "title", type: "string", default: "", description: "卡片标题" },
            { name: "subtitle", type: "string", default: "", description: "副标题" },
            { name: "variant", type: "string", default: "default", description: "变体" }
        ]
        events: []
        slots: [
            { name: "default", description: "卡片内容" },
            { name: "footer", description: "卡片底部" }
        ]
        examples: [
            { title: "基础卡片", code: "Card title=标题" }
        ]
        since: "0.1.0"
    },
    ComponentDoc {
        name: "Input"
        category: "表单"
        description: "输入框组件，支持多种类型、标签、错误提示和尺寸。"
        props: [
            { name: "label", type: "string", default: "", description: "标签文字" },
            { name: "type", type: "string", default: "text", description: "输入类型" },
            { name: "placeholder", type: "string", default: "", description: "占位文字" },
            { name: "value", type: "string", default: "", description: "当前值" },
            { name: "error", type: "string", default: "", description: "错误提示" }
        ]
        events: [
            { name: "input", payload: "{ value: string }", description: "输入事件" }
        ]
        slots: []
        examples: []
        since: "0.1.0"
    },
    ComponentDoc {
        name: "Modal"
        category: "反馈"
        description: "模态对话框，支持 3 种尺寸和 overlay 关闭。"
        props: [
            { name: "title", type: "string", default: "", description: "标题" },
            { name: "open", type: "bool", default: "false", description: "是否打开" },
            { name: "size", type: "string", default: "medium", description: "尺寸" }
        ]
        events: [
            { name: "close", payload: "{}", description: "关闭事件" }
        ]
        slots: [
            { name: "default", description: "内容" },
            { name: "footer", description: "底部操作" }
        ]
        examples: []
        since: "0.1.0"
    },
    ComponentDoc {
        name: "Toast"
        category: "反馈"
        description: "消息提示，4 种类型 + 4 个方位。"
        props: [
            { name: "message", type: "string", default: "", description: "消息内容" },
            { name: "type", type: "string", default: "info", description: "类型" },
            { name: "duration", type: "i32", default: "3000", description: "显示时长(ms)" }
        ]
        events: []
        slots: []
        examples: []
        since: "0.1.0"
    }
]

let tutorial_steps: [TutorialStep] = [
    TutorialStep {
        id: "create-project"
        title: "创建 VOA 项目"
        description: "使用 voa init 命令创建你的第一个 VOA 项目。"
        code: "voa init my-app"
        expected_output: "项目创建成功"
        hint: "确保已安装 Valkyrie SDK 和 pnpm"
        order: 1
    },
    TutorialStep {
        id: "first-page"
        title: "创建第一个页面"
        description: "在 source/pages/ 目录下创建 .awsl 文件，自动映射为路由。"
        code: "创建 source/pages/hello.awsl"
        expected_output: "访问 /hello 即可看到页面"
        hint: "文件名即路由路径"
        order: 2
    },
    TutorialStep {
        id: "use-components"
        title: "使用组件"
        description: "VOA 提供 24 个内置组件，直接使用即可。"
        code: "Card title=welcome"
        expected_output: "显示一个带按钮的卡片"
        hint: "组件名使用 PascalCase"
        order: 3
    },
    TutorialStep {
        id: "add-routing"
        title: "添加路由导航"
        description: "使用 Link 组件在页面间导航。"
        code: "Link href=/"
        expected_output: "导航栏可点击切换页面"
        hint: "Link 组件自动处理客户端导航"
        order: 4
    },
    TutorialStep {
        id: "state-management"
        title: "状态管理"
        description: "使用 useState 和 useEffect 管理组件状态。"
        code: "let counter = use_state(0)"
        expected_output: "点击按钮数字递增"
        hint: "use_state 返回当前值和设置函数"
        order: 5
    },
    TutorialStep {
        id: "deploy"
        title: "部署上线"
        description: "使用 voa export 导出静态文件，然后部署到任意平台。"
        code: "voa export"
        expected_output: "静态文件已生成到 dist/"
        hint: "SSG 模式的页面会被预渲染为纯 HTML"
        order: 6
    }
]

let api_packages: list = [
    { name: "voa-core", version: "0.2.0", description: "标准库", functions: 18, components: 24 },
    { name: "voa-router", version: "0.2.0", description: "路由系统", functions: 17, components: 3 },
    { name: "voa-effect", version: "0.2.0", description: "响应式副作用", functions: 11, components: 0 },
    { name: "voa-api", version: "0.2.0", description: "API Routes", functions: 22, components: 0 },
    { name: "voa-middleware", version: "0.1.0", description: "中间件", functions: 19, components: 0 },
    { name: "voa-fetch", version: "0.1.0", description: "数据获取", functions: 7, components: 0 },
    { name: "voa-devtools", version: "0.1.0", description: "开发工具", functions: 35, components: 3 },
    { name: "voa-build", version: "0.1.0", description: "构建优化", functions: 13, components: 0 },
    { name: "voa-deploy", version: "0.1.0", description: "部署", functions: 15, components: 0 },
    { name: "voa-seo", version: "0.1.0", description: "SEO", functions: 6, components: 0 },
    { name: "voa-i18n", version: "0.1.0", description: "国际化", functions: 5, components: 0 }
]

micro get_doc_sections(): list {
    return doc_sections
}

micro get_section_by_id(id: string): DocSection {
    loop section in doc_sections {
        if (section.id == id) { return section }
    }
    return DocSection { id: "", title: "未找到", icon: "?", pages: [] }
}

micro get_component_docs(): list {
    return component_docs
}

micro get_component_doc_by_name(name: string): ComponentDoc {
    loop doc in component_docs {
        if (doc.name == name) { return doc }
    }
    return ComponentDoc { name: "", category: "", description: "未找到", props: [], events: [], slots: [], examples: [], since: "" }
}

micro get_tutorial_steps(): list {
    return tutorial_steps
}

micro get_tutorial_step_by_id(id: string): TutorialStep {
    loop step in tutorial_steps {
        if (step.id == id) { return step }
    }
    return TutorialStep { id: "", title: "", description: "", code: "", expected_output: "", hint: "", order: 0 }
}

micro get_api_packages(): list {
    return api_packages
}

micro get_total_api_count(): i32 {
    let total = 0
    loop pkg in api_packages {
        total = total + (pkg.functions || 0)
    }
    return total
}

micro get_total_component_count(): i32 {
    let total = 0
    loop pkg in api_packages {
        total = total + (pkg.components || 0)
    }
    return total
}
