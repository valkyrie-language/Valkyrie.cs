# 数学 API

[clr("System.Math", "Abs"), pure]
micro math_abs_i32(value: i32): i32

[clr("System.Math", "Abs"), pure]
micro math_abs_f64(value: f64): f64

[clr("System.Math", "Max"), pure]
micro math_max_i32(a: i32, b: i32): i32

[clr("System.Math", "Min"), pure]
micro math_min_i32(a: i32, b: i32): i32

[clr("System.Math", "Sqrt"), pure]
micro math_sqrt(value: f64): f64

[clr("System.Math", "Pow"), pure]
micro math_pow(x: f64, y: f64): f64

[clr("System.Math", "Floor"), pure]
micro math_floor(value: f64): f64

[clr("System.Math", "Ceiling"), pure]
micro math_ceiling(value: f64): f64

[clr("System.Math", "Round"), pure]
micro math_round(value: f64): f64

[clr("System.Math", "Sin"), pure]
micro math_sin(value: f64): f64

[clr("System.Math", "Cos"), pure]
micro math_cos(value: f64): f64

[clr("System.Math", "Log"), pure]
micro math_log(value: f64): f64
