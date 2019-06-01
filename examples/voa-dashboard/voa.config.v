// VOA 实时仪表盘配置
export let projectConfig: {
    name: "voa-dashboard",
    version: "0.1.0",
    render: {
        defaultMode: "csr"
    },
    websocket: {
        url: "ws://localhost:3000/__voa_ws__"
        reconnect: true
        max_reconnect_attempts: 10
    }
}
