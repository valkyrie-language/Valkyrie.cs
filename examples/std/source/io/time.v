# std.io: time — 时间与计时
# 编译时根据 arch 委托 adaptor 实现

micro now(): i64 {
    <% match arch %>
        <% case "clr" %>
        return 0
        <% case "jvm" %>
        return std.adaptor.jvm.system.jvm_current_time_millis()
        <% case "wasm32" %>
        return i64(std.adaptor.wasm.performance.perf_now())
        <% case "wasip2" %>
        return std.adaptor.wasip2.clock.wasi_p2_clock_monotonic_now()
        <% else %>
        return 0
    <% end match %>
}

micro monotonic(): i64 {
    <% match arch %>
        <% case "wasm32" %>
        return i64(std.adaptor.wasm.performance.perf_now())
        <% case "wasip2" %>
        return std.adaptor.wasip2.clock.wasi_p2_clock_monotonic_now()
        <% else %>
        return 0
    <% end match %>
}

micro sleep(ms: i32): void {
    <% match arch %>
        <% case "clr" %>
        std.adaptor.dotnet.threading.task_delay(ms)
        <% case "jvm" %>
        std.adaptor.jvm.thread.jvm_thread_sleep(ms)
        <% case "wasm32" %>
        std.adaptor.wasm.timer.set_timeout(0, ms)
        <% else %>
        return
    <% end match %>
}

micro elapsed_since(start: i64): i64 {
    return monotonic() - start
}