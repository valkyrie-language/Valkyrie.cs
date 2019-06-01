# I/O API — 文件描述符读写关闭

[wasi("fd_write")]
micro wasi_fd_write(fd: i32, iovs_ptr: i32, iovs_len: i32, nwritten_ptr: i32): i32

[wasi("fd_read")]
micro wasi_fd_read(fd: i32, iovs_ptr: i32, iovs_len: i32, nread_ptr: i32): i32

[wasi("fd_close")]
micro wasi_fd_close(fd: i32): i32

[wasi("fd_seek")]
micro wasi_fd_seek(fd: i32, offset: i64, whence: i32, newoffset_ptr: i32): i32

[wasi("fd_tell")]
micro wasi_fd_tell(fd: i32, offset_ptr: i32): i32

[wasi("fd_advise")]
micro wasi_fd_advise(fd: i32, offset: i64, len: i64, advice: i32): i32

[wasi("fd_allocate")]
micro wasi_fd_allocate(fd: i32, offset: i64, len: i64): i32

[wasi("fd_filestat_get")]
micro wasi_fd_filestat_get(fd: i32, buf_ptr: i32): i32

[wasi("fd_filestat_set_size")]
micro wasi_fd_filestat_set_size(fd: i32, size: i64): i32

[wasi("fd_filestat_set_times")]
micro wasi_fd_filestat_set_times(fd: i32, atim: i64, mtim: i64, fstflags: i32): i32

[wasi("fd_prestat_dir_name")]
micro wasi_fd_prestat_dir_name(fd: i32, path_ptr: i32, path_len: i32): i32

[wasi("fd_prestat")]
micro wasi_fd_prestat(fd: i32, prestat_ptr: i32): i32
