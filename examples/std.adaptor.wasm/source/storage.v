# std.web_sdk: Storage API
# [js_builtin] 直接映射 JS 内置 Storage API，无依赖
# Storage 有副作用（IO），不标注 pure
# JS 字符串内部为 UTF-16，所有字符串参数标注为 utf16

#region localStorage

[js_builtin("localStorage.getItem")]
micro local_get(key: utf16): utf16

[js_builtin("localStorage.setItem")]
micro local_set(key: utf16, value: utf16)

[js_builtin("localStorage.removeItem")]
micro local_remove(key: utf16)

[js_builtin("localStorage.clear")]
micro local_clear()

[js_builtin("localStorage.length")]
micro local_len(): i32

#endregion

#region sessionStorage

[js_builtin("sessionStorage.getItem")]
micro session_get(key: utf16): utf16

[js_builtin("sessionStorage.setItem")]
micro session_set(key: utf16, value: utf16)

[js_builtin("sessionStorage.removeItem")]
micro session_remove(key: utf16)

[js_builtin("sessionStorage.clear")]
micro session_clear()

[js_builtin("sessionStorage.length")]
micro session_len(): i32

#endregion
