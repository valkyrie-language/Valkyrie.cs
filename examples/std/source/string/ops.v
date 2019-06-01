# std.string: ops — 字符串操作
# 编译时根据 arch 委托 adaptor 实现，else 降级为内联

micro len(s: utf8): i32 {
    <% match arch %>
        <% case "clr" %>
        return std.adaptor.dotnet.string.string_length(s)
        <% case "jvm" %>
        return std.adaptor.jvm.string.jvm_string_length(s)
        <% else %>
        return 0
    <% end match %>
}

micro is_empty(s: utf8): bool {
    return len(s) == 0
}

micro concat(a: utf8, b: utf8): utf8 {
    <% match arch %>
        <% case "clr" %>
        return std.adaptor.dotnet.string.string_concat(a, b)
        <% case "jvm" %>
        return std.adaptor.jvm.string.jvm_string_concat(a, b)
        <% else %>
        return a
    <% end match %>
}

micro substring(s: utf8, start: i32, length: i32): utf8 {
    <% match arch %>
        <% case "clr" %>
        return std.adaptor.dotnet.string.string_substring(s, start, length)
        <% case "jvm" %>
        return std.adaptor.jvm.string.jvm_string_substring(s, start, length)
        <% else %>
        return ""
    <% end match %>
}

micro index_of(s: utf8, sub: utf8): i32 {
    <% match arch %>
        <% case "clr" %>
        return std.adaptor.dotnet.string.string_index_of(s, sub)
        <% case "jvm" %>
        return std.adaptor.jvm.string.jvm_string_index_of(s, sub)
        <% else %>
        return -1
    <% end match %>
}

micro last_index_of(s: utf8, sub: utf8): i32 {
    <% match arch %>
        <% case "jvm" %>
        return std.adaptor.jvm.string.jvm_string_last_index_of(s, sub)
        <% else %>
        return -1
    <% end match %>
}

micro replace(s: utf8, old: utf8, new: utf8): utf8 {
    <% match arch %>
        <% case "clr" %>
        return std.adaptor.dotnet.string.string_replace(s, old, new)
        <% case "jvm" %>
        return std.adaptor.jvm.string.jvm_string_replace(s, old, new)
        <% else %>
        return s
    <% end match %>
}

micro to_upper(s: utf8): utf8 {
    <% match arch %>
        <% case "clr" %>
        return std.adaptor.dotnet.string.string_to_upper(s)
        <% case "jvm" %>
        return std.adaptor.jvm.string.jvm_string_to_upper(s)
        <% else %>
        return s
    <% end match %>
}

micro to_lower(s: utf8): utf8 {
    <% match arch %>
        <% case "clr" %>
        return std.adaptor.dotnet.string.string_to_lower(s)
        <% case "jvm" %>
        return std.adaptor.jvm.string.jvm_string_to_lower(s)
        <% else %>
        return s
    <% end match %>
}

micro split(s: utf8, sep: utf8): List<utf8> {
    <% match arch %>
        <% case "clr" %>
        return std.adaptor.dotnet.string.string_split(s, sep)
        <% case "jvm" %>
        return std.adaptor.jvm.string.jvm_string_split(s, sep)
        <% else %>
        return List.new()
    <% end match %>
}

micro trim(s: utf8): utf8 {
    <% match arch %>
        <% case "clr" %>
        return std.adaptor.dotnet.string.string_trim(s)
        <% case "jvm" %>
        return std.adaptor.jvm.string.jvm_string_trim(s)
        <% else %>
        return s
    <% end match %>
}

micro trim_start(s: utf8): utf8 {
    <% match arch %>
        <% case "jvm" %>
        return std.adaptor.jvm.string.jvm_string_trim_start(s)
        <% else %>
        return s
    <% end match %>
}

micro trim_end(s: utf8): utf8 {
    <% match arch %>
        <% case "jvm" %>
        return std.adaptor.jvm.string.jvm_string_trim_end(s)
        <% else %>
        return s
    <% end match %>
}

micro starts_with(s: utf8, prefix: utf8): bool {
    let idx: i32 = index_of(s, prefix)
    return idx == 0
}

micro ends_with(s: utf8, suffix: utf8): bool {
    let idx: i32 = last_index_of(s, suffix)
    if idx == -1 { return false }
    return idx + len(suffix) == len(s)
}

micro contains(s: utf8, sub: utf8): bool {
    return index_of(s, sub) != -1
}

micro format(fmt: utf8, arg: utf8): utf8 {
    <% match arch %>
        <% case "clr" %>
        return std.adaptor.dotnet.string.string_format(fmt, arg)
        <% case "jvm" %>
        return std.adaptor.jvm.string.jvm_string_format(fmt, arg)
        <% else %>
        return fmt
    <% end match %>
}

micro repeat(s: utf8, n: i32): utf8 {
    let mut result: utf8 = ""
    let mut i: i32 = 0
    while i < n {
        result = concat(result, s)
        i = i + 1
    }
    return result
}

micro chars(s: utf8): List<utf8> {
    let result: List<utf8> = List.new()
    let mut i: i32 = 0
    let n: i32 = len(s)
    while i < n {
        let ch: utf8 = substring(s, i, 1)
        List.push(result, ch)
        i = i + 1
    }
    return result
}

micro lines(s: utf8): List<utf8> {
    return split(s, "\n")
}