# std.web_sdk: Performance API
# [js_builtin] 直接映射 JS 内置 Performance API，无依赖
# now() 是纯函数，mark/measure 有副作用
# JS 字符串内部为 UTF-16，所有字符串参数标注为 utf16

#region 时间戳

[js_builtin("performance.now"), pure]
micro perf_now(): f64

#endregion

#region 性能标记

[js_builtin("performance.mark")]
micro perf_mark(name: utf16)

[js_builtin("performance.measure")]
micro perf_measure(name: utf16, start: utf16, end: utf16)

#endregion

#region 时间原点

[js_builtin("performance.timeOrigin"), pure]
micro perf_time_origin(): f64

#endregion
