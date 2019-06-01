# std.web_sdk: Console API
# [js_builtin] 直接映射 JS 内置全局对象，无依赖
# console 是副作用函数，不标注 pure
# JS 字符串内部为 UTF-16，所有字符串参数标注为 utf16

#region 日志

[js_builtin("console.log")]
micro console_log(msg: utf16)

[js_builtin("console.warn")]
micro console_warn(msg: utf16)

[js_builtin("console.error")]
micro console_error(msg: utf16)

[js_builtin("console.debug")]
micro console_debug(msg: utf16)

[js_builtin("console.info")]
micro console_info(msg: utf16)

#endregion

#region 分组

[js_builtin("console.group")]
micro console_group(label: utf16)

[js_builtin("console.groupEnd")]
micro console_group_end()

#endregion

#region 计时

[js_builtin("console.time")]
micro console_time(label: utf16)

[js_builtin("console.timeEnd")]
micro console_time_end(label: utf16)

#endregion

#region 清空

[js_builtin("console.clear")]
micro console_clear()

#endregion
