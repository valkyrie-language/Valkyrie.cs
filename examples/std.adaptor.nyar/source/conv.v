# std.adaptor.nyar: 类型转换 [vm] 绑定
# 将 valkyrie-core 的类型转换映射为 NyarVM opcode
# 编译器将 [vm] 调用内联为对应转换指令

#region 数值类型转换

[vm("i32_to_i64")]
micro i32_to_i64(x: i32): i64

[vm("i64_to_i32")]
micro i64_to_i32(x: i64): i32

[vm("i32_to_f64")]
micro i32_to_f64(x: i32): f64

[vm("f64_to_i32")]
micro f64_to_i32(x: f64): i32

[vm("i64_to_f64")]
micro i64_to_f64(x: i64): f64

[vm("f64_to_i64")]
micro f64_to_i64(x: f64): i64

#endregion

#region 字符串编码转换

[vm("utf8_to_utf16")]
micro utf8_to_utf16(s: utf8): utf16

[vm("utf16_to_utf8")]
micro utf16_to_utf8(s: utf16): utf8

[vm("utf8_to_c_str")]
micro utf8_to_c_str(s: utf8): c_str

[vm("c_str_to_utf8")]
micro c_str_to_utf8(s: c_str): utf8

[vm("utf16_to_c_str")]
micro utf16_to_c_str(s: utf16): c_str

[vm("c_str_to_utf16")]
micro c_str_to_utf16(s: c_str): utf16

#endregion

#region 字符串长度

[vm("utf16_len")]
micro utf16_len(s: utf16): i32

[vm("c_str_len")]
micro c_str_len(s: c_str): i32

#endregion