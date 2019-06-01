# std.adaptor.nyar: 字符串操作 [vm] 绑定
# 将 valkyrie-core 的字符串操作映射为 NyarVM opcode
# NyarVM 内部字符串表示为 UTF-8 编码

#region utf8 操作

[vm("str_concat")]
micro utf8_concat(a: utf8, b: utf8): utf8

[vm("str_len_bytes")]
micro utf8_len_bytes(s: utf8): i32

[vm("str_len_chars")]
micro utf8_len_chars(s: utf8): i32

[vm("str_substr")]
micro utf8_substr(s: utf8, start: i32, len: i32): utf8

[vm("str_eq")]
micro utf8_eq(a: utf8, b: utf8): bool

[vm("str_ne")]
micro utf8_ne(a: utf8, b: utf8): bool

#endregion