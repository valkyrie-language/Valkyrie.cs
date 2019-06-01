# std.web_sdk: Timer API
# [js_builtin] 直接映射 JS 内置 Timer API，无依赖
# Timer 有副作用（调度），不标注 pure

# 定时器

[js_builtin("setTimeout")]
micro set_timeout(callback: i32, delay: i32): i32

[js_builtin("setInterval")]
micro set_interval(callback: i32, delay: i32): i32

[js_builtin("clearTimeout")]
micro clear_timeout(id: i32)

[js_builtin("clearInterval")]
micro clear_interval(id: i32)

# 动画帧

[js_builtin("requestAnimationFrame")]
micro raf(callback: i32): i32

[js_builtin("cancelAnimationFrame")]
micro caf(id: i32)

# 微任务

[js_builtin("queueMicrotask")]
micro queue_microtask(callback: i32)
