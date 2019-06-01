# std.adaptor.nyar: 对象与闭包操作 [vm] 绑定
# 将 valkyrie-core 的对象操作映射为 NyarVM opcode

#region 对象字段

[vm("obj_len")]
micro object_len(obj: i32): i32

[vm("obj_get_field")]
micro object_get_field(obj: i32, index: i32): i32

[vm("obj_set_field")]
micro object_set_field(obj: i32, index: i32, value: i32): i32

#endregion

#region 对象索引

[vm("obj_get_index")]
micro object_get_index(obj: i32, index: i32): i32

[vm("obj_set_index")]
micro object_set_index(obj: i32, index: i32, value: i32): i32

#endregion

#region 闭包 upvalue

[vm("closure_get_upvalue")]
micro closure_get_upvalue(closure: i32, index: i32): i32

[vm("closure_set_upvalue")]
micro closure_set_upvalue(closure: i32, index: i32, value: i32): i32

#endregion