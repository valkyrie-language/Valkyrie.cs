# valkyrie-core: 运行时内置函数
# 抽象声明，由各平台适配层提供具体实现
# NyarVM 适配：std.adaptor.nyar/builtin.v

#region 输出

micro println_i32(value: i32): i32

micro println_utf8(value: utf8): utf8

micro print_i32(value: i32): i32

micro print_utf8(value: utf8): utf8

#endregion

#region 系统

micro exit(code: i32): i32

micro get_time(): i64

micro sleep_ms(ms: i32): i32

#endregion

#region 数学

micro math_sin(x: f64): f64

micro math_cos(x: f64): f64

micro math_sqrt(x: f64): f64

micro math_rand(): i32

#endregion