# VOA JS Builtin — Crypto
# [js_builtin] 直接映射 JS 内置 Crypto API，无依赖
# getRandomValues 有副作用（填充随机值），randomUUID 有副作用

# 随机值

[js_builtin("crypto.getRandomValues")]
micro crypto_random(buf: i32, len: i32)

[js_builtin("crypto.randomUUID")]
micro crypto_uuid(): string
