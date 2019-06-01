# VOA 文档站配置 -- SSG 渲染, SEO 友好
export let projectConfig = {
    name: "voa-docs",
    version: "0.1.0",
    render: {
        defaultMode: "ssg",
        routes: [
            { path: "/", mode: "ssg" },
            { path: "/docs/:section", mode: "ssg" },
            { path: "/docs/:section/:page", mode: "ssg" },
            { path: "/components", mode: "ssg" },
            { path: "/components/:name", mode: "ssg" },
            { path: "/tutorial", mode: "ssg" },
            { path: "/tutorial/:step", mode: "ssg" },
            { path: "/api", mode: "ssg" },
            { path: "/api/:package", mode: "ssg" }
        ]
    },
    seo: {
        title: "VOA - Valkyrie Omni App Framework",
        description: "纯 GGScript + AWSL 的全栈框架，类似 Next.js 但不写一行 C#",
        og_image: "/images/og-cover.png"
    }
}
