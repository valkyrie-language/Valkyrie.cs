# std.text: json

union Value {
    Null
    Bool { value: bool }
    Number { value: f64 }
    String { value: utf8 }
    Array { items: [Value] }
    Object { fields: [Pair<utf8, Value>] }
}

structure Pair<K, V> {
    key: K
    value: V
}

micro parse(text: utf8): Value {
    <% match arch %>
        <% case "wasm32" %>
        return std.adaptor.wasm.json.json_parse(text)
        <% case "clr" %>
        return std.adaptor.dotnet.json.json_parse(text)
        <% case "jvm" %>
        return std.adaptor.jvm.json.json_parse(text)
        <% else %>
        return Null
    <% end match %>
}

micro stringify(value: Value): utf8 {
    <% match arch %>
        <% case "wasm32" %>
        return std.adaptor.wasm.json.json_stringify(value)
        <% case "clr" %>
        return std.adaptor.dotnet.json.json_stringify(value)
        <% case "jvm" %>
        return std.adaptor.jvm.json.json_stringify(value)
        <% else %>
        return ""
    <% end match %>
}

micro get(obj: Value, key: utf8): Option<Value> {
    match obj {
        Object(fields) => {
            let mut i: i32 = 0
            while i < len(fields) {
                if fields[i].key == key {
                    return Some { value: fields[i].value }
                }
                i = i + 1
            }
            return None
        }
        _ => { return None }
    }
}

micro get_index(arr: Value, index: i32): Option<Value> {
    match arr {
        Array(items) => {
            if index < 0 or index >= len(items) {
                return None
            }
            return Some { value: items[index] }
        }
        _ => { return None }
    }
}

micro set(obj: Value, key: utf8, value: Value): Value {
    match obj {
        Object(fields) => {
            let mut i: i32 = 0
            while i < len(fields) {
                if fields[i].key == key {
                    fields[i].value = value
                    return Object { fields: fields }
                }
                i = i + 1
            }
            push(fields, Pair { key: key, value: value })
            return Object { fields: fields }
        }
        _ => { return obj }
    }
}

micro is_null(value: Value): bool {
    match value {
        Null => { return true }
        _ => { return false }
    }
}

micro as_bool(value: Value): Option<bool> {
    match value {
        Bool(v) => { return Some { value: v } }
        _ => { return None }
    }
}

micro as_number(value: Value): Option<f64> {
    match value {
        Number(v) => { return Some { value: v } }
        _ => { return None }
    }
}

micro as_string(value: Value): Option<utf8> {
    match value {
        String(v) => { return Some { value: v } }
        _ => { return None }
    }
}

micro as_array(value: Value): Option<[Value]> {
    match value {
        Array(items) => { return Some { value: items } }
        _ => { return None }
    }
}

micro as_object(value: Value): Option<[Pair<utf8, Value>]> {
    match value {
        Object(fields) => { return Some { value: fields } }
        _ => { return None }
    }
}
