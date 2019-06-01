# std.web_sdk: URL API
# [js_builtin] 直接映射 JS 内置 URL API，无依赖
# URL 操作是纯函数，标注 pure
# JS 字符串内部为 UTF-16，所有字符串参数标注为 utf16

#region URL 解析

[js_builtin("URL"), pure]
micro url_new(url: utf16): i32

[js_builtin("URL.href"), pure]
micro url_href(handle: i32): utf16

[js_builtin("URL.pathname"), pure]
micro url_pathname(handle: i32): utf16

[js_builtin("URL.search"), pure]
micro url_search(handle: i32): utf16

[js_builtin("URL.hash"), pure]
micro url_hash(handle: i32): utf16

[js_builtin("URL.host"), pure]
micro url_host(handle: i32): utf16

[js_builtin("URL.origin"), pure]
micro url_origin(handle: i32): utf16

#endregion

#region URLSearchParams

[js_builtin("URLSearchParams"), pure]
micro url_params_new(): i32

[js_builtin("URLSearchParams.get"), pure]
micro url_params_get(handle: i32, name: utf16): utf16

[js_builtin("URLSearchParams.set")]
micro url_params_set(handle: i32, name: utf16, value: utf16)

[js_builtin("URLSearchParams.has"), pure]
micro url_params_has(handle: i32, name: utf16): i32

[js_builtin("URLSearchParams.delete")]
micro url_params_del(handle: i32, name: utf16)

#endregion
