# valkyrie-bootstrap: Opcode 枚举
# NyarVM 字节码指令集定义
# 每个操作码占 1 字节，按功能分区

# 控制流 (0x00 - 0x0C)
micro op_nop(): i32 { return 0x00 }
micro op_jump(): i32 { return 0x01 }
micro op_jump_if_true(): i32 { return 0x02 }
micro op_jump_if_false(): i32 { return 0x03 }
micro op_call(): i32 { return 0x04 }
micro op_return(): i32 { return 0x05 }
micro op_tail_call(): i32 { return 0x06 }
micro op_throw(): i32 { return 0x07 }
micro op_catch(): i32 { return 0x08 }
micro op_yield(): i32 { return 0x0A }
micro op_resume(): i32 { return 0x0B }
micro op_effect_handle(): i32 { return 0x0C }

# 栈操作 (0x10 - 0x13)
micro op_const(): i32 { return 0x10 }
micro op_pop(): i32 { return 0x11 }
micro op_dup(): i32 { return 0x12 }
micro op_swap(): i32 { return 0x13 }

# 局部变量 (0x20 - 0x24)
micro op_load_local(): i32 { return 0x20 }
micro op_store_local(): i32 { return 0x21 }
micro op_load_arg(): i32 { return 0x22 }
micro op_load_global(): i32 { return 0x23 }
micro op_store_global(): i32 { return 0x24 }

# i32 算术 (0x30 - 0x3E)
micro op_i32_add(): i32 { return 0x30 }
micro op_i32_sub(): i32 { return 0x31 }
micro op_i32_mul(): i32 { return 0x32 }
micro op_i32_div_s(): i32 { return 0x33 }
micro op_i32_div_u(): i32 { return 0x34 }
micro op_i32_rem_s(): i32 { return 0x35 }
micro op_i32_rem_u(): i32 { return 0x36 }
micro op_i32_neg(): i32 { return 0x37 }
micro op_i32_and(): i32 { return 0x38 }
micro op_i32_or(): i32 { return 0x39 }
micro op_i32_xor(): i32 { return 0x3A }
micro op_i32_shl(): i32 { return 0x3B }
micro op_i32_shr_s(): i32 { return 0x3C }
micro op_i32_shr_u(): i32 { return 0x3D }
micro op_i32_not(): i32 { return 0x3E }

# i32 比较 (0x40 - 0x49)
micro op_i32_eq(): i32 { return 0x40 }
micro op_i32_ne(): i32 { return 0x41 }
micro op_i32_lt_s(): i32 { return 0x42 }
micro op_i32_lt_u(): i32 { return 0x43 }
micro op_i32_le_s(): i32 { return 0x44 }
micro op_i32_le_u(): i32 { return 0x45 }
micro op_i32_gt_s(): i32 { return 0x46 }
micro op_i32_gt_u(): i32 { return 0x47 }
micro op_i32_ge_s(): i32 { return 0x48 }
micro op_i32_ge_u(): i32 { return 0x49 }

# i64 算术 (0x50 - 0x55)
micro op_i64_add(): i32 { return 0x50 }
micro op_i64_sub(): i32 { return 0x51 }
micro op_i64_mul(): i32 { return 0x52 }
micro op_i64_div_s(): i32 { return 0x53 }
micro op_i64_div_u(): i32 { return 0x54 }
micro op_i64_neg(): i32 { return 0x55 }

# f32 算术 (0x60 - 0x64)
micro op_f32_add(): i32 { return 0x60 }
micro op_f32_sub(): i32 { return 0x61 }
micro op_f32_mul(): i32 { return 0x62 }
micro op_f32_div(): i32 { return 0x63 }
micro op_f32_neg(): i32 { return 0x64 }

# f64 算术 (0x70 - 0x74)
micro op_f64_add(): i32 { return 0x70 }
micro op_f64_sub(): i32 { return 0x71 }
micro op_f64_mul(): i32 { return 0x72 }
micro op_f64_div(): i32 { return 0x73 }
micro op_f64_neg(): i32 { return 0x74 }

# 类型转换 (0x80 - 0x85)
micro op_i32_extend_i64_s(): i32 { return 0x80 }
micro op_i32_extend_i64_u(): i32 { return 0x81 }
micro op_i64_trunc_i32_s(): i32 { return 0x82 }
micro op_i64_trunc_i32_u(): i32 { return 0x83 }
micro op_i32_to_f32_s(): i32 { return 0x84 }
micro op_i32_to_f64_s(): i32 { return 0x85 }

# 内存操作 (0x90 - 0x95)
micro op_alloc(): i32 { return 0x90 }
micro op_free(): i32 { return 0x91 }
micro op_i32_load(): i32 { return 0x92 }
micro op_i32_store(): i32 { return 0x93 }
micro op_i64_load(): i32 { return 0x94 }
micro op_i64_store(): i32 { return 0x95 }

# 对象操作 (0xA0 - 0xA8)
micro op_new_object(): i32 { return 0xA0 }
micro op_get_field(): i32 { return 0xA1 }
micro op_set_field(): i32 { return 0xA2 }
micro op_get_index(): i32 { return 0xA3 }
micro op_set_index(): i32 { return 0xA4 }
micro op_length(): i32 { return 0xA5 }
micro op_new_closure(): i32 { return 0xA6 }
micro op_get_upvalue(): i32 { return 0xA7 }
micro op_set_upvalue(): i32 { return 0xA8 }

# 字符串操作 (0xB0 - 0xB3)
micro op_string_concat(): i32 { return 0xB0 }
micro op_string_len_bytes(): i32 { return 0xB1 }
micro op_string_len_chars(): i32 { return 0xB2 }
micro op_string_substr(): i32 { return 0xB3 }

# BigInt 操作 (0xC0 - 0xC2)
micro op_bigint_add(): i32 { return 0xC0 }
micro op_bigint_sub(): i32 { return 0xC1 }
micro op_bigint_mul(): i32 { return 0xC2 }

# 内置函数 (0xD0 - 0xDC)
micro op_print(): i32 { return 0xD0 }
micro op_println(): i32 { return 0xD1 }
micro op_exit(): i32 { return 0xD2 }
micro op_get_time(): i32 { return 0xD3 }
micro op_sleep(): i32 { return 0xD4 }
micro op_math_sin(): i32 { return 0xD8 }
micro op_math_cos(): i32 { return 0xD9 }
micro op_math_sqrt(): i32 { return 0xDA }
micro op_math_abs(): i32 { return 0xDB }
micro op_math_rand(): i32 { return 0xDC }

# 方言分派 (0xE0)
micro op_builtin_call(): i32 { return 0xE0 }

# 操作码分类
micro opcode_is_control(op: i32): bool {
    return op <= 0x0C
}

micro opcode_is_stack(op: i32): bool {
    return op >= 0x10 && op <= 0x13
}

micro opcode_is_local(op: i32): bool {
    return op >= 0x20 && op <= 0x24
}

micro opcode_is_i32_arith(op: i32): bool {
    return op >= 0x30 && op <= 0x3E
}

micro opcode_is_i32_cmp(op: i32): bool {
    return op >= 0x40 && op <= 0x49
}

micro opcode_is_i64_arith(op: i32): bool {
    return op >= 0x50 && op <= 0x55
}

micro opcode_is_f64_arith(op: i32): bool {
    return op >= 0x70 && op <= 0x74
}

micro opcode_is_memory(op: i32): bool {
    return op >= 0x90 && op <= 0x95
}

micro opcode_is_object(op: i32): bool {
    return op >= 0xA0 && op <= 0xA8
}

micro opcode_has_i32_operand(op: i32): bool {
    if op == op_jump() { return true }
    if op == op_jump_if_true() { return true }
    if op == op_jump_if_false() { return true }
    if op == op_call() { return true }
    if op == op_tail_call() { return true }
    if op == op_catch() { return true }
    if op == op_resume() { return true }
    if op == op_effect_handle() { return true }
    if op == op_const() { return true }
    if op == op_load_local() { return true }
    if op == op_store_local() { return true }
    if op == op_load_arg() { return true }
    if op == op_load_global() { return true }
    if op == op_store_global() { return true }
    if op == op_alloc() { return true }
    if op == op_i32_load() { return true }
    if op == op_i32_store() { return true }
    if op == op_i64_load() { return true }
    if op == op_i64_store() { return true }
    if op == op_new_object() { return true }
    if op == op_get_field() { return true }
    if op == op_set_field() { return true }
    if op == op_new_closure() { return true }
    if op == op_get_upvalue() { return true }
    if op == op_set_upvalue() { return true }
    return false
}

micro opcode_size(op: i32): i32 {
    if opcode_has_i32_operand(op) {
        return 5
    } else {
        if op == op_builtin_call() {
            return 13
        } else {
            return 1
        }
    }
}
