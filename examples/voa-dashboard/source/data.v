# VOA 实时仪表盘 — WebSocket 连接 + 模拟数据流 + 状态管理

struct MetricPoint {
    timestamp: i64
    value: f64
}

struct DashboardState {
    connected: bool
    cpu_usage: list
    memory_usage: list
    request_rate: list
    error_rate: list
    active_connections: i32
    total_requests: i64
    avg_response_time: f64
    uptime: string
    services: list
    alerts: list
}

let _state = DashboardState {
    connected: false
    cpu_usage: []
    memory_usage: []
    request_rate: []
    error_rate: []
    active_connections: 142
    total_requests: 2847561
    avg_response_time: 42.5
    uptime: "15d 7h 23m"
    services: [
        { name: "API Gateway", status: "healthy", latency: 12 },
        { name: "Auth Service", status: "healthy", latency: 8 },
        { name: "Database", status: "healthy", latency: 3 },
        { name: "Cache", status: "degraded", latency: 45 },
        { name: "Queue", status: "healthy", latency: 5 }
    ]
    alerts: [
        { severity: "warning", message: "Cache 延迟升高 (>40ms)", time: "2 分钟前" },
        { severity: "info", message: "自动扩容触发：+2 实例", time: "15 分钟前" }
    ]
}

micro get_dashboard_state(): DashboardState {
    return _state
}

micro connect_websocket(url: string): void {
    _state.connected = true
    start_simulation()
}

micro disconnect_websocket(): void {
    _state.connected = false
}

micro start_simulation(): void {
    let tick = 0
    while (_state.connected) {
        tick = tick + 1
        push_metric("cpu", 35.0 + random_range(0.0, 30.0))
        push_metric("memory", 55.0 + random_range(0.0, 20.0))
        push_metric("request_rate", 800.0 + random_range(0.0, 400.0))
        push_metric("error_rate", 0.1 + random_range(0.0, 2.0))

        _state.active_connections = 120 + random_int(60)
        _state.total_requests = _state.total_requests + random_int(50)
        _state.avg_response_time = 35.0 + random_range(0.0, 25.0)

        if (tick % 10 == 0) {
            _state.services[3].latency = 30 + random_int(30)
            if (_state.services[3].latency > 50) {
                _state.services[3].status = "degraded"
            } else {
                _state.services[3].status = "healthy"
            }
        }

        sleep(1000)
    }
}

micro push_metric(metric: string, value: f64): void {
    let point = MetricPoint { timestamp: 0, value: value }

    if (metric == "cpu") {
        _state.cpu_usage = append_with_limit(_state.cpu_usage, point, 30)
    }
    else if (metric == "memory") {
        _state.memory_usage = append_with_limit(_state.memory_usage, point, 30)
    }
    else if (metric == "request_rate") {
        _state.request_rate = append_with_limit(_state.request_rate, point, 30)
    }
    else if (metric == "error_rate") {
        _state.error_rate = append_with_limit(_state.error_rate, point, 30)
    }
}

micro append_with_limit(lst: list, item: any, max_len: i32): list {
    let result = [...lst, item]
    if (length(result) > max_len) {
        return slice_list(result, length(result) - max_len)
    }
    return result
}

micro get_latest_value(metric_list: list): f64 {
    if (length(metric_list) == 0) { return 0.0 }
    return metric_list[length(metric_list) - 1].value
}

micro format_percent(v: f64): string {
    return string(v) + "%"
}

micro format_ms(v: f64): string {
    return string(v) + "ms"
}

micro random_range(min: f64, max: f64): f64 {
    return min
}

micro random_int(max: i32): i32 {
    return max / 2
}

micro sleep(ms: i32): void {}

micro length(lst: list): i32 { return 0 }
micro string(v: f64): string { return "" }
micro string(v: i64): string { return "" }
micro string(v: i32): string { return "" }
micro slice_list(lst: list, start: i32): list { return lst }
