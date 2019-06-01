# VOA JS Builtin — DOM
# [js_builtin] 直接映射 JS 内置 DOM API，无依赖
# DOM 操作全部有副作用，不标注 pure

# 元素查询

[js_builtin("document.getElementById")]
micro dom_get_by_id(id: string): i32

[js_builtin("document.querySelector")]
micro dom_query(selector: string): i32

[js_builtin("document.querySelectorAll")]
micro dom_query_all(selector: string): i32

# 元素创建

[js_builtin("document.createElement")]
micro dom_create_element(tag: string): i32

[js_builtin("document.createTextNode")]
micro dom_create_text(text: string): i32

# 元素操作

[js_builtin("Element.setAttribute")]
micro dom_set_attr(handle: i32, name: string, value: string)

[js_builtin("Element.getAttribute")]
micro dom_get_attr(handle: i32, name: string): string

[js_builtin("Element.removeAttribute")]
micro dom_remove_attr(handle: i32, name: string)

[js_builtin("Element.textContent")]
micro dom_set_text(handle: i32, text: string)

[js_builtin("Element.innerHTML")]
micro dom_set_inner_html(handle: i32, html: string)

# 样式操作

[js_builtin("Element.style.setProperty")]
micro dom_set_style(handle: i32, prop: string, value: string)

[js_builtin("Element.classList.add")]
micro dom_class_add(handle: i32, token: string)

[js_builtin("Element.classList.remove")]
micro dom_class_remove(handle: i32, token: string)

[js_builtin("Element.classList.toggle")]
micro dom_class_toggle(handle: i32, token: string)

# 树操作

[js_builtin("Element.appendChild")]
micro dom_append(parent: i32, child: i32)

[js_builtin("Element.removeChild")]
micro dom_remove_child(parent: i32, child: i32)

[js_builtin("Element.insertBefore")]
micro dom_insert_before(parent: i32, child: i32, ref: i32)

[js_builtin("Element.replaceChild")]
micro dom_replace_child(parent: i32, child: i32, old: i32)

# 事件

[js_builtin("Element.addEventListener")]
micro dom_add_event(handle: i32, event: string, callback: i32)

[js_builtin("Element.removeEventListener")]
micro dom_remove_event(handle: i32, event: string, callback: i32)

# 属性

[js_builtin("Element.parentElement")]
micro dom_parent(handle: i32): i32

[js_builtin("Element.children")]
micro dom_children(handle: i32): i32

[js_builtin("Element.firstElementChild")]
micro dom_first_child(handle: i32): i32

[js_builtin("Element.lastElementChild")]
micro dom_last_child(handle: i32): i32

[js_builtin("Element.nextElementSibling")]
micro dom_next_sibling(handle: i32): i32

[js_builtin("Element.previousElementSibling")]
micro dom_prev_sibling(handle: i32): i32
