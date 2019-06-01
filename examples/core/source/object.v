# valkyrie-core: 对象与闭包操作
# 抽象声明，由各平台适配层提供具体实现
# NyarVM 适配：std.adaptor.nyar/object.v

#region 对象字段

micro object_len(obj: i32): i32

micro object_get_field(obj: i32, index: i32): i32

micro object_set_field(obj: i32, index: i32, value: i32): i32

#endregion

#region 对象索引

micro object_get_index(obj: i32, index: i32): i32

micro object_set_index(obj: i32, index: i32, value: i32): i32

#endregion

#region 闭包 upvalue

micro closure_get_upvalue(closure: i32, index: i32): i32

micro closure_set_upvalue(closure: i32, index: i32, value: i32): i32

#endregion