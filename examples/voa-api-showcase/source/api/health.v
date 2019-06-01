# VOA API 示例 — 健康检查端点
# GET /api/health → { status: "ok", timestamp: ..., uptime: ... }

export micro GET(req: VoaApiRequest): VoaApiResponse {
    return json_response({
        status: "ok",
        service: "voa-api-showcase",
        version: "0.1.0",
        timestamp: "2026-11-01T00:00:00Z",
        uptime: "24h"
    })
}
