# std.adaptor.nyar: 比较运算 [vm] 绑定
# 将 valkyrie-core 的比较运算映射为 NyarVM opcode
# bool 逻辑保留 Valkyrie 短路求值实现，不在此处绑定

#region i32 比较

[vm("i32_eq")]
micro i32_eq(a: i32, b: i32): bool

[vm("i32_ne")]
micro i32_ne(a: i32, b: i32): bool

[vm("i32_lt")]
micro i32_lt(a: i32, b: i32): bool

[vm("i32_le")]
micro i32_le(a: i32, b: i32): bool

[vm("i32_gt")]
micro i32_gt(a: i32, b: i32): bool

[vm("i32_ge")]
micro i32_ge(a: i32, b: i32): bool

#endregion