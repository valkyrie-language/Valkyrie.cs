# std.adaptor.nyar: 算术运算 [vm] 绑定
# 将 valkyrie-core 的算术运算映射为 NyarVM opcode
# 编译器使用 [vm] 绑定直接内联为对应指令，零函数调用开销

#region i32 算术

[vm("i32_add")]
micro i32_add(a: i32, b: i32): i32

[vm("i32_sub")]
micro i32_sub(a: i32, b: i32): i32

[vm("i32_mul")]
micro i32_mul(a: i32, b: i32): i32

[vm("i32_div")]
micro i32_div(a: i32, b: i32): i32

[vm("i32_rem")]
micro i32_rem(a: i32, b: i32): i32

[vm("i32_neg")]
micro i32_neg(a: i32): i32

#endregion

#region i32 位运算

[vm("i32_and")]
micro i32_and(a: i32, b: i32): i32

[vm("i32_or")]
micro i32_or(a: i32, b: i32): i32

[vm("i32_xor")]
micro i32_xor(a: i32, b: i32): i32

[vm("i32_shl")]
micro i32_shl(a: i32, b: i32): i32

[vm("i32_shr")]
micro i32_shr(a: i32, b: i32): i32

[vm("i32_not")]
micro i32_not(a: i32): i32

#endregion

#region i64 算术

[vm("i64_add")]
micro i64_add(a: i64, b: i64): i64

[vm("i64_sub")]
micro i64_sub(a: i64, b: i64): i64

[vm("i64_mul")]
micro i64_mul(a: i64, b: i64): i64

[vm("i64_div")]
micro i64_div(a: i64, b: i64): i64

[vm("i64_neg")]
micro i64_neg(a: i64): i64

#endregion

#region f64 算术

[vm("f64_add")]
micro f64_add(a: f64, b: f64): f64

[vm("f64_sub")]
micro f64_sub(a: f64, b: f64): f64

[vm("f64_mul")]
micro f64_mul(a: f64, b: f64): f64

[vm("f64_div")]
micro f64_div(a: f64, b: f64): f64

[vm("f64_neg")]
micro f64_neg(a: f64): f64

#endregion