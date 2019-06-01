# WASI Preview 2 — 随机数
# 对标 wasi:random/random

[wasi_p2("wasi:random/random", "get-random-bytes")]
extern wasi_p2_random_get_bytes(len: i64): [u8]

[wasi_p2("wasi:random/random", "get-random-u64")]
extern wasi_p2_random_get_u64(): i64

[wasi_p2("wasi:random/insecure", "get-insecure-random-bytes")]
extern wasi_p2_random_get_insecure_bytes(len: i64): [u8]

[wasi_p2("wasi:random/insecure", "get-insecure-random-u64")]
extern wasi_p2_random_get_insecure_u64(): i64
