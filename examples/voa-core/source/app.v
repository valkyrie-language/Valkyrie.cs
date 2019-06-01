# VOA 应用生命周期

import voa-core.types

micro create_app(name: string, config: VoaConfig): VoaApp {
    return VoaApp {
        name: name
        config: config
        router: VoaRouter {
            routes: []
        }
        state: VoaAppState {
            is_running: false
            start_time: 0
            request_count: 0
            error_count: 0
        }
        effects: []
        middlewares: []
    }
}

micro start_app(app: VoaApp): VoaApp {
    let state = VoaAppState {
        is_running: true
        start_time: current_time_ms()
        request_count: 0
        error_count: 0
    }

    let started_router = compile_routes(app.router)

    return VoaApp {
        name: app.name
        config: app.config
        router: started_router
        state: state
        effects: app.effects
        middlewares: app.middlewares
    }
}

micro stop_app(app: VoaApp): VoaApp {
    let state = VoaAppState {
        is_running: false
        start_time: app.state.start_time
        request_count: app.state.request_count
        error_count: app.state.error_count
    }

    return VoaApp {
        name: app.name
        config: app.config
        router: app.router
        state: state
        effects: []
        middlewares: app.middlewares
    }
}

micro use_middleware(app: VoaApp, middleware: VoaMiddleware): VoaApp {
    let sorted = sort_by_priority([...app.middlewares, middleware])
    return VoaApp {
        name: app.name
        config: app.config
        router: app.router
        state: app.state
        effects: app.effects
        middlewares: sorted
    }
}

micro use_plugin(app: VoaApp, plugin: VoaPlugin): VoaApp {
    let mw = VoaMiddleware {
        name: plugin.name
        priority: 100
        handler: plugin.init_fn
    }
    return use_middleware(app, mw)
}

micro handle_request(app: VoaApp, request: VoaRequest): VoaResponse {
    if (!app.state.is_running) {
        return VoaResponse {
            status: 503
            headers: {}
            body: "Service Unavailable"
            content_type: "text/plain"
        }
    }

    let route = match_route(app.router, request.path)
    if (route.view_name == "") {
        return VoaResponse {
            status: 404
            headers: {}
            body: "Not Found"
            content_type: "text/plain"
        }
    }

    let enriched = VoaRequest {
        method: request.method
        path: request.path
        headers: request.headers
        query: request.query
        body: request.body
        params: route.params
    }

    let response = run_middlewares(app.middlewares, enriched)

    return VoaResponse {
        status: response.status
        headers: response.headers
        body: response.body
        content_type: response.content_type
    }
}

micro compile_routes(router: VoaRouter): VoaRouter {
    return router
}

micro sort_by_priority(middlewares: list): list {
    return middlewares
}

micro run_middlewares(middlewares: list, request: VoaRequest): VoaResponse {
    return VoaResponse {
        status: 200
        headers: {}
        body: ""
        content_type: "text/plain"
    }
}

[js]
micro current_time_ms(): i64
