# valkyrie-core: 算术运算
# 纯 Valkyrie 实现，不依赖任何运行时
# 编译器后端自动将原生运算符映射为目标平台指令

#region i32 算术

micro i32_add(a: i32, b: i32): i32 {
    return a + b
}

micro i32_sub(a: i32, b: i32): i32 {
    return a - b
}

micro i32_mul(a: i32, b: i32): i32 {
    return a * b
}

micro i32_div(a: i32, b: i32): i32 {
    return a / b
}

micro i32_rem(a: i32, b: i32): i32 {
    return a % b
}

micro i32_neg(a: i32): i32 {
    return -a
}

#endregion

#region i32 位运算

micro i32_and(a: i32, b: i32): i32 {
    return a & b
}

micro i32_or(a: i32, b: i32): i32 {
    return a | b
}

micro i32_xor(a: i32, b: i32): i32 {
    return a ^ b
}

micro i32_shl(a: i32, b: i32): i32 {
    return a << b
}

micro i32_shr(a: i32, b: i32): i32 {
    return a >> b
}

micro i32_not(a: i32): i32 {
    return ~a
}

#endregion

#region i64 算术

micro i64_add(a: i64, b: i64): i64 {
    return a + b
}

micro i64_sub(a: i64, b: i64): i64 {
    return a - b
}

micro i64_mul(a: i64, b: i64): i64 {
    return a * b
}

micro i64_div(a: i64, b: i64): i64 {
    return a / b
}

micro i64_neg(a: i64): i64 {
    return -a
}

#endregion

#region f64 算术

micro f64_add(a: f64, b: f64): f64 {
    return a + b
}

micro f64_sub(a: f64, b: f64): f64 {
    return a - b
}

micro f64_mul(a: f64, b: f64): f64 {
    return a * b
}

micro f64_div(a: f64, b: f64): f64 {
    return a / b
}

micro f64_neg(a: f64): f64 {
    return -a
}

#endregion