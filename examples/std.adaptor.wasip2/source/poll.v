# WASI Preview 2 — 异步轮询
# 对标 wasi:io/poll（基于 p2 资源模型的异步 I/O）

[wasi_p2("wasi:io/poll", "poll")]
extern wasi_p2_poll(in: [i32]): ([i32, bool, i32], i32)

[wasi_p2("wasi:io/poll", "poll-oneoff")]
extern wasi_p2_poll_oneoff(subscriptions: i32, count: i32): (i32, i32)

[wasi_p2("wasi:io/poll", "drop-pollable")]
extern wasi_p2_pollable_drop(pollable: i32): void

[wasi_p2("wasi:io/poll", "ready")]
extern wasi_p2_pollable_ready(pollable: i32): bool

[wasi_p2("wasi:io/poll", "block")]
extern wasi_p2_pollable_block(pollable: i32): void
