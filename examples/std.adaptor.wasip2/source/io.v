# WASI Preview 2 — I/O 流
# 对标 wasi:io/streams 接口
# p2 使用资源句柄 (resource handle)，返回 result<T, error>

[wasi_p2("wasi:io/streams", "read")]
extern wasi_p2_stream_read(stream: i32, len: i64): (i64, i32)

[wasi_p2("wasi:io/streams", "blocking-read")]
extern wasi_p2_stream_blocking_read(stream: i32, len: i64): (i64, i32)

[wasi_p2("wasi:io/streams", "skip")]
extern wasi_p2_stream_skip(stream: i32, len: i64): (i64, i32)

[wasi_p2("wasi:io/streams", "blocking-skip")]
extern wasi_p2_stream_blocking_skip(stream: i32, len: i64): (i64, i32)

[wasi_p2("wasi:io/streams", "subscribe-to-input-stream")]
extern wasi_p2_input_stream_subscribe(stream: i32): i32

[wasi_p2("wasi:io/streams", "write")]
extern wasi_p2_output_stream_write(stream: i32, contents: i64): (i32, i32)

[wasi_p2("wasi:io/streams", "blocking-write")]
extern wasi_p2_output_stream_blocking_write(stream: i32, contents: i64): (i32, i32)

[wasi_p2("wasi:io/streams", "write-zeroes")]
extern wasi_p2_output_stream_write_zeroes(stream: i32, len: i64): (i32, i32)

[wasi_p2("wasi:io/streams", "blocking-write-zeroes")]
extern wasi_p2_output_stream_blocking_write_zeroes(stream: i32, len: i64): (i32, i32)

[wasi_p2("wasi:io/streams", "splice")]
extern wasi_p2_output_stream_splice(src: i32, len: i64): (i64, i32)

[wasi_p2("wasi:io/streams", "blocking-splice")]
extern wasi_p2_output_stream_blocking_splice(src: i32, len: i64): (i64, i32)

[wasi_p2("wasi:io/streams", "forward")]
extern wasi_p2_output_stream_forward(src: i32): (i64, i32)

[wasi_p2("wasi:io/streams", "subscribe-to-output-stream")]
extern wasi_p2_output_stream_subscribe(stream: i32): i32

[wasi_p2("wasi:io/streams", "drop-input-stream")]
extern wasi_p2_input_stream_drop(stream: i32): void

[wasi_p2("wasi:io/streams", "drop-output-stream")]
extern wasi_p2_output_stream_drop(stream: i32): void

[wasi_p2("wasi:io/streams", "write-via-stream")]
extern wasi_p2_write_via_stream(input_stream: i32, output_stream: i32): (i64, i32)
