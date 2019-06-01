# std.adaptor.linux: POSIX / Linux API
# [c] 绑定 Linux 平台 C 约定原生 API（libc 动态链接）
# [syscall] 绑定 Linux 内核系统调用（绕过 libc，直接 syscall 指令）
# POSIX API 使用字节缓冲区，所有字符串参数标注为 c_str
# 占坑：待实现

#region 文件描述符

[c("libc", "write")]
micro posix_write(fd: i32, buf: c_str, len: i32): i32

[c("libc", "read")]
micro posix_read(fd: i32, buf: c_str, len: i32): i32

[c("libc", "close")]
micro posix_close(fd: i32): i32

[c("libc", "open")]
micro posix_open(path: c_str, flags: i32, mode: i32): i32

[c("libc", "lseek")]
micro posix_lseek(fd: i32, offset: i64, whence: i32): i64

[c("libc", "fcntl")]
micro posix_fcntl(fd: i32, cmd: i32, arg: i32): i32

[c("libc", "ioctl")]
micro posix_ioctl(fd: i32, request: i64, arg: i32): i32

#endregion

#region 文件系统

[c("libc", "stat")]
micro posix_stat(path: c_str, buf: i32): i32

[c("libc", "fstat")]
micro posix_fstat(fd: i32, buf: i32): i32

[c("libc", "mkdir")]
micro posix_mkdir(path: c_str, mode: i32): i32

[c("libc", "rmdir")]
micro posix_rmdir(path: c_str): i32

[c("libc", "unlink")]
micro posix_unlink(path: c_str): i32

[c("libc", "rename")]
micro posix_rename(old: c_str, new_: c_str): i32

[c("libc", "chmod")]
micro posix_chmod(path: c_str, mode: i32): i32

[c("libc", "chown")]
micro posix_chown(path: c_str, uid: i32, gid: i32): i32

#endregion

#region 内存

[c("libc", "malloc")]
micro posix_malloc(size: i32): i32

[c("libc", "free")]
micro posix_free(ptr: i32)

[c("libc", "realloc")]
micro posix_realloc(ptr: i32, size: i32): i32

[c("libc", "mmap")]
micro posix_mmap(addr: i32, len: i32, prot: i32, flags: i32, fd: i32, offset: i64): i32

[c("libc", "munmap")]
micro posix_munmap(addr: i32, len: i32): i32

[c("libc", "mprotect")]
micro posix_mprotect(addr: i32, len: i32, prot: i32): i32

#endregion

#region 进程

[c("libc", "getpid")]
micro posix_getpid(): i32

[c("libc", "getppid")]
micro posix_getppid(): i32

[c("libc", "getuid")]
micro posix_getuid(): i32

[c("libc", "getgid")]
micro posix_getgid(): i32

[c("libc", "fork")]
micro posix_fork(): i32

[c("libc", "execve")]
micro posix_execve(path: c_str, argv: i32, envp: i32): i32

[c("libc", "waitpid")]
micro posix_waitpid(pid: i32, status: i32, options: i32): i32

[c("libc", "exit")]
micro posix_exit(code: i32)

#endregion

#region 信号

[c("libc", "kill")]
micro posix_kill(pid: i32, sig: i32): i32

[c("libc", "signal")]
micro posix_signal(sig: i32, handler: i32): i32

#endregion

#region 时间

[c("libc", "clock_gettime")]
micro posix_clock_gettime(clock_id: i32, tp: i32): i32

[c("libc", "nanosleep")]
micro posix_nanosleep(req: i32, rem: i32): i32

[c("libc", "gettimeofday")]
micro posix_gettimeofday(tv: i32, tz: i32): i32

#endregion

#region 线程

[c("libpthread", "pthread_create")]
micro posix_pthread_create(thread: i32, attr: i32, start: i32, arg: i32): i32

[c("libpthread", "pthread_join")]
micro posix_pthread_join(thread: i32, retval: i32): i32

[c("libpthread", "pthread_mutex_lock")]
micro posix_mutex_lock(mutex: i32): i32

[c("libpthread", "pthread_mutex_unlock")]
micro posix_mutex_unlock(mutex: i32): i32

#endregion

#region 网络

[c("libc", "socket")]
micro posix_socket(domain: i32, type_: i32, protocol: i32): i32

[c("libc", "bind")]
micro posix_bind(fd: i32, addr: i32, len: i32): i32

[c("libc", "listen")]
micro posix_listen(fd: i32, backlog: i32): i32

[c("libc", "accept")]
micro posix_accept(fd: i32, addr: i32, len: i32): i32

[c("libc", "connect")]
micro posix_connect(fd: i32, addr: i32, len: i32): i32

#endregion

#region 系统调用（绕过 libc）

[syscall(1)]
micro sys_write(fd: i32, buf: c_str, len: i32): i64

[syscall(0)]
micro sys_read(fd: i32, buf: i32, len: i32): i64

[syscall(3)]
micro sys_close(fd: i32): i64

[syscall(2)]
micro sys_open(path: c_str, flags: i32, mode: i32): i64

[syscall(39)]
micro sys_getpid(): i64

[syscall(57)]
micro sys_fork(): i64

[syscall(59)]
micro sys_execve(path: c_str, argv: i32, envp: i32): i64

[syscall(60)]
micro sys_exit(code: i32)

[syscall(9)]
micro sys_mmap(addr: i32, len: i32, prot: i32, flags: i32, fd: i32, offset: i64): i64

[syscall(11)]
micro sys_munmap(addr: i32, len: i32): i64

[syscall(228)]
micro sys_clock_gettime(clock_id: i32, tp: i32): i64

[syscall(35)]
micro sys_nanosleep(req: i32, rem: i32): i64

[syscall(41)]
micro sys_socket(domain: i32, type_: i32, protocol: i32): i64

[syscall(42)]
micro sys_connect(fd: i32, addr: i32, len: i32): i64

[syscall(43)]
micro sys_accept(fd: i32, addr: i32, len: i32): i64

[syscall(44)]
micro sys_sendto(fd: i32, buf: i32, len: i32, flags: i32, addr: i32, addr_len: i32): i64

[syscall(45)]
micro sys_recvfrom(fd: i32, buf: i32, len: i32, flags: i32, addr: i32, addr_len: i32): i64

#endregion
