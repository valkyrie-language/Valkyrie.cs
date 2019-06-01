# std.adaptor.nyar: VM 内置函数 [vm] 绑定
# 将 valkyrie-core 的运行时函数映射为 NyarVM opcode
# 无函数体 = [vm] 声明式绑定，编译器直接映射为对应 opcode

#region 输出

[vm("println")]
micro println_i32(value: i32): i32

[vm("println")]
micro println_utf8(value: utf8): utf8

[vm("print")]
micro print_i32(value: i32): i32

[vm("print")]
micro print_utf8(value: utf8): utf8

#endregion

#region 系统

[vm("exit")]
micro exit(code: i32): i32

[vm("get_time")]
micro get_time(): i64

[vm("sleep_ms")]
micro sleep_ms(ms: i32): i32

#endregion

#region 数学

[vm("math_sin")]
micro math_sin(x: f64): f64

[vm("math_cos")]
micro math_cos(x: f64): f64

[vm("math_sqrt")]
micro math_sqrt(x: f64): f64

[vm("math_rand")]
micro math_rand(): i32

#endregion