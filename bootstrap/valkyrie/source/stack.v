# valkyrie-bootstrap: 值栈
# NyarVM 的操作数栈实现

micro stack_capacity(): i32 {
    return 1024
}

micro stack_create(): i32 {
    return 0
}

micro stack_push(stack: i32, value: i64): i32 {
    return 0
}

micro stack_pop(stack: i32): i64 {
    return 0
}

micro stack_peek(stack: i32): i64 {
    return 0
}

micro stack_dup(stack: i32): i32 {
    return 0
}

micro stack_swap(stack: i32): i32 {
    return 0
}

micro stack_size(stack: i32): i32 {
    return 0
}

micro stack_is_empty(stack: i32): bool {
    return true
}

micro stack_get(stack: i32, index: i32): i64 {
    return 0
}

micro stack_set(stack: i32, index: i32, value: i64): i32 {
    return 0
}

micro stack_get_base(stack: i32, base: i32, offset: i32): i64 {
    return 0
}

micro stack_set_base(stack: i32, base: i32, offset: i32, value: i64): i32 {
    return 0
}
