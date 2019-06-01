# std.adaptor.macos: Darwin / macOS API
# [c] 绑定 macOS 平台 C 约定原生 API（libSystem / Framework 动态链接）
# [syscall] 绑定 Darwin 内核系统调用（Mach 系统调用）
# NSLog 使用 UTF-8 编码，字符串参数标注为 utf8
# 占坑：待实现

#region Foundation

[c("Foundation", "NSLog")]
micro darwin_ns_log(format: utf8): i32

[c("Foundation", "NSStringFromClass")]
micro darwin_ns_string_from_class(cls: i32): i32

#endregion

#region libSystem（POSIX 兼容层）

[c("libSystem", "write")]
micro darwin_write(fd: i32, buf: c_str, len: i32): i32

[c("libSystem", "read")]
micro darwin_read(fd: i32, buf: i32, len: i32): i32

[c("libSystem", "close")]
micro darwin_close(fd: i32): i32

[c("libSystem", "open")]
micro darwin_open(path: c_str, flags: i32, mode: i32): i32

[c("libSystem", "mmap")]
micro darwin_mmap(addr: i32, len: i32, prot: i32, flags: i32, fd: i32, offset: i64): i32

[c("libSystem", "munmap")]
micro darwin_munmap(addr: i32, len: i32): i32

[c("libSystem", "getpid")]
micro darwin_getpid(): i32

[c("libSystem", "fork")]
micro darwin_fork(): i32

[c("libSystem", "exit")]
micro darwin_exit(code: i32)

[c("libSystem", "clock_gettime")]
micro darwin_clock_gettime(clock_id: i32, tp: i32): i32

#endregion

#region Core Foundation

[c("CoreFoundation", "CFStringCreateWithCString")]
micro darwin_cf_string_create(alloc: i32, str: c_str, encoding: i32): i32

[c("CoreFoundation", "CFRelease")]
micro darwin_cf_release(cf: i32)

[c("CoreFoundation", "CFStringGetLength")]
micro darwin_cf_string_length(cf: i32): i32

[c("CoreFoundation", "CFDataCreate")]
micro darwin_cf_data_create(alloc: i32, bytes: i32, length: i32): i32

[c("CoreFoundation", "CFDataGetLength")]
micro darwin_cf_data_length(cf: i32): i32

#endregion

#region Core Graphics

[c("CoreGraphics", "CGMainDisplayID")]
micro darwin_cg_main_display(): i32

[c("CoreGraphics", "CGDisplayPixelsWide")]
micro darwin_cg_display_width(display: i32): i32

[c("CoreGraphics", "CGDisplayPixelsHigh")]
micro darwin_cg_display_height(display: i32): i32

#endregion

#region ApplicationServices

[c("ApplicationServices", "AXIsProcessTrusted")]
micro darwin_ax_is_trusted(): i32

#endregion

#region Objective-C 运行时

[c("objc", "objc_getClass")]
micro darwin_objc_get_class(name: c_str): i32

[c("objc", "sel_registerName")]
micro darwin_sel_register(name: c_str): i32

[c("objc", "objc_msgSend")]
micro darwin_objc_msg_send(obj: i32, sel: i32): i64

[c("objc", "objc_msgSend_fpret")]
micro darwin_objc_msg_send_fpret(obj: i32, sel: i32): f64

#endregion

#region Darwin 系统调用

[syscall(4)]
micro sys_darwin_write(fd: i32, buf: c_str, len: i32): i64

[syscall(3)]
micro sys_darwin_read(fd: i32, buf: i32, len: i32): i64

[syscall(6)]
micro sys_darwin_close(fd: i32): i64

[syscall(5)]
micro sys_darwin_open(path: c_str, flags: i32, mode: i32): i64

[syscall(20)]
micro sys_darwin_getpid(): i64

[syscall(1)]
micro sys_darwin_exit(code: i32)

[syscall(197)]
micro sys_darwin_mmap(addr: i32, len: i32, prot: i32, flags: i32, fd: i32, offset: i64): i64

[syscall(73)]
micro sys_darwin_munmap(addr: i32, len: i32): i64

[syscall(228)]
micro sys_darwin_clock_gettime(clock_id: i32, tp: i32): i64

#endregion

#region Mach 系统调用

[syscall(-26)]
micro sys_darwin_mach_reply_port(): i32

[syscall(-31)]
micro sys_darwin_mach_msg(msg: i32, option: i32, send_size: i32, recv_size: i32, recv_name: i32, timeout: i32, notify: i32): i32

[syscall(-17)]
micro sys_darwin_mach_task_self(): i32

#endregion
