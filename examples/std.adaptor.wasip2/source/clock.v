# WASI Preview 2 — 时钟与时序
# 对标 wasi:clocks/monotonic-clock + wasi:clocks/wall-clock

[wasi_p2("wasi:clocks/monotonic-clock", "now")]
extern wasi_p2_clock_monotonic_now(): i64

[wasi_p2("wasi:clocks/monotonic-clock", "resolution")]
extern wasi_p2_clock_monotonic_resolution(): i64

[wasi_p2("wasi:clocks/monotonic-clock", "subscribe-duration")]
extern wasi_p2_clock_subscribe_duration(when: i64): i32

[wasi_p2("wasi:clocks/monotonic-clock", "subscribe-instant")]
extern wasi_p2_clock_subscribe_instant(when: i64): i32

[wasi_p2("wasi:clocks/wall-clock", "now")]
extern wasi_p2_clock_wall_now(): (i64, i32)

[wasi_p2("wasi:clocks/wall-clock", "resolution")]
extern wasi_p2_clock_wall_resolution(): (i64, i32)
