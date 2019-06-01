# valkyrie-core: 原始类型定义
# 直接映射 NyarVM 的 Value 类型系统
# Value = Int | Long | Double | Bool | Null | Object | BigInt | Utf8 | Closure | Continuation | Effect | WitnessTable

micro identity(x: i32): i32 {
    return x
}

micro identity_long(x: i64): i64 {
    return x
}

micro identity_f64(x: f64): f64 {
    return x
}

micro identity_bool(x: bool): bool {
    return x
}

micro identity_utf8(x: utf8): utf8 {
    return x
}

micro default_i32(): i32 {
    return 0
}

micro default_i64(): i64 {
    return 0
}

micro default_f64(): f64 {
    return 0.0
}

micro default_bool(): bool {
    return false
}

micro default_utf8(): utf8 {
    return ""
}

micro is_null(value: i32): bool {
    return value == 0
}

micro is_not_null(value: i32): bool {
    return value != 0
}

micro is_true(value: bool): bool {
    return value
}

micro is_false(value: bool): bool {
    if (value) {
        return false
    } else {
        return true
    }
}

micro sizeof_i32(): i32 {
    return 4
}

micro sizeof_i64(): i32 {
    return 8
}

micro sizeof_f64(): i32 {
    return 8
}

micro sizeof_ptr(): i32 {
    return 8
}
