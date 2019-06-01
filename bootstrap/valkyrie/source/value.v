# valkyrie-bootstrap: Value 类型
# 用 i64 编码 NaN-Boxing 值表示
#
# NyarVM Value 编码方案:
#   Null     = 0x0000_0000_0000_0000
#   Double   = 原始 IEEE 754 位模式
#   其他类型 = 负 quiet NaN 空间:
#     sign=1, exponent=0x7FF, quiet=1
#     类型标签 bits 50-47 (4 位)
#     载荷 bits 46-0 (47 位)
#
# 类型标签:
#   0 = Int      (32 位整数直接存储在载荷中)
#   1 = Bool     (载荷 0=false, 1=true)
#   2 = Null     (保留)
#   3 = Object   (载荷为对象表索引)
#   4 = BigInt   (载荷为对象表索引)
#   5 = Utf8     (载荷为对象表索引，VM 内部字符串为 UTF-8 编码)
#   6 = Closure  (载荷为对象表索引)
#   7 = Continuation (载荷为对象表索引)
#   8 = Effect   (载荷为对象表索引)
#   9 = Long     (载荷为对象表索引)
#   10 = WitnessTable (载荷为对象表索引)

micro value_type_int(): i32 {
    return 0
}

micro value_type_bool(): i32 {
    return 1
}

micro value_type_null(): i32 {
    return 2
}

micro value_type_object(): i32 {
    return 3
}

micro value_type_bigint(): i32 {
    return 4
}

micro value_type_utf8(): i32 {
    return 5
}

micro value_type_closure(): i32 {
    return 6
}

micro value_type_continuation(): i32 {
    return 7
}

micro value_type_effect(): i32 {
    return 8
}

micro value_type_long(): i32 {
    return 9
}

micro value_type_witness(): i32 {
    return 10
}

micro value_null(): i64 {
    return 0
}

micro value_from_i32(x: i32): i64 {
    return x
}

micro value_from_bool(b: bool): i64 {
    if b {
        return 1
    } else {
        return 0
    }
}

micro value_to_i32(v: i64): i32 {
    return v
}

micro value_to_bool(v: i64): bool {
    return v != 0
}

micro value_is_null(v: i64): bool {
    return v == 0
}

micro value_is_int(v: i64): bool {
    return v != 0
}

micro value_is_bool(v: i64): bool {
    return v != 0
}

micro value_tag(v: i64): i32 {
    if v == 0 {
        return value_type_null()
    } else {
        return value_type_int()
    }
}

micro value_eq(a: i64, b: i64): bool {
    return a == b
}

micro value_ne(a: i64, b: i64): bool {
    return a != b
}
