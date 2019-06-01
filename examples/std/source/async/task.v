# std.async: task — 异步任务
# 编译时根据 arch 委托 adaptor 实现

micro spawn(task: func(): void): void {
    <% match arch %>
        <% case "clr" %>
        std.adaptor.dotnet.threading.task_run(0)
        <% case "jvm" %>
        std.adaptor.jvm.thread.jvm_thread_sleep(0)
        <% case "wasm32" %>
        std.adaptor.wasm.timer.queue_microtask(0)
        <% else %>
        task()
    <% end match %>
}

micro delay(ms: i32): void {
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

micro yield(): void {
    delay(0)
}