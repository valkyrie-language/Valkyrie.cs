# std.collections: ArrayList<T>

class ArrayList<T> {
    items: [T]
    length: usize
}

micro array_list_new<T>(capacity: usize): ArrayList<T> {
    return ArrayList<T> {
        items: [],
        length: 0
    }
}

micro array_list_capacity<T>(list: ArrayList<T>): usize {
    return len(list.items)
}

micro array_list_reserve<T>(list: ArrayList<T>, additional: usize): ArrayList<T> {
    return list
}

micro array_list_shrink_to_fit<T>(list: ArrayList<T>): ArrayList<T> {
    return list
}

micro array_list_truncate<T>(list: ArrayList<T>, size: usize): ArrayList<T> {
    let result: ArrayList<T> = list
    if size < result.length {
        result.length = size
    }
    return result
}

micro array_list_as_slice<T>(list: ArrayList<T>): [T] {
    return list.items
}

micro array_list_push<T>(list: ArrayList<T>, value: T): ArrayList<T> {
    let result: ArrayList<T> = list
    push(result.items, value)
    result.length = result.length + 1
    return result
}

micro array_list_pop<T>(list: ArrayList<T>): Option<T> {
    if list.length == 0 {
        return None
    }
    return Some { value: list.items[list.length - 1] }
}

micro array_list_insert<T>(list: ArrayList<T>, index: usize, value: T): ArrayList<T> {
    let result: ArrayList<T> = list
    if index > result.length {
        return result
    }
    push(result.items, value)
    let mut i: usize = result.length
    while i > index {
        result.items[i] = result.items[i - 1]
        i = i - 1
    }
    result.items[index] = value
    result.length = result.length + 1
    return result
}

micro array_list_remove<T>(list: ArrayList<T>, index: usize): Option<T> {
    if index >= list.length {
        return None
    }
    return Some { value: list.items[index] }
}

micro array_list_swap_remove<T>(list: ArrayList<T>, index: usize): Option<T> {
    if index >= list.length {
        return None
    }
    return Some { value: list.items[index] }
}

micro array_list_clear<T>(list: ArrayList<T>): ArrayList<T> {
    let result: ArrayList<T> = list
    result.length = 0
    return result
}

micro array_list_len<T>(list: ArrayList<T>): usize {
    return list.length
}

micro array_list_is_empty<T>(list: ArrayList<T>): bool {
    return list.length == 0
}

micro array_list_first<T>(list: ArrayList<T>): Option<T> {
    if list.length == 0 {
        return None
    }
    return Some { value: list.items[0] }
}

micro array_list_last<T>(list: ArrayList<T>): Option<T> {
    if list.length == 0 {
        return None
    }
    return Some { value: list.items[list.length - 1] }
}

micro array_list_get<T>(list: ArrayList<T>, index: usize): Option<T> {
    if index >= list.length {
        return None
    }
    return Some { value: list.items[index] }
}

micro array_list_set<T>(list: ArrayList<T>, index: usize, value: T): ArrayList<T> {
    let result: ArrayList<T> = list
    if index < result.length {
        result.items[index] = value
    }
    return result
}

micro array_list_contains<T>(list: ArrayList<T>, value: T): bool {
    let mut i: usize = 0
    while i < list.length {
        if list.items[i] == value {
            return true
        }
        i = i + 1
    }
    return false
}

micro array_list_retain<T>(list: ArrayList<T>, pred: micro(T) -> bool): ArrayList<T> {
    let result: ArrayList<T> = array_list_new<T>(0)
    let mut i: usize = 0
    while i < list.length {
        if pred(list.items[i]) {
            push(result.items, list.items[i])
            result.length = result.length + 1
        }
        i = i + 1
    }
    return result
}

micro array_list_from_slice<T>(slice: [T]): ArrayList<T> {
    let result: ArrayList<T> = array_list_new<T>(len(slice))
    let mut i: usize = 0
    while i < len(slice) {
        push(result.items, slice[i])
        result.length = result.length + 1
        i = i + 1
    }
    return result
}

micro array_list_extend<T>(list: ArrayList<T>, other: ArrayList<T>): ArrayList<T> {
    let result: ArrayList<T> = list
    let mut i: usize = 0
    while i < other.length {
        push(result.items, other.items[i])
        result.length = result.length + 1
        i = i + 1
    }
    return result
}

micro array_list_slice<T>(list: ArrayList<T>, start: usize, end: usize): ArrayList<T> {
    let result: ArrayList<T> = array_list_new<T>(0)
    let mut i: usize = start
    while i < end and i < list.length {
        push(result.items, list.items[i])
        result.length = result.length + 1
        i = i + 1
    }
    return result
}

micro array_list_iter<T>(list: ArrayList<T>, f: micro(T) -> void): void {
    let mut i: usize = 0
    while i < list.length {
        f(list.items[i])
        i = i + 1
    }
}

micro array_list_map<T, U>(list: ArrayList<T>, f: micro(T) -> U): ArrayList<U> {
    let result: ArrayList<U> = array_list_new<U>(0)
    let mut i: usize = 0
    while i < list.length {
        push(result.items, f(list.items[i]))
        result.length = result.length + 1
        i = i + 1
    }
    return result
}

micro array_list_filter<T>(list: ArrayList<T>, pred: micro(T) -> bool): ArrayList<T> {
    let result: ArrayList<T> = array_list_new<T>(0)
    let mut i: usize = 0
    while i < list.length {
        if pred(list.items[i]) {
            push(result.items, list.items[i])
            result.length = result.length + 1
        }
        i = i + 1
    }
    return result
}

micro array_list_fold<T, U>(list: ArrayList<T>, init: U, f: micro(U, T) -> U): U {
    let mut acc: U = init
    let mut i: usize = 0
    while i < list.length {
        acc = f(acc, list.items[i])
        i = i + 1
    }
    return acc
}

micro array_list_find<T>(list: ArrayList<T>, pred: micro(T) -> bool): Option<T> {
    let mut i: usize = 0
    while i < list.length {
        if pred(list.items[i]) {
            return Some { value: list.items[i] }
        }
        i = i + 1
    }
    return None
}

micro array_list_find_index<T>(list: ArrayList<T>, pred: micro(T) -> bool): Option<usize> {
    let mut i: usize = 0
    while i < list.length {
        if pred(list.items[i]) {
            return Some { value: i }
        }
        i = i + 1
    }
    return None
}

micro array_list_all<T>(list: ArrayList<T>, pred: micro(T) -> bool): bool {
    let mut i: usize = 0
    while i < list.length {
        if !pred(list.items[i]) {
            return false
        }
        i = i + 1
    }
    return true
}

micro array_list_any<T>(list: ArrayList<T>, pred: micro(T) -> bool): bool {
    let mut i: usize = 0
    while i < list.length {
        if pred(list.items[i]) {
            return true
        }
        i = i + 1
    }
    return false
}

micro array_list_reverse<T>(list: ArrayList<T>): ArrayList<T> {
    let result: ArrayList<T> = list
    if result.length == 0 {
        return result
    }
    let mut lo: usize = 0
    let mut hi: usize = result.length - 1
    while lo < hi {
        let tmp: T = result.items[lo]
        result.items[lo] = result.items[hi]
        result.items[hi] = tmp
        lo = lo + 1
        hi = hi - 1
    }
    return result
}

micro array_list_remove_value<T>(list: ArrayList<T>, value: T): ArrayList<T> {
    let result: ArrayList<T> = array_list_new<T>(0)
    let mut removed: bool = false
    let mut i: usize = 0
    while i < list.length {
        if !removed and list.items[i] == value {
            removed = true
        } else {
            push(result.items, list.items[i])
            result.length = result.length + 1
        }
        i = i + 1
    }
    return result
}

micro array_list_join(list: ArrayList<utf8>, sep: utf8): utf8 {
    if list.length == 0 {
        return ""
    }
    let mut result: utf8 = list.items[0]
    let mut i: usize = 1
    while i < list.length {
        result = result + sep + list.items[i]
        i = i + 1
    }
    return result
}