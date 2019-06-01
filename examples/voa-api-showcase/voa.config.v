# VOA 项目配置 -- API Routes 展示
export let projectConfig = {
    name: "voa-api-showcase",
    version: "0.1.0",
    render: {
        defaultMode: "csr"
    },
    api: {
        directory: "source/api",
        cors: {
            origin: "*",
            methods: "GET,POST,PUT,PATCH,DELETE"
        },
        middleware: {
            logger: true,
            security_headers: true,
            timing: true
        }
    }
}
