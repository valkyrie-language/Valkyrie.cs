# std.adaptor.nyar: 内存操作 [vm] 绑定
# 将 valkyrie-core 的内存操作映射为 NyarVM opcode
# 编译器将 [vm] 调用内联为对应内存指令

#region 分配与释放

[vm("alloc")]
micro alloc(size: i32): i32

[vm("free")]
micro free(ptr: i32): i32

#endregion

#region i32 加载与存储

[vm("i32_load")]
micro i32_load(ptr: i32, offset: i32): i32

[vm("i32_store")]
micro i32_store(ptr: i32, offset: i32, value: i32): i32

#endregion

#region i64 加载与存储

[vm("i64_load")]
micro i64_load(ptr: i32, offset: i32): i64

[vm("i64_store")]
micro i64_store(ptr: i32, offset: i32, value: i64): i32

#endregion

#region 批量操作

[vm("mem_copy")]
micro mem_copy(dst: i32, src: i32, len: i32): i32

[vm("mem_set")]
micro mem_set(dst: i32, value: i32, len: i32): i32

#endregion