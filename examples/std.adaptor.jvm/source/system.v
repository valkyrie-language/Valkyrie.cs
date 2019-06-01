# std.adaptor.jvm: java.lang.System
# [jvm] 绑定 JVM 类静态方法/字段，编译为 invokestatic/getstatic
# System IO 操作有副作用，不标注 pure
# Java String 内部为 UTF-16，所有字符串参数标注为 utf16

#region 标准流

[jvm("java/lang/System", "out")]
micro jvm_system_out(): i32

[jvm("java/lang/System", "err")]
micro jvm_system_err(): i32

[jvm("java/lang/System", "in")]
micro jvm_system_in(): i32

#endregion

#region 系统属性

[jvm("java/lang/System", "getProperty")]
micro jvm_get_property(key: utf16): utf16

[jvm("java/lang/System", "setProperty")]
micro jvm_set_property(key: utf16, value: utf16): utf16

[jvm("java/lang/System", "getenv")]
micro jvm_get_env(name: utf16): utf16

#endregion

#region 时间

[jvm("java/lang/System", "currentTimeMillis")]
micro jvm_current_time_millis(): i64

[jvm("java/lang/System", "nanoTime")]
micro jvm_nano_time(): i64

#endregion

#region 退出

[jvm("java/lang/System", "exit")]
micro jvm_exit(code: i32)

#endregion

#region GC

[jvm("java/lang/System", "gc")]
micro jvm_gc()

#endregion

#region 数组拷贝

[jvm("java/lang/System", "arraycopy")]
micro jvm_arraycopy(src: i32, src_pos: i32, dst: i32, dst_pos: i32, length: i32)

#endregion

#region 标识哈希

[jvm("java/lang/System", "identityHashCode"), pure]
micro jvm_identity_hash(obj: i32): i32

#endregion
