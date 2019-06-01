{
    project_type: "frontend",
    target: "wasm",
    server: {
        host: "localhost",
        port: 3001,
    },
    build: {
        output: "dist",
        sourcemap: true,
    },
    hot_reload: {
        enabled: true,
    },
}
