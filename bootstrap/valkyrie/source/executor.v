# valkyrie-bootstrap: 指令执行器
# NyarVM 的核心 — 字节码解释执行引擎
#
# 执行循环:
#   1. 从 bytecode[frame.pc] 读取操作码
#   2. 分派到对应的处理逻辑
#   3. 更新 frame.pc
#   4. 重复直到 Return 或异常
#
# 控制结果:
#   Continue = 0
#   Return   = 1
#   Throw    = 2
#   Yield    = 3

micro control_continue(): i32 { return 0 }
micro control_return(): i32 { return 1 }
micro control_throw(): i32 { return 2 }
micro control_yield(): i32 { return 3 }

micro executor_step_i32_add(stack: i32): i32 {
    let b: i64 = stack_pop(stack)
    let a: i64 = stack_pop(stack)
    let result: i64 = a + b
    stack_push(stack, result)
    return control_continue()
}

micro executor_step_i32_sub(stack: i32): i32 {
    let b: i64 = stack_pop(stack)
    let a: i64 = stack_pop(stack)
    let result: i64 = a - b
    stack_push(stack, result)
    return control_continue()
}

micro executor_step_i32_mul(stack: i32): i32 {
    let b: i64 = stack_pop(stack)
    let a: i64 = stack_pop(stack)
    let result: i64 = a * b
    stack_push(stack, result)
    return control_continue()
}

micro executor_step_i32_div_s(stack: i32): i32 {
    let b: i64 = stack_pop(stack)
    let a: i64 = stack_pop(stack)
    if b == 0 {
        stack_push(stack, 0)
    } else {
        stack_push(stack, a / b)
    }
    return control_continue()
}

micro executor_step_i32_rem_s(stack: i32): i32 {
    let b: i64 = stack_pop(stack)
    let a: i64 = stack_pop(stack)
    if b == 0 {
        stack_push(stack, 0)
    } else {
        stack_push(stack, a % b)
    }
    return control_continue()
}

micro executor_step_i32_neg(stack: i32): i32 {
    let a: i64 = stack_pop(stack)
    stack_push(stack, -a)
    return control_continue()
}

micro executor_step_i32_and(stack: i32): i32 {
    let b: i64 = stack_pop(stack)
    let a: i64 = stack_pop(stack)
    stack_push(stack, a & b)
    return control_continue()
}

micro executor_step_i32_or(stack: i32): i32 {
    let b: i64 = stack_pop(stack)
    let a: i64 = stack_pop(stack)
    stack_push(stack, a | b)
    return control_continue()
}

micro executor_step_i32_xor(stack: i32): i32 {
    let b: i64 = stack_pop(stack)
    let a: i64 = stack_pop(stack)
    stack_push(stack, a ^ b)
    return control_continue()
}

micro executor_step_i32_not(stack: i32): i32 {
    let a: i64 = stack_pop(stack)
    stack_push(stack, ~a)
    return control_continue()
}

micro executor_step_i32_eq(stack: i32): i32 {
    let b: i64 = stack_pop(stack)
    let a: i64 = stack_pop(stack)
    if a == b {
        stack_push(stack, 1)
    } else {
        stack_push(stack, 0)
    }
    return control_continue()
}

micro executor_step_i32_ne(stack: i32): i32 {
    let b: i64 = stack_pop(stack)
    let a: i64 = stack_pop(stack)
    if a != b {
        stack_push(stack, 1)
    } else {
        stack_push(stack, 0)
    }
    return control_continue()
}

micro executor_step_i32_lt_s(stack: i32): i32 {
    let b: i64 = stack_pop(stack)
    let a: i64 = stack_pop(stack)
    if a < b {
        stack_push(stack, 1)
    } else {
        stack_push(stack, 0)
    }
    return control_continue()
}

micro executor_step_i32_le_s(stack: i32): i32 {
    let b: i64 = stack_pop(stack)
    let a: i64 = stack_pop(stack)
    if a <= b {
        stack_push(stack, 1)
    } else {
        stack_push(stack, 0)
    }
    return control_continue()
}

micro executor_step_i32_gt_s(stack: i32): i32 {
    let b: i64 = stack_pop(stack)
    let a: i64 = stack_pop(stack)
    if a > b {
        stack_push(stack, 1)
    } else {
        stack_push(stack, 0)
    }
    return control_continue()
}

micro executor_step_i32_ge_s(stack: i32): i32 {
    let b: i64 = stack_pop(stack)
    let a: i64 = stack_pop(stack)
    if a >= b {
        stack_push(stack, 1)
    } else {
        stack_push(stack, 0)
    }
    return control_continue()
}

micro executor_step_const(stack: i32, value: i64): i32 {
    stack_push(stack, value)
    return control_continue()
}

micro executor_step_pop(stack: i32): i32 {
    stack_pop(stack)
    return control_continue()
}

micro executor_step_dup(stack: i32): i32 {
    stack_dup(stack)
    return control_continue()
}

micro executor_step_swap(stack: i32): i32 {
    stack_swap(stack)
    return control_continue()
}

micro executor_step_load_local(stack: i32, frame: i32, index: i32): i32 {
    let base: i32 = frame_stack_base(frame)
    let value: i64 = stack_get_base(stack, base, index)
    stack_push(stack, value)
    return control_continue()
}

micro executor_step_store_local(stack: i32, frame: i32, index: i32): i32 {
    let value: i64 = stack_pop(stack)
    let base: i32 = frame_stack_base(frame)
    stack_set_base(stack, base, index, value)
    return control_continue()
}

micro executor_step_load_arg(stack: i32, frame: i32, index: i32): i32 {
    let base: i32 = frame_stack_base(frame)
    let value: i64 = stack_get_base(stack, base, index)
    stack_push(stack, value)
    return control_continue()
}

micro executor_step_return(stack: i32): i32 {
    return control_return()
}

micro executor_step_jump(offset: i32): i32 {
    return offset
}

micro executor_step_jump_if_false(stack: i32, offset: i32): i32 {
    let cond: i64 = stack_pop(stack)
    if cond == 0 {
        return offset
    } else {
        return 0
    }
}

micro executor_step_jump_if_true(stack: i32, offset: i32): i32 {
    let cond: i64 = stack_pop(stack)
    if cond != 0 {
        return offset
    } else {
        return 0
    }
}

micro executor_step_nop(): i32 {
    return control_continue()
}

micro executor_dispatch_i32_arith(stack: i32, op: i32): i32 {
    if op == op_i32_add() { return executor_step_i32_add(stack) }
    if op == op_i32_sub() { return executor_step_i32_sub(stack) }
    if op == op_i32_mul() { return executor_step_i32_mul(stack) }
    if op == op_i32_div_s() { return executor_step_i32_div_s(stack) }
    if op == op_i32_rem_s() { return executor_step_i32_rem_s(stack) }
    if op == op_i32_neg() { return executor_step_i32_neg(stack) }
    if op == op_i32_and() { return executor_step_i32_and(stack) }
    if op == op_i32_or() { return executor_step_i32_or(stack) }
    if op == op_i32_xor() { return executor_step_i32_xor(stack) }
    if op == op_i32_not() { return executor_step_i32_not(stack) }
    return control_continue()
}

micro executor_dispatch_i32_cmp(stack: i32, op: i32): i32 {
    if op == op_i32_eq() { return executor_step_i32_eq(stack) }
    if op == op_i32_ne() { return executor_step_i32_ne(stack) }
    if op == op_i32_lt_s() { return executor_step_i32_lt_s(stack) }
    if op == op_i32_le_s() { return executor_step_i32_le_s(stack) }
    if op == op_i32_gt_s() { return executor_step_i32_gt_s(stack) }
    if op == op_i32_ge_s() { return executor_step_i32_ge_s(stack) }
    return control_continue()
}

micro executor_dispatch_stack(stack: i32, op: i32, operand: i64): i32 {
    if op == op_const() { return executor_step_const(stack, operand) }
    if op == op_pop() { return executor_step_pop(stack) }
    if op == op_dup() { return executor_step_dup(stack) }
    if op == op_swap() { return executor_step_swap(stack) }
    return control_continue()
}

micro executor_dispatch_control(stack: i32, op: i32, operand: i32): i32 {
    if op == op_nop() { return executor_step_nop() }
    if op == op_return() { return executor_step_return(stack) }
    if op == op_jump() { return executor_step_jump(operand) }
    if op == op_jump_if_false() { return executor_step_jump_if_false(stack, operand) }
    if op == op_jump_if_true() { return executor_step_jump_if_true(stack, operand) }
    return control_continue()
}
