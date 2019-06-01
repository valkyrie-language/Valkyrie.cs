# 数学 API

[jvm("java.lang.Math", "abs"), pure]
micro jvm_math_abs_i32(value: i32): i32

[jvm("java.lang.Math", "abs"), pure]
micro jvm_math_abs_f64(value: f64): f64

[jvm("java.lang.Math", "max"), pure]
micro jvm_math_max_i32(a: i32, b: i32): i32

[jvm("java.lang.Math", "min"), pure]
micro jvm_math_min_i32(a: i32, b: i32): i32

[jvm("java.lang.Math", "sqrt"), pure]
micro jvm_math_sqrt(value: f64): f64

[jvm("java.lang.Math", "pow"), pure]
micro jvm_math_pow(x: f64, y: f64): f64

[jvm("java.lang.Math", "floor"), pure]
micro jvm_math_floor(value: f64): f64

[jvm("java.lang.Math", "ceil"), pure]
micro jvm_math_ceil(value: f64): f64

[jvm("java.lang.Math", "round"), pure]
micro jvm_math_round(value: f64): i64

[jvm("java.lang.Math", "sin"), pure]
micro jvm_math_sin(value: f64): f64

[jvm("java.lang.Math", "cos"), pure]
micro jvm_math_cos(value: f64): f64

[jvm("java.lang.Math", "random"), pure]
micro jvm_math_random(): f64
