# valkyrie-bootstrap: 函数与帧
# NyarVM 的函数定义和调用帧
# VM 内部字符串表示为 UTF-8 编码

micro function_create(name: utf8, arity: i32, local_count: i32, code_offset: i32, code_length: i32): i32 {
    return 0
}

micro function_name(func: i32): utf8 {
    return ""
}

micro function_arity(func: i32): i32 {
    return 0
}

micro function_local_count(func: i32): i32 {
    return 0
}

micro function_code_offset(func: i32): i32 {
    return 0
}

micro function_code_length(func: i32): i32 {
    return 0
}

micro frame_create(func: i32, return_pc: i32, stack_base: i32): i32 {
    return 0
}

micro frame_function(frame: i32): i32 {
    return 0
}

micro frame_return_pc(frame: i32): i32 {
    return 0
}

micro frame_stack_base(frame: i32): i32 {
    return 0
}

micro frame_pc(frame: i32): i32 {
    return 0
}

micro frame_set_pc(frame: i32, pc: i32): i32 {
    return pc
}

micro frame_local_count(frame: i32): i32 {
    let func: i32 = frame_function(frame)
    let arity: i32 = function_arity(func)
    let locals: i32 = function_local_count(func)
    return arity + locals
}
