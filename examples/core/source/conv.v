# valkyrie-core: 类型转换
# 抽象声明，由各平台适配层提供具体实现
# NyarVM 适配：std.adaptor.nyar/conv.v
# WASM 适配：std.adaptor.wasm
# 原生适配：std.adaptor.dotnet / std.adaptor.jvm

#region 数值类型转换

micro i32_to_i64(x: i32): i64

micro i64_to_i32(x: i64): i32

micro i32_to_f64(x: i32): f64

micro f64_to_i32(x: f64): i32

micro i64_to_f64(x: i64): f64

micro f64_to_i64(x: f64): i64

#endregion

#region 字符串编码转换

micro utf8_to_utf16(s: utf8): utf16

micro utf16_to_utf8(s: utf16): utf8

micro utf8_to_c_str(s: utf8): c_str

micro c_str_to_utf8(s: c_str): utf8

micro utf16_to_c_str(s: utf16): c_str

micro c_str_to_utf16(s: c_str): utf16

#endregion

#region 字符串长度

micro utf16_len(s: utf16): i32

micro c_str_len(s: c_str): i32

#endregion