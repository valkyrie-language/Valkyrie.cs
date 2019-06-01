# 时钟 API

[wasi("clock_time_get")]
micro wasi_clock_time_get(clock_id: i32, precision: i64, time_ptr: i32): i32

[wasi("clock_res_get")]
micro wasi_clock_res_get(clock_id: i32, resolution_ptr: i32): i32
