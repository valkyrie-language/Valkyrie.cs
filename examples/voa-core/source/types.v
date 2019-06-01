# VOA 核心类型定义

struct VoaApp {
    name: string
    config: VoaConfig
    router: VoaRouter
    state: VoaAppState
    effects: list
    middlewares: list
}

struct VoaAppState {
    is_running: bool
    start_time: i64
    request_count: i64
    error_count: i64
}

struct VoaConfig {
    project_type: string
    target: string
    environment: string
    server: VoaServerConfig
    build: VoaBuildConfig
    hot_reload: VoaHotReloadConfig
}

struct VoaServerConfig {
    host: string
    port: int
    workers: int
    timeout: int
    cors: bool
    cors_origins: list
}

struct VoaBuildConfig {
    output: string
    minify: bool
    sourcemap: bool
    optimization: string
    ssr: bool
    pwa: bool
}

struct VoaHotReloadConfig {
    enabled: bool
    debounce_ms: int
    watch_extensions: list
}

struct VoaRequest {
    method: string
    path: string
    headers: map
    query: map
    body: string
    params: map
}

struct VoaResponse {
    status: int
    headers: map
    body: string
    content_type: string
}

struct VoaMiddleware {
    name: string
    priority: int
    handler: string
}

struct VoaPlugin {
    name: string
    version: string
    init_fn: string
    destroy_fn: string
}

enum VoaEnvironment {
    Development
    Staging
    Production
    Test
}

enum VoaProjectType {
    Frontend
    Backend
    Fullstack
    Library
}
