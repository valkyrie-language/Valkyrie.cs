# VOA JS Builtin — Storage
# [js_builtin] 直接映射 JS 内置 Storage API，无依赖
# Storage 有副作用（IO），不标注 pure

# localStorage

[js_builtin("localStorage.getItem")]
micro local_get(key: string): string

[js_builtin("localStorage.setItem")]
micro local_set(key: string, value: string)

[js_builtin("localStorage.removeItem")]
micro local_remove(key: string)

[js_builtin("localStorage.clear")]
micro local_clear()

[js_builtin("localStorage.length")]
micro local_len(): i32

# sessionStorage

[js_builtin("sessionStorage.getItem")]
micro session_get(key: string): string

[js_builtin("sessionStorage.setItem")]
micro session_set(key: string, value: string)

[js_builtin("sessionStorage.removeItem")]
micro session_remove(key: string)

[js_builtin("sessionStorage.clear")]
micro session_clear()

[js_builtin("sessionStorage.length")]
micro session_len(): i32
