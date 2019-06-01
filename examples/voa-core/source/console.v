# VOA JS Builtin — Console
# [js_builtin] 直接映射 JS 内置全局对象，无依赖
# console 是副作用函数，不标注 pure

# 日志

[js_builtin("console.log")]
micro console_log(msg: string)

[js_builtin("console.warn")]
micro console_warn(msg: string)

[js_builtin("console.error")]
micro console_error(msg: string)

[js_builtin("console.debug")]
micro console_debug(msg: string)

[js_builtin("console.info")]
micro console_info(msg: string)

# 分组

[js_builtin("console.group")]
micro console_group(label: string)

[js_builtin("console.groupEnd")]
micro console_group_end()

# 计时

[js_builtin("console.time")]
micro console_time(label: string)

[js_builtin("console.timeEnd")]
micro console_time_end(label: string)

# 清空

[js_builtin("console.clear")]
micro console_clear()
