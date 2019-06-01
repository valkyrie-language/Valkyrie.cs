# WASI Preview 2 — 网络套接字
# 对标 wasi:sockets/tcp + wasi:sockets/udp + wasi:sockets/ip-name-lookup + wasi:sockets/network

[wasi_p2("wasi:sockets/instance-network", "instance-network")]
extern wasi_p2_net_instance_network(): i32

[wasi_p2("wasi:sockets/ip-name-lookup", "resolve-addresses")]
extern wasi_p2_net_resolve_addresses(network: i32, name: utf8): ([(utf8, i32)], i32)

[wasi_p2("wasi:sockets/tcp-create-socket", "create-tcp-socket")]
extern wasi_p2_tcp_create_socket(address_family: i32): (i32, i32)

[wasi_p2("wasi:sockets/tcp", "bind")]
extern wasi_p2_tcp_bind(socket: i32, network: i32, local_address: utf8, local_port: i32): i32

[wasi_p2("wasi:sockets/tcp", "listen")]
extern wasi_p2_tcp_listen(socket: i32, backlog: i32): i32

[wasi_p2("wasi:sockets/tcp", "accept")]
extern wasi_p2_tcp_accept(socket: i32): (i32, (utf8, i32), i32)

[wasi_p2("wasi:sockets/tcp", "connect")]
extern wasi_p2_tcp_connect(socket: i32, network: i32, remote_address: utf8, remote_port: i32): (i32, i32, (utf8, i32), i32)

[wasi_p2("wasi:sockets/tcp", "send")]
extern wasi_p2_tcp_send(socket: i32, data: [u8]): (i64, i32)

[wasi_p2("wasi:sockets/tcp", "receive")]
extern wasi_p2_tcp_receive(socket: i32, max_len: i64): ([u8], bool, i32)

[wasi_p2("wasi:sockets/tcp", "shutdown")]
extern wasi_p2_tcp_shutdown(socket: i32, shutdown_type: i32): i32

[wasi_p2("wasi:sockets/tcp", "close")]
extern wasi_p2_tcp_close(socket: i32): void

[wasi_p2("wasi:sockets/tcp", "subscribe")]
extern wasi_p2_tcp_subscribe(socket: i32): i32

[wasi_p2("wasi:sockets/tcp", "set-listen-backlog-size")]
extern wasi_p2_tcp_set_listen_backlog_size(socket: i32, backlog: i64): i32

[wasi_p2("wasi:sockets/tcp", "set-keep-alive")]
extern wasi_p2_tcp_set_keep_alive(socket: i32, keep_alive_ms: i64): i32

[wasi_p2("wasi:sockets/tcp", "set-no-delay")]
extern wasi_p2_tcp_set_no_delay(socket: i32, enabled: bool): i32

[wasi_p2("wasi:sockets/tcp", "set-unicast-hop-limit")]
extern wasi_p2_tcp_set_unicast_hop_limit(socket: i32, value: i32): i32

[wasi_p2("wasi:sockets/tcp", "get-receive-buffer-size")]
extern wasi_p2_tcp_get_receive_buffer_size(socket: i32): (i64, i32)

[wasi_p2("wasi:sockets/tcp", "set-receive-buffer-size")]
extern wasi_p2_tcp_set_receive_buffer_size(socket: i32, value: i64): i32

[wasi_p2("wasi:sockets/tcp", "get-send-buffer-size")]
extern wasi_p2_tcp_get_send_buffer_size(socket: i32): (i64, i32)

[wasi_p2("wasi:sockets/tcp", "set-send-buffer-size")]
extern wasi_p2_tcp_set_send_buffer_size(socket: i32, value: i64): i32

[wasi_p2("wasi:sockets/udp-create-socket", "create-udp-socket")]
extern wasi_p2_udp_create_socket(address_family: i32): (i32, i32)

[wasi_p2("wasi:sockets/udp", "bind")]
extern wasi_p2_udp_bind(socket: i32, network: i32, local_address: utf8, local_port: i32): i32

[wasi_p2("wasi:sockets/udp", "send")]
extern wasi_p2_udp_send(socket: i32, data: [u8], remote_address: utf8, remote_port: i32): (i64, i32)

[wasi_p2("wasi:sockets/udp", "receive")]
extern wasi_p2_udp_receive(socket: i32, max_len: i64): ([u8], (utf8, i32), i32)

[wasi_p2("wasi:sockets/udp", "close")]
extern wasi_p2_udp_close(socket: i32): void

[wasi_p2("wasi:sockets/udp", "subscribe")]
extern wasi_p2_udp_subscribe(socket: i32): i32
