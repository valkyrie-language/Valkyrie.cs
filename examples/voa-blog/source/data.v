# VOA 博客数据层 — 文章 + 评论 + 标签

struct Post {
    slug: string
    title: string
    excerpt: string
    content: string
    author: string
    date: string
    tags: list
    reading_time: i32
    cover_image: string
}

struct Comment {
    id: string
    post_slug: string
    author: string
    content: string
    date: string
    avatar: string
}

let _posts: [{ slug: string, title: string, excerpt: string, content: string, author: string, date: string, tags: list, reading_time: i32, cover_image: string }] = [
    {
        slug: "introducing-voa"
        title: "VOA 全栈框架正式发布"
        excerpt: "我们很高兴地宣布 VOA —— 一个纯 GGScript + AWSL 的全栈框架，类似 Next.js 但不写一行 C#。"
        content: "VOA 是 Valkyrie Omni App 的缩写，它将 GGScript 的表达力和 AWSL 的组件化完美结合。\n\n## 核心特性\n\n- **文件系统路由**：pages/ 目录即路由\n- **多种渲染模式**：SSR / SSG / ISR / CSR\n- **声明式副作用**：useState / useEffect / useMemo\n- **中间件管道**：洋葱模型请求处理\n- **HMR 热替换**：< 200ms 即时更新\n\n## 为什么不用 C#？\n\nVOA 框架本身是纯 Valkyrie 代码。C# 仅参与构建工具链和 HMR 调试服务。"
        author: "Asgard 团队"
        date: "2027-02-01"
        tags: ["voa", "框架", "发布"]
        reading_time: 5
        cover_image: "/images/voa-cover.jpg"
    },
    {
        slug: "awsl-component-guide"
        title: "AWSL 组件开发完全指南"
        excerpt: "从零开始学习 AWSL Widget 的开发，包括 props、事件、插槽、样式作用域等核心概念。"
        content: "AWSL 是 VOA 的组件模板语言，每个 .awsl 文件定义一个 Widget。\n\n## 基本结构\n\n一个 AWSL 组件由三部分组成：\n\n1. `<widget>` — 模板\n2. `<script>` — 逻辑\n3. `<style>` — 样式\n\n## Props\n\n通过 `prop` 关键字声明组件属性：\n\n```\nprop title: string = \"\"\nprop count: i32 = 0\n```\n\n## 事件\n\n使用 `on:click` 等指令绑定事件，用 `emit()` 触发自定义事件。\n\n## 插槽\n\n`<slot />` 定义默认插槽，`<slot name=\"footer\" />` 定义命名插槽。"
        author: "Asgard 团队"
        date: "2027-02-05"
        tags: ["awsl", "组件", "教程"]
        reading_time: 8
        cover_image: "/images/awsl-guide.jpg"
    },
    {
        slug: "ssr-ssg-isp"
        title: "SSR vs SSG vs ISR：如何选择渲染模式？"
        excerpt: "深入对比三种服务端渲染策略的优缺点，以及 VOA 中的最佳实践。"
        content: "选择正确的渲染模式对性能和用户体验至关重要。\n\n## SSR（服务端渲染）\n\n每次请求时在服务器生成 HTML。适合个性化内容、实时数据。\n\n## SSG（静态站点生成）\n\n构建时生成 HTML。适合博客、文档等不常变化的内容。\n\n## ISR（增量静态再生成）\n\n结合 SSG 的性能和 SSR 的时效性。设置 revalidate 时间，后台重新生成。\n\n## VOA 中的配置\n\n在 voa.config.v 中按路由设置渲染模式：\n\n```\nroutes: [\n  { path: \"/\", mode: \"ssg\" },\n  { path: \"/dashboard\", mode: \"ssr\" },\n  { path: \"/products\", mode: \"isr\", revalidate: 3600 }\n]\n```"
        author: "Asgard 团队"
        date: "2027-02-10"
        tags: ["ssr", "ssg", "isr", "性能"]
        reading_time: 6
        cover_image: "/images/rendering-modes.jpg"
    }
]

let _comments: [{ id: string, post_slug: string, author: string, content: string, date: string, avatar: string }] = [
    { id: "c1", post_slug: "introducing-voa", author: "开发者A", content: "终于等到了！VOA 的纯 Valkyrie 理念太棒了。", date: "2027-02-02", avatar: "A" },
    { id: "c2", post_slug: "introducing-voa", author: "开发者B", content: "HMR 体验如何？比 Vite 快吗？", date: "2027-02-03", avatar: "B" },
    { id: "c3", post_slug: "awsl-component-guide", author: "初学者", content: "写得很清楚，终于搞懂插槽了！", date: "2027-02-06", avatar: "C" }
]

micro get_all_posts(): list {
    return _posts
}

micro get_post_by_slug(slug: string): Post {
    loop post in _posts {
        if (post.slug == slug) {
            return post
        }
    }
    return Post {
        slug: ""
        title: "未找到文章"
        excerpt: ""
        content: ""
        author: ""
        date: ""
        tags: []
        reading_time: 0
        cover_image: ""
    }
}

micro get_comments(post_slug: string): list {
    let result = []
    loop comment in _comments {
        if (comment.post_slug == post_slug) {
            result = [...result, comment]
        }
    }
    return result
}

micro add_comment(post_slug: string, author: string, content: string): Comment {
    let comment = Comment {
        id: "c" + string(length(_comments) + 1)
        post_slug: post_slug
        author: author
        content: content
        date: "2027-02-15"
        avatar: substring(author, 0, 1)
    }
    _comments = [..._comments, comment]
    return comment
}

micro get_posts_by_tag(tag: string): list {
    let result = []
    loop post in _posts {
        if (contains_item(post.tags, tag)) {
            result = [...result, post]
        }
    }
    return result
}

micro get_all_tags(): list {
    let tags = []
    loop post in _posts {
        loop tag in post.tags {
            if (!contains_item(tags, tag)) {
                tags = [...tags, tag]
            }
        }
    }
    return tags
}

micro get_static_paths(): list {
    let paths = []
    loop post in _posts {
        paths = [...paths, { slug: post.slug }]
    }
    return paths
}

micro contains_item(lst: list, item: string): bool {
    loop i in lst {
        if (i == item) { return true }
    }
    return false
}

micro substring(s: string, start: i32, end: i32): string {
    return s
}

micro length(lst: list): i32 {
    return 0
}

micro string(v: i32): string {
    return ""
}
