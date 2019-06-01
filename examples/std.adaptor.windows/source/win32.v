# std.adaptor.windows: Win32 API
# [c] 绑定 Windows 平台 C 约定原生 API（本质上是 cdecl/stdcall 调用约定）
# [com] 绑定 Windows COM 接口方法（vtable 调用）
# [syscall] 绑定 Windows NT 系统调用（Nt* 系列函数）
# Win32 API 使用 UTF-16 (wchar_t)，所有字符串参数标注为 utf16
# 占坑：待实现

#region 窗口

[c("user32", "MessageBoxW")]
micro win_message_box(title: utf16, text: utf16): i32

[c("user32", "CreateWindowExW")]
micro win_create_window(ex_style: i32, class_name: utf16, window_name: utf16, style: i32, x: i32, y: i32, w: i32, h: i32, parent: i32, menu: i32, instance: i32, param: i32): i32

[c("user32", "ShowWindow")]
micro win_show_window(handle: i32, cmd: i32): i32

[c("user32", "UpdateWindow")]
micro win_update_window(handle: i32): i32

[c("user32", "GetMessageW")]
micro win_get_message(msg: i32, handle: i32, filter_min: i32, filter_max: i32): i32

[c("user32", "TranslateMessage")]
micro win_translate_message(msg: i32): i32

[c("user32", "DispatchMessageW")]
micro win_dispatch_message(msg: i32): i64

[c("user32", "PostQuitMessage")]
micro win_post_quit_message(exit_code: i32)

#endregion

#region 控制台

[c("kernel32", "GetStdHandle")]
micro win_get_std_handle(nstd: i32): i32

[c("kernel32", "WriteConsoleW")]
micro win_write_console(handle: i32, buf: utf16, len: i32): i32

[c("kernel32", "ReadConsoleW")]
micro win_read_console(handle: i32, buf: i32, len: i32): i32

[c("kernel32", "AllocConsole")]
micro win_alloc_console(): i32

[c("kernel32", "FreeConsole")]
micro win_free_console(): i32

#endregion

#region 内存

[c("kernel32", "VirtualAlloc")]
micro win_virtual_alloc(addr: i32, size: i32, alloc_type: i32, protect: i32): i32

[c("kernel32", "VirtualFree")]
micro win_virtual_free(addr: i32, size: i32, free_type: i32): i32

[c("kernel32", "VirtualProtect")]
micro win_virtual_protect(addr: i32, size: i32, new_protect: i32, old_protect: i32): i32

#endregion

#region 进程与线程

[c("kernel32", "GetCurrentProcessId")]
micro win_get_pid(): i32

[c("kernel32", "GetCurrentThreadId")]
micro win_get_tid(): i32

[c("kernel32", "Sleep")]
micro win_sleep(ms: i32)

[c("kernel32", "GetTickCount")]
micro win_get_tick_count(): i32

#endregion

#region COM 接口

[com("IUnknown", "QueryInterface")]
micro com_query_interface(this: i32, riid: i32, out: i32): i32

[com("IUnknown", "AddRef")]
micro com_add_ref(this: i32): i32

[com("IUnknown", "Release")]
micro com_release(this: i32): i32

[com("IDispatch", "GetTypeInfoCount")]
micro com_get_type_info_count(this: i32, count: i32): i32

[com("IDispatch", "GetTypeInfo")]
micro com_get_type_info(this: i32, index: i32, lcid: i32, out: i32): i32

[com("IDispatch", "GetIDsOfNames")]
micro com_get_ids_of_names(this: i32, riid: i32, names: i32, count: i32, lcid: i32, ids: i32): i32

[com("IDispatch", "Invoke")]
micro com_invoke(this: i32, id: i32, riid: i32, lcid: i32, flags: i16, params: i32, result: i32, excep: i32, arg_err: i32): i32

#endregion

#region COM 初始化

[c("ole32", "CoInitializeEx")]
micro co_initialize(reserved: i32, apartment: i32): i32

[c("ole32", "CoUninitialize")]
micro co_uninitialize()

[c("ole32", "CoCreateInstance")]
micro co_create_instance(clsid: i32, outer: i32, cls_ctx: i32, iid: i32, out: i32): i32

#endregion

#region NT 系统调用

[syscall("NtCreateFile")]
micro nt_create_file(handle: i32, access: i32, obj_attr: i32, io_status: i32, alloc_size: i32, file_attr: i32, share_access: i32, disposition: i32, options: i32, ea: i32, ea_len: i32): i32

[syscall("NtClose")]
micro nt_close(handle: i32): i32

[syscall("NtReadFile")]
micro nt_read_file(handle: i32, event: i32, apc: i32, apc_ctx: i32, io_status: i32, buf: i32, len: i32, offset: i32, key: i32): i32

[syscall("NtWriteFile")]
micro nt_write_file(handle: i32, event: i32, apc: i32, apc_ctx: i32, io_status: i32, buf: i32, len: i32, offset: i32, key: i32): i32

#endregion
