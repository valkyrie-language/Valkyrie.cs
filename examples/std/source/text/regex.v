# std.text: regex — 正则表达式

struct Regex {
    pattern: utf8
}

micro compile(pattern: utf8): Regex {
    return Regex { pattern: pattern }
}

micro is_match(re: Regex, text: utf8): bool {
    <% match arch %>
        <% case "clr" %>
        return std.adaptor.dotnet.regex.is_match(re.pattern, text)
        <% case "jvm" %>
        return std.adaptor.jvm.regex.is_match(re.pattern, text)
        <% case "wasm32" %>
        return std.adaptor.wasm.regex.is_match(re.pattern, text)
        <% else %>
        return false
    <% end match %>
}

micro find(re: Regex, text: utf8): Option<Match> {
    <% match arch %>
        <% case "clr" %>
        return std.adaptor.dotnet.regex.find(re.pattern, text)
        <% case "jvm" %>
        return std.adaptor.jvm.regex.find(re.pattern, text)
        <% case "wasm32" %>
        return std.adaptor.wasm.regex.find(re.pattern, text)
        <% else %>
        return None
    <% end match %>
}

micro find_all(re: Regex, text: utf8): List<Match> {
    <% match arch %>
        <% case "clr" %>
        return std.adaptor.dotnet.regex.find_all(re.pattern, text)
        <% case "jvm" %>
        return std.adaptor.jvm.regex.find_all(re.pattern, text)
        <% case "wasm32" %>
        return std.adaptor.wasm.regex.find_all(re.pattern, text)
        <% else %>
        return List.new()
    <% end match %>
}

micro replace(re: Regex, text: utf8, replacement: utf8): utf8 {
    <% match arch %>
        <% case "clr" %>
        return std.adaptor.dotnet.regex.replace(re.pattern, text, replacement)
        <% case "jvm" %>
        return std.adaptor.jvm.regex.replace(re.pattern, text, replacement)
        <% case "wasm32" %>
        return std.adaptor.wasm.regex.replace(re.pattern, text, replacement)
        <% else %>
        return text
    <% end match %>
}

micro split(re: Regex, text: utf8): List<utf8> {
    <% match arch %>
        <% case "clr" %>
        return std.adaptor.dotnet.regex.split(re.pattern, text)
        <% case "jvm" %>
        return std.adaptor.jvm.regex.split(re.pattern, text)
        <% case "wasm32" %>
        return std.adaptor.wasm.regex.split(re.pattern, text)
        <% else %>
        return List.new()
    <% end match %>
}

struct Match {
    index: i32
    length: i32
    groups: List<utf8>
}

micro match_value(m: Match): utf8 {
    if List.len(m.groups) > 0 {
        return List.get(m.groups, 0).unwrap()
    }
    return ""
}