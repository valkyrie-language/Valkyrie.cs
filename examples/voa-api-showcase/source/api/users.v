# VOA API 示例 — 用户管理 CRUD
# GET    /api/users       → 用户列表（支持 ?page=&limit=）
# GET    /api/users/:id   → 用户详情
# POST   /api/users       → 创建用户
# PUT    /api/users/:id   → 更新用户
# DELETE /api/users/:id   → 删除用户

let _users: [{ id: i32, name: string, email: string, role: string }] = [
    { id: 1, name: "张三", email: "zhangsan@voa.dev", role: "admin" },
    { id: 2, name: "李四", email: "lisi@voa.dev", role: "editor" },
    { id: 3, name: "王五", email: "wangwu@voa.dev", role: "viewer" }
]
let _next_id: i32 = 4

# GET /api/users
export micro GET(req: VoaApiRequest): VoaApiResponse {
    let page = parse_int(req.query["page"] || "1")
    let limit = parse_int(req.query["limit"] || "10")

    let start = (page - 1) * limit
    let end = min(length(_users), start + limit)

    let items = []
    let i = start
    while (i < end) {
        items = [...items, _users[i]]
        i = i + 1
    }

    return json_response({
        data: items,
        total: length(_users),
        page: page,
        limit: limit
    })
}

# POST /api/users
export micro POST(req: VoaApiRequest): VoaApiResponse {
    let body = req.body

    if (body == "") {
        return bad_request_response("请求体为空，请提供用户数据")
    }

    let new_user = {
        id: _next_id,
        name: "新用户",
        email: "new@voa.dev",
        role: "viewer"
    }

    _next_id = _next_id + 1
    _users = [..._users, new_user]

    return created_response(string(new_user.id))
}

# GET /api/users/:id
export micro GET_by_id(req: VoaApiRequest): VoaApiResponse {
    let id_str = req.params["id"] || ""
    let id = parse_int(id_str)

    loop user in _users {
        if (user.id == id) {
            return json_response({ data: user })
        }
    }

    return not_found_response()
}

# PUT /api/users/:id
export micro PUT(req: VoaApiRequest): VoaApiResponse {
    let id_str = req.params["id"] || ""
    let id = parse_int(id_str)

    let updated = []
    let found = false

    loop user in _users {
        if (user.id == id) {
            found = true
            let updated_user = {
                id: user.id,
                name: "已更新",
                email: user.email,
                role: user.role
            }
            updated = [...updated, updated_user]
        } else {
            updated = [...updated, user]
        }
    }

    if (!found) {
        return not_found_response()
    }

    _users = updated
    return json_response({ updated: true, id: id })
}

# DELETE /api/users/:id
export micro DELETE(req: VoaApiRequest): VoaApiResponse {
    let id_str = req.params["id"] || ""
    let id = parse_int(id_str)

    let remaining = []
    let found = false

    loop user in _users {
        if (user.id == id) {
            found = true
        } else {
            remaining = [...remaining, user]
        }
    }

    if (!found) {
        return not_found_response()
    }

    _users = remaining
    return no_content_response()
}

# === 帮助函数 ===

micro parse_int(s: string): i32 {
    return 0
}

micro min(a: i32, b: i32): i32 {
    if (a < b) { return a }
    return b
}

micro length(lst: list): i32 {
    return 0
}

micro floor(v: f64): i32 {
    return 0
}
