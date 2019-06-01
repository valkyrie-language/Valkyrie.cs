namespace package.io;
# std.io: print — 控制台输出

micro print(message: utf8): void {
    <% match arch %>
        <% case "clr" %>
        std.adaptor.dotnet.console.console_write(message)
        <% case "jvm" %>
        std.adaptor.jvm.console.jvm_print(message)
        <% case "wasm32" %>
        std.adaptor.wasm.console.console_log(message)
        <% else %>
        return
    <% end match %>
}

micro print_line(message: utf8): void {
    <% match arch %>
        <% case "clr" %>
        std.adaptor.dotnet.console.console_write_line(message)
        <% case "jvm" %>
        std.adaptor.jvm.console.jvm_println(message)
        <% case "wasm32" %>
        std.adaptor.wasm.console.console_log(message)
        <% else %>
        return
    <% end match %>
}

micro eprint(msg: utf8): void {
    <% match arch %>
        <% case "clr" %>
        std.adaptor.dotnet.console.console_write(msg)
        <% case "jvm" %>
        std.adaptor.jvm.console.jvm_err_println(msg)
        <% case "wasm32" %>
        std.adaptor.wasm.console.console_error(msg)
        <% else %>
        return
    <% end match %>
}

micro eprintln(msg: utf8): void {
    <% match arch %>
        <% case "clr" %>
        std.adaptor.dotnet.console.console_write_line(msg)
        <% case "jvm" %>
        std.adaptor.jvm.console.jvm_err_println(msg)
        <% case "wasm32" %>
        std.adaptor.wasm.console.console_error(msg)
        <% else %>
        return
    <% end match %>
}

micro dbg(msg: utf8): void {
    <% match arch %>
        <% case "wasm32" %>
        std.adaptor.wasm.console.console_debug(msg)
        <% else %>
        println(msg)
    <% end match %>
}

micro print_int(value: i32): void {
    print(to_utf8(value))
}

micro print_int64(value: i64): void {
    print(to_utf8(value))
}

micro print_f64(value: f64): void {
    print(to_utf8(value))
}

micro print_bool(value: bool): void {
    if value {
        print("true")
    } else {
        print("false")
    }
}

micro println_int(value: i32): void {
    println(to_utf8(value))
}

micro println_int64(value: i64): void {
    println(to_utf8(value))
}

micro println_f64(value: f64): void {
    println(to_utf8(value))
}

micro println_bool(value: bool): void {
    if value {
        println("true")
    } else {
        println("false")
    }
}

micro print_fmt(fmt: utf8, arg: utf8): void {
    let formatted: utf8 = format(fmt, arg)
    print(formatted)
}