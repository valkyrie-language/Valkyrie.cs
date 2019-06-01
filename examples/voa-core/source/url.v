# VOA JS Builtin — URL
# [js_builtin] 直接映射 JS 内置 URL API，无依赖
# URL 操作是纯函数，标注 pure

# URL 解析

[js_builtin("URL"), pure]
micro url_new(url: string): i32

[js_builtin("URL.href"), pure]
micro url_href(handle: i32): string

[js_builtin("URL.pathname"), pure]
micro url_pathname(handle: i32): string

[js_builtin("URL.search"), pure]
micro url_search(handle: i32): string

[js_builtin("URL.hash"), pure]
micro url_hash(handle: i32): string

[js_builtin("URL.host"), pure]
micro url_host(handle: i32): string

[js_builtin("URL.origin"), pure]
micro url_origin(handle: i32): string

# URLSearchParams

[js_builtin("URLSearchParams"), pure]
micro url_params_new(): i32

[js_builtin("URLSearchParams.get"), pure]
micro url_params_get(handle: i32, name: string): string

[js_builtin("URLSearchParams.set")]
micro url_params_set(handle: i32, name: string, value: string)

[js_builtin("URLSearchParams.has"), pure]
micro url_params_has(handle: i32, name: string): i32

[js_builtin("URLSearchParams.delete")]
micro url_params_del(handle: i32, name: string)
