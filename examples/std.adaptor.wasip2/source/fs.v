# WASI Preview 2 — 文件系统
# 对标 wasi:filesystem/types + wasi:filesystem/preopens + wasi:filesystem/filesystem

[wasi_p2("wasi:filesystem/types", "filesystem-error-code")]
extern wasi_p2_fs_error_code(): i32

[wasi_p2("wasi:filesystem/preopens", "get-directories")]
extern wasi_p2_preopens(): [i32]

[wasi_p2("wasi:filesystem/filesystem", "open-at")]
extern wasi_p2_fs_open_at(dir: i32, path: utf8, flags: i32, mode: i32): (i32, i32)

[wasi_p2("wasi:filesystem/filesystem", "create-file-at")]
extern wasi_p2_fs_create_file_at(dir: i32, path: utf8, flags: i32): (i32, i32)

[wasi_p2("wasi:filesystem/filesystem", "create-directory-at")]
extern wasi_p2_fs_create_dir_at(dir: i32, path: utf8): i32

[wasi_p2("wasi:filesystem/filesystem", "remove-file-at")]
extern wasi_p2_fs_remove_file_at(dir: i32, path: utf8): i32

[wasi_p2("wasi:filesystem/filesystem", "remove-directory-at")]
extern wasi_p2_fs_remove_dir_at(dir: i32, path: utf8): i32

[wasi_p2("wasi:filesystem/filesystem", "rename-at")]
extern wasi_p2_fs_rename_at(old_dir: i32, old_path: utf8, new_dir: i32, new_path: utf8): i32

[wasi_p2("wasi:filesystem/filesystem", "symlink-at")]
extern wasi_p2_fs_symlink_at(target: utf8, dir: i32, path: utf8): i32

[wasi_p2("wasi:filesystem/filesystem", "get-stat-at")]
extern wasi_p2_fs_get_stat_at(dir: i32, path: utf8): (Metadata, i32)

[wasi_p2("wasi:filesystem/filesystem", "set-times-at")]
extern wasi_p2_fs_set_times_at(dir: i32, path: utf8, atime: i64, mtime: i64): i32

[wasi_p2("wasi:filesystem/filesystem", "readlink-at")]
extern wasi_p2_fs_readlink_at(dir: i32, path: utf8): (utf8, i32)

[wasi_p2("wasi:filesystem/filesystem", "readdir-at")]
extern wasi_p2_fs_readdir_at(dir: i32, path: utf8): ([DirEntry], i32)

[wasi_p2("wasi:filesystem/filesystem", "get-fd-flags")]
extern wasi_p2_fs_get_fd_flags(fd: i32): (i32, i32)

[wasi_p2("wasi:filesystem/filesystem", "set-fd-flags")]
extern wasi_p2_fs_set_fd_flags(fd: i32, flags: i32): i32

[wasi_p2("wasi:filesystem/filesystem", "close-fd")]
extern wasi_p2_fs_close_fd(fd: i32): void

[wasi_p2("wasi:filesystem/filesystem", "read-via-stream")]
extern wasi_p2_fs_read_via_stream(fd: i32, offset: i64): (i32, i32)

[wasi_p2("wasi:filesystem/filesystem", "write-via-stream")]
extern wasi_p2_fs_write_via_stream(fd: i32, offset: i64): (i32, i32)

[wasi_p2("wasi:filesystem/filesystem", "truncate-via-stream")]
extern wasi_p2_fs_truncate_via_stream(fd: i32): (i32, i32)

[wasi_p2("wasi:filesystem/filesystem", "get-fd-status")]
extern wasi_p2_fs_get_fd_status(fd: i32): (Metadata, i32)

[wasi_p2("wasi:filesystem/filesystem", "stat-fd")]
extern wasi_p2_fs_stat_fd(fd: i32): (Metadata, i32)

[wasi_p2("wasi:filesystem/filesystem", "set-times-fd")]
extern wasi_p2_fs_set_times_fd(fd: i32, atime: i64, mtime: i64): i32
