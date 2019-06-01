# 线程 API

[jvm("java.lang.Thread", "sleep")]
micro jvm_thread_sleep(milliseconds: i64): void

[jvm("java.lang.Thread", "currentThread")]
micro jvm_thread_current(): i32

[jvm("java.lang.Thread", "getName")]
micro jvm_thread_get_name(thread: i32): string

[jvm("java.lang.Thread", "setPriority")]
micro jvm_thread_set_priority(thread: i32, priority: i32): void

[jvm("java.lang.Thread", "isAlive")]
micro jvm_thread_is_alive(thread: i32): bool
