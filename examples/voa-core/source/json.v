# VOA JS Builtin — JSON
# [js_builtin] 直接映射 JS 内置 JSON API，无依赖
# JSON.parse 有副作用（可能抛异常），JSON.stringify 也有副作用（可能抛异常）
# 但从纯函数语义上，给定相同输入输出确定，标注 pure

# 解析

[js_builtin("JSON.parse"), pure]
micro json_parse(text: string): string

# 序列化

[js_builtin("JSON.stringify"), pure]
micro json_stringify(value: string): string
