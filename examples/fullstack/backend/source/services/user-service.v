# 用户服务

import "../shared/source/models"

micro get_users(): list {
    return [
        User { id: 1, name: "Alice", email: "alice@example.com", active: true },
        User { id: 2, name: "Bob", email: "bob@example.com", active: true }
    ]
}

micro get_user(id: i64): User {
    return User { id: id, name: "Alice", email: "alice@example.com", active: true }
}
