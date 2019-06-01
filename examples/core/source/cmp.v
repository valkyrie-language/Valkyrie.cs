# valkyrie-core: 比较运算
# 纯 Valkyrie 实现，不依赖任何运行时
# 编译器后端自动将原生比较运算符映射为目标平台指令

#region i32 比较

micro i32_eq(a: i32, b: i32): bool {
    return a == b
}

micro i32_ne(a: i32, b: i32): bool {
    return a != b
}

micro i32_lt(a: i32, b: i32): bool {
    return a < b
}

micro i32_le(a: i32, b: i32): bool {
    return a <= b
}

micro i32_gt(a: i32, b: i32): bool {
    return a > b
}

micro i32_ge(a: i32, b: i32): bool {
    return a >= b
}

#endregion

#region bool 逻辑（短路求值，Valkyrie 实现）

micro bool_eq(a: bool, b: bool): bool {
    if (a == b) {
        return true
    } else {
        return false
    }
}

micro bool_not(a: bool): bool {
    if (a) {
        return false
    } else {
        return true
    }
}

micro bool_and(a: bool, b: bool): bool {
    if (a) {
        if (b) {
            return true
        } else {
            return false
        }
    } else {
        return false
    }
}

micro bool_or(a: bool, b: bool): bool {
    if (a) {
        return true
    } else {
        if (b) {
            return true
        } else {
            return false
        }
    }
}

micro bool_xor(a: bool, b: bool): bool {
    if (a) {
        if (b) {
            return false
        } else {
            return true
        }
    } else {
        if (b) {
            return true
        } else {
            return false
        }
    }
}

#endregion