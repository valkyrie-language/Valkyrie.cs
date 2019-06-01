# VOA JS Builtin — Math
# [js_builtin] 直接映射 JS 内置 Math API，无依赖
# Math 函数全部是纯函数，标注 pure，可被 DCE 消除无用调用

# 常量

[js_builtin("Math.E"), pure]
micro math_e(): f64

[js_builtin("Math.LN2"), pure]
micro math_ln2(): f64

[js_builtin("Math.LN10"), pure]
micro math_ln10(): f64

[js_builtin("Math.PI"), pure]
micro math_pi(): f64

[js_builtin("Math.SQRT2"), pure]
micro math_sqrt2(): f64

# 取整

[js_builtin("Math.floor"), pure]
micro math_floor(x: f64): i32

[js_builtin("Math.ceil"), pure]
micro math_ceil(x: f64): i32

[js_builtin("Math.round"), pure]
micro math_round(x: f64): i32

[js_builtin("Math.trunc"), pure]
micro math_trunc(x: f64): i32

# 绝对值与符号

[js_builtin("Math.abs"), pure]
micro math_abs(x: f64): f64

[js_builtin("Math.sign"), pure]
micro math_sign(x: f64): i32

# 极值

[js_builtin("Math.min"), pure]
micro math_min(a: f64, b: f64): f64

[js_builtin("Math.max"), pure]
micro math_max(a: f64, b: f64): f64

# 幂与根

[js_builtin("Math.pow"), pure]
micro math_pow(base: f64, exp: f64): f64

[js_builtin("Math.sqrt"), pure]
micro math_sqrt(x: f64): f64

[js_builtin("Math.cbrt"), pure]
micro math_cbrt(x: f64): f64

[js_builtin("Math.hypot"), pure]
micro math_hypot(a: f64, b: f64): f64

# 对数与指数

[js_builtin("Math.log"), pure]
micro math_log(x: f64): f64

[js_builtin("Math.log2"), pure]
micro math_log2(x: f64): f64

[js_builtin("Math.log10"), pure]
micro math_log10(x: f64): f64

[js_builtin("Math.exp"), pure]
micro math_exp(x: f64): f64

[js_builtin("Math.expm1"), pure]
micro math_expm1(x: f64): f64

# 三角函数

[js_builtin("Math.sin"), pure]
micro math_sin(x: f64): f64

[js_builtin("Math.cos"), pure]
micro math_cos(x: f64): f64

[js_builtin("Math.tan"), pure]
micro math_tan(x: f64): f64

[js_builtin("Math.asin"), pure]
micro math_asin(x: f64): f64

[js_builtin("Math.acos"), pure]
micro math_acos(x: f64): f64

[js_builtin("Math.atan"), pure]
micro math_atan(x: f64): f64

[js_builtin("Math.atan2"), pure]
micro math_atan2(y: f64, x: f64): f64

# 随机

[js_builtin("Math.random")]
micro math_random(): f64

# 夹值

[js_builtin("Math.clz32"), pure]
micro math_clz32(x: i32): i32

[js_builtin("Math.imul"), pure]
micro math_imul(a: i32, b: i32): i32

[js_builtin("Math.fround"), pure]
micro math_fround(x: f64): f64
