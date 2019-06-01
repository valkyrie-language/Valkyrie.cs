# valkyrie-bootstrap: 常量池
# NyarVM 字节码的常量池结构
# 分三类: Int64, Float64, Utf8
# VM 内部字符串表示为 UTF-8 编码

micro const_pool_int_type(): i32 { return 0 }
micro const_pool_float_type(): i32 { return 1 }
micro const_pool_utf8_type(): i32 { return 2 }

micro const_pool_capacity(): i32 {
    return 256
}

micro const_pool_create(): i32 {
    return 0
}

micro const_pool_add_int(pool: i32, value: i64): i32 {
    return 0
}

micro const_pool_add_float(pool: i32, value: f64): i32 {
    return 0
}

micro const_pool_add_utf8(pool: i32, value: utf8): i32 {
    return 0
}

micro const_pool_get_int(pool: i32, index: i32): i64 {
    return 0
}

micro const_pool_get_float(pool: i32, index: i32): f64 {
    return 0.0
}

micro const_pool_get_utf8(pool: i32, index: i32): utf8 {
    return ""
}

micro const_pool_int_count(pool: i32): i32 {
    return 0
}

micro const_pool_float_count(pool: i32): i32 {
    return 0
}

micro const_pool_utf8_count(pool: i32): i32 {
    return 0
}
