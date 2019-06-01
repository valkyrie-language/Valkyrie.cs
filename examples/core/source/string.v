# valkyrie-core: 字符串操作
# 抽象声明，由各平台适配层提供具体实现
# NyarVM 适配：std.adaptor.nyar/string.v

#region utf8 操作

micro utf8_concat(a: utf8, b: utf8): utf8

micro utf8_len_bytes(s: utf8): i32

micro utf8_len_chars(s: utf8): i32

micro utf8_substr(s: utf8, start: i32, len: i32): utf8

micro utf8_eq(a: utf8, b: utf8): bool

micro utf8_ne(a: utf8, b: utf8): bool

#endregion