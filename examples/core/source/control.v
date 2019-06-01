# valkyrie-core: 控制流原语
# 纯 Valkyrie 实现，不依赖任何运行时

#region 空操作

micro nop(): i32 {
    return 0
}

#endregion

#region select（三元表达式）

micro select_i32(cond: bool, then_val: i32, else_val: i32): i32 {
    if (cond) {
        return then_val
    } else {
        return else_val
    }
}

micro select_i64(cond: bool, then_val: i64, else_val: i64): i64 {
    if (cond) {
        return then_val
    } else {
        return else_val
    }
}

micro select_f64(cond: bool, then_val: f64, else_val: f64): f64 {
    if (cond) {
        return then_val
    } else {
        return else_val
    }
}

micro select_bool(cond: bool, then_val: bool, else_val: bool): bool {
    if (cond) {
        return then_val
    } else {
        return else_val
    }
}

#endregion

#region 循环

micro loop_n(body: i32, n: i32): i32 {
    let mut i: i32 = 0
    let mut result: i32 = 0
    while (i < n) {
        result = result + body
        i += 1
    }
    return result
}

micro while_do_i32(cond: i32, limit: i32): i32 {
    let mut acc: i32 = 0
    while (cond < limit) {
        acc += cond
        cond += 1
    }
    return acc
}

#endregion