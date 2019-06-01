# 文件系统 API

[wasi("path_open")]
micro wasi_path_open(fd: i32, dirflags: i32, path_ptr: i32, path_len: i32, oflags: i32, fs_rights_base: i64, fs_rights_inheriting: i64, fdflags: i32, opened_fd_ptr: i32): i32

[wasi("path_rename")]
micro wasi_path_rename(fd: i32, old_path_ptr: i32, old_path_len: i32, new_fd: i32, new_path_ptr: i32, new_path_len: i32): i32

[wasi("path_remove_directory")]
micro wasi_path_remove_directory(fd: i32, path_ptr: i32, path_len: i32): i32

[wasi("path_filestat_get")]
micro wasi_path_filestat_get(fd: i32, flags: i32, path_ptr: i32, path_len: i32, buf_ptr: i32): i32

[wasi("path_filestat_set_times")]
micro wasi_path_filestat_set_times(fd: i32, flags: i32, path_ptr: i32, path_len: i32, atim: i64, mtim: i64, fstflags: i32): i32

[wasi("path_create_directory")]
micro wasi_path_create_directory(fd: i32, path_ptr: i32, path_len: i32): i32

[wasi("path_link")]
micro wasi_path_link(old_fd: i32, old_flags: i32, old_path_ptr: i32, old_path_len: i32, new_fd: i32, new_path_ptr: i32, new_path_len: i32): i32

[wasi("path_readlink")]
micro wasi_path_readlink(fd: i32, path_ptr: i32, path_len: i32, buf_ptr: i32, buf_len: i32, nread_ptr: i32): i32

[wasi("path_symlink")]
micro wasi_path_symlink(old_path_ptr: i32, old_path_len: i32, fd: i32, new_path_ptr: i32, new_path_len: i32): i32

[wasi("path_unlink_file")]
micro wasi_path_unlink_file(fd: i32, path_ptr: i32, path_len: i32): i32
