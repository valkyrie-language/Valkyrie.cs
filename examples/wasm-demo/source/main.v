# WASM Demo - 演示 [js_builtin] 和 [js] 属性

# JS 内置 API 绑定
[js_builtin("console.log")]
micro js_log(msg: string)

[js_builtin("console.log")]
micro js_log_int(value: i32)

# DOM API 绑定
[js_builtin("document.getElementById")]
micro dom_get_element_by_id(id: string): i32

[js_builtin("Element.textContent")]
micro dom_set_text(handle: i32, text: string)

# 业务逻辑
micro add(x: i32, y: i32): i32 {
    return x + y
}

micro main(): i32 {
    let result = add(3, 4)
    js_log_int(result)
    let handle = dom_get_element_by_id("app")
    return result
}
