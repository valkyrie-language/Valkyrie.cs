// VOA 博客项目配置
export let projectConfig: {
    name: "voa-blog",
    version: "0.1.0",
    render: {
        defaultMode: "ssg",
        routes: [
            { path: "/", mode: "ssg" },
            { path: "/posts", mode: "ssg" },
            { path: "/posts/:slug", mode: "ssg" },
            { path: "/about", mode: "ssg" },
            { path: "/api/comments", mode: "ssr" }
        ]
    },
    ssg: {
        generate_paths: ["getStaticPaths"]
    }
}
