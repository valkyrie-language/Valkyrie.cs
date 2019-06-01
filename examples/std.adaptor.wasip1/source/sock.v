# 网络套接字 API

[wasi("sock_connect")]
micro wasi_sock_connect(fd: i32, addr_ptr: i32, addr_len: i32): i32

[wasi("sock_send")]
micro wasi_sock_send(fd: i32, iovs_ptr: i32, iovs_len: i32, si_flags: i32, nsent_ptr: i32): i32

[wasi("sock_recv")]
micro wasi_sock_recv(fd: i32, iovs_ptr: i32, iovs_len: i32, ri_flags: i32, nread_ptr: i32, ro_flags_ptr: i32): i32

[wasi("sock_shutdown")]
micro wasi_sock_shutdown(fd: i32, how: i32): i32
