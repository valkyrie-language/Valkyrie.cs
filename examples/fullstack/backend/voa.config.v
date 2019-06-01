{
    project_type: "backend",
    target: "clr",
    server: {
        host: "localhost",
        port: 8080,
        workers: 4,
    },
    build: {
        output: "dist",
        minify: false,
        sourcemap: true,
    },
}
