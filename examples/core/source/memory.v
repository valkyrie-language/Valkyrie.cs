# valkyrie-core: 内存操作
# 抽象声明，由各平台适配层提供具体实现
# NyarVM 适配：std.adaptor.nyar/memory.v

#region 分配与释放

micro alloc(size: i32): i32

micro free(ptr: i32): i32

#endregion

#region i32 加载与存储

micro i32_load(ptr: i32, offset: i32): i32

micro i32_store(ptr: i32, offset: i32, value: i32): i32

#endregion

#region i64 加载与存储

micro i64_load(ptr: i32, offset: i32): i64

micro i64_store(ptr: i32, offset: i32, value: i64): i32

#endregion

#region 批量操作

micro mem_copy(dst: i32, src: i32, len: i32): i32

micro mem_set(dst: i32, value: i32, len: i32): i32

#endregion