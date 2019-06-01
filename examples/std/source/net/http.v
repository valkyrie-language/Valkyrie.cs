# std.net: http — HTTP 客户端
# 编译时根据 arch 委托 adaptor 实现

micro get(url: utf8): utf8 {
    <% match arch %>
        <% case "clr" %>
        return std.adaptor.dotnet.net.http_get_string_async(url)
        <% case "wasm32" %>
        let handle: i32 = std.adaptor.wasm.fetch.http_fetch(url)
        return std.adaptor.wasm.fetch.response_text(handle)
        <% else %>
        return ""
    <% end match %>
}

micro post(url: utf8, body: utf8): utf8 {
    <% match arch %>
        <% case "clr" %>
        return std.adaptor.dotnet.net.http_post_async(url, 0)
        <% case "wasm32" %>
        let handle: i32 = std.adaptor.wasm.fetch.http_fetch(url)
        return std.adaptor.wasm.fetch.response_text(handle)
        <% else %>
        return ""
    <% end match %>
}

micro put(url: utf8, body: utf8): utf8 {
    <% match arch %>
        <% case "wasm32" %>
        let handle: i32 = std.adaptor.wasm.fetch.http_fetch(url)
        return std.adaptor.wasm.fetch.response_text(handle)
        <% else %>
        return ""
    <% end match %>
}

micro delete(url: utf8): utf8 {
    <% match arch %>
        <% case "wasm32" %>
        let handle: i32 = std.adaptor.wasm.fetch.http_fetch(url)
        return std.adaptor.wasm.fetch.response_text(handle)
        <% else %>
        return ""
    <% end match %>
}

micro get_json(url: utf8): Option<Value> {
    let text: utf8 = get(url)
    if text == "" {
        return None
    }
    let val: Value = parse(text)
    return Some { value: val }
}

micro post_json(url: utf8, body: Value): utf8 {
    let text: utf8 = stringify(body)
    return post(url, text)
}