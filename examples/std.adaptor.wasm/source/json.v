# std.web_sdk: JSON API
# [js_builtin] 直接映射 JS 内置 JSON API，无依赖
# 给定相同输入输出确定，标注 pure
# JS 字符串内部为 UTF-16，所有字符串参数标注为 utf16

#region 解析

[js_builtin("JSON.parse"), pure]
micro json_parse(text: utf16): utf16

#endregion

#region 序列化

[js_builtin("JSON.stringify"), pure]
micro json_stringify(value: utf16): utf16

#endregion
