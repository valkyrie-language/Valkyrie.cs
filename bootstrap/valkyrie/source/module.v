# valkyrie-bootstrap: 模块
# NyarVM 的模块加载与链接
# VM 内部字符串表示为 UTF-8 编码

micro module_create(name: utf8): i32 {
    return 0
}

micro module_name(module: i32): utf8 {
    return ""
}

micro module_function_count(module: i32): i32 {
    return 0
}

micro module_add_function(module: i32, func: i32): i32 {
    return 0
}

micro module_get_function(module: i32, index: i32): i32 {
    return 0
}

micro module_find_function(module: i32, name: utf8): i32 {
    return -1
}

micro module_add_import(module: i32, module_name: utf8, symbol_name: utf8): i32 {
    return 0
}

micro module_add_export(module: i32, name: utf8, func_index: i32): i32 {
    return 0
}

micro module_find_export(module: i32, name: utf8): i32 {
    return -1
}

micro module_constant_pool(module: i32): i32 {
    return 0
}

micro module_bytecode(module: i32): i32 {
    return 0
}

micro module_bytecode_length(module: i32): i32 {
    return 0
}

micro module_load_bytecode(module: i32, bytecode: i32, length: i32): i32 {
    return 0
}

micro module_read_u8(module: i32, offset: i32): i32 {
    return 0
}

micro module_read_i32(module: i32, offset: i32): i32 {
    return 0
}

micro module_read_i64(module: i32, offset: i32): i64 {
    return 0
}

micro module_read_f64(module: i32, offset: i32): f64 {
    return 0.0
}
