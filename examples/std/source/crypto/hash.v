# std.crypto: hash — 哈希与随机数
# 编译时根据 arch 委托 adaptor 实现

micro sha256(data: utf8): utf8 {
    <% match arch %>
        <% else %>
        return ""
    <% end match %>
}

micro md5(data: utf8): utf8 {
    <% match arch %>
        <% else %>
        return ""
    <% end match %>
}

micro hmac_sha256(key: utf8, data: utf8): utf8 {
    <% match arch %>
        <% else %>
        return ""
    <% end match %>
}

micro random_bytes(len: i32): utf8 {
    <% match arch %>
        <% case "wasm32" %>
        std.adaptor.wasm.crypto.crypto_random(0, len)
        return ""
        <% else %>
        return ""
    <% end match %>
}

micro random_uuid(): utf8 {
    <% match arch %>
        <% case "wasm32" %>
        return std.adaptor.wasm.crypto.crypto_uuid()
        <% else %>
        return ""
    <% end match %>
}