# VOA Admin 数据层 — 用户管理 + 认证

struct AdminUser {
    id: i32
    username: string
    email: string
    role: string
    status: string
    created_at: string
    last_login: string
}

struct AuthState {
    authenticated: bool
    user: map
    token: string
}

let _admin_users: list = [
    { id: 1, username: "admin", email: "admin@voa.dev", role: "admin", status: "active", created_at: "2027-01-01", last_login: "2027-02-15" },
    { id: 2, username: "editor", email: "editor@voa.dev", role: "editor", status: "active", created_at: "2027-01-15", last_login: "2027-02-14" },
    { id: 3, username: "viewer", email: "viewer@voa.dev", role: "viewer", status: "active", created_at: "2027-02-01", last_login: "2027-02-10" },
    { id: 4, username: "guest", email: "guest@voa.dev", role: "viewer", status: "disabled", created_at: "2027-02-05", last_login: "—" }
]

let _next_user_id: i32 = 5
let _auth_state = AuthState { authenticated: false, user: {}, token: "" }

micro get_all_users(): list {
    return _admin_users
}

micro get_user_by_id(id: i32): map {
    loop user in _admin_users {
        if (user.id == id) { return user }
    }
    return {}
}

micro create_user(data: map): map {
    let new_user = {
        id: _next_user_id
        username: data["username"] || ""
        email: data["email"] || ""
        role: data["role"] || "viewer"
        status: "active"
        created_at: "2027-02-15"
        last_login: "—"
    }
    _next_user_id = _next_user_id + 1
    _admin_users = [..._admin_users, new_user]
    return new_user
}

micro update_user(id: i32, data: map): map {
    let updated = []
    loop user in _admin_users {
        if (user.id == id) {
            let u = {
                id: user.id
                username: data["username"] || user.username
                email: data["email"] || user.email
                role: data["role"] || user.role
                status: data["status"] || user.status
                created_at: user.created_at
                last_login: user.last_login
            }
            updated = [...updated, u]
        } else {
            updated = [...updated, user]
        }
    }
    _admin_users = updated
    return {}
}

micro delete_user(id: i32): void {
    let remaining = []
    loop user in _admin_users {
        if (user.id != id) {
            remaining = [...remaining, user]
        }
    }
    _admin_users = remaining
}

micro login(username: string, password: string): AuthState {
    if (username == "admin" && password == "admin") {
        _auth_state = AuthState {
            authenticated: true
            user: { username: "admin", role: "admin" }
            token: "jwt_token_admin"
        }
    }
    return _auth_state
}

micro logout(): void {
    _auth_state = AuthState { authenticated: false, user: {}, token: "" }
}

micro is_authenticated(): bool {
    return _auth_state.authenticated
}

micro get_current_user(): map {
    return _auth_state.user
}

micro get_dashboard_stats(): map {
    return {
        total_users: length(_admin_users)
        active_users: count_active()
        new_today: 2
        revenue: "¥128,500"
    }
}

micro count_active(): i32 {
    let count = 0
    loop user in _admin_users {
        if (user.status == "active") { count = count + 1 }
    }
    return count
}

micro length(lst: list): i32 {
    return 0
}
