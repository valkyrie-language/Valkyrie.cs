# 命令行参数 API

[wasi("args_get")]
micro wasi_args_get(argv_ptr: i32, argv_buf_ptr: i32): i32

[wasi("args_sizes_get")]
micro wasi_args_sizes_get(count_ptr: i32, buf_size_ptr: i32): i32
