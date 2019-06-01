# std.io: read — 控制台输入

micro read_line(): utf8 {
    <% match arch %>
        <% case "clr" %>
        return std.adaptor.dotnet.console.console_read_line()
        <% case "jvm" %>
        return std.adaptor.jvm.console.jvm_read_line(0)
        <% else %>
        return ""
    <% end match %>
}

micro read_all(): utf8 {
    <% match arch %>
        <% case "clr" %>
        return std.adaptor.dotnet.io.file_read_all_text("")
        <% else %>
        return ""
    <% end match %>
}

micro read_char(): i32 {
    <% match arch %>
        <% case "clr" %>
        return std.adaptor.dotnet.console.console_read_key()
        <% case "jvm" %>
        return std.adaptor.jvm.console.jvm_read()
        <% else %>
        return 0
    <% end match %>
}

micro read_int(): Option<i32> {
    let line: utf8 = read_line()
    if line == "" {
        return None
    }
    return Some { value: to_i32(line) }
}

micro read_f64(): Option<f64> {
    let line: utf8 = read_line()
    if line == "" {
        return None
    }
    return Some { value: to_f64(line) }
}

micro stdin_lines(): List<utf8> {
    let lines: List<utf8> = List.new()
    let mut line: utf8 = read_line()
    while line != "" {
        List.push(lines, line)
        line = read_line()
    }
    return lines
}