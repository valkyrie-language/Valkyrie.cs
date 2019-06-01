# VOA JS Builtin — Performance
# [js_builtin] 直接映射 JS 内置 Performance API，无依赖
# now() 是纯函数，mark/measure 有副作用

# 时间戳

[js_builtin("performance.now"), pure]
micro perf_now(): f64

# 性能标记

[js_builtin("performance.mark")]
micro perf_mark(name: string)

[js_builtin("performance.measure")]
micro perf_measure(name: string, start: string, end: string)

# 时间原点

[js_builtin("performance.timeOrigin"), pure]
micro perf_time_origin(): f64
