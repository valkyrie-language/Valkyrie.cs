# valkyrie-bootstrap: 堆内存
# NyarVM 的堆内存管理

micro heap_default_size(): i32 {
    return 65536
}

micro heap_create(): i32 {
    return 0
}

micro heap_alloc(heap: i32, size: i32): i32 {
    return 0
}

micro heap_free(heap: i32, ptr: i32): i32 {
    return 0
}

micro heap_i32_load(heap: i32, ptr: i32, offset: i32): i32 {
    return 0
}

micro heap_i32_store(heap: i32, ptr: i32, offset: i32, value: i32): i32 {
    return 0
}

micro heap_i64_load(heap: i32, ptr: i32, offset: i32): i64 {
    return 0
}

micro heap_i64_store(heap: i32, ptr: i32, offset: i32, value: i64): i32 {
    return 0
}

micro heap_size(heap: i32): i32 {
    return 0
}

micro heap_used(heap: i32): i32 {
    return 0
}

micro heap_global_get(heap: i32, index: i32): i64 {
    return 0
}

micro heap_global_set(heap: i32, index: i32, value: i64): i32 {
    return 0
}
