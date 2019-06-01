# VOA 项目配置 -- HMR + DevTools 展示
export let projectConfig = {
    name: "voa-hmr-showcase",
    version: "0.1.0",
    render: {
        defaultMode: "csr"
    },
    devtools: {
        enabled: true,
        panels: ["components", "routes", "cache", "performance"],
        keyboard_shortcuts: true,
        error_overlay: true,
        hmr_indicator: true
    }
}
