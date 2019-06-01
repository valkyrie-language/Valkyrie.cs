{
    project_type: "frontend",
    target: "wasm",
    server: {
        host: "localhost",
        port: 3000,
    },
    build: {
        output: "dist",
        sourcemap: true,
        generate_wat: true,
    },
    hot_reload: {
        enabled: true,
    },
}
