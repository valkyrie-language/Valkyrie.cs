# 环境变量 API

[wasi("environ_get")]
micro wasi_environ_get(environ_ptr: i32, environ_buf_ptr: i32): i32

[wasi("environ_sizes_get")]
micro wasi_environ_sizes_get(count_ptr: i32, buf_size_ptr: i32): i32
