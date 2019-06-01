# VOA JS Builtin — Fetch
# [js_builtin] 直接映射 JS 内置 fetch API，无依赖
# fetch 有副作用（网络 IO），不标注 pure

# 请求

[js_builtin("fetch")]
micro http_fetch(url: string): i32

[js_builtin("Request")]
micro http_request(url: string, options: i32): i32

# 响应

[js_builtin("Response.json")]
micro response_json(handle: i32): i32

[js_builtin("Response.text")]
micro response_text(handle: i32): string

[js_builtin("Response.ok")]
micro response_ok(handle: i32): i32

[js_builtin("Response.status")]
micro response_status(handle: i32): i32

[js_builtin("Response.headers")]
micro response_headers(handle: i32): i32

# Headers

[js_builtin("Headers")]
micro headers_new(): i32

[js_builtin("Headers.append")]
micro headers_append(handle: i32, name: string, value: string)

[js_builtin("Headers.get")]
micro headers_get(handle: i32, name: string): string
