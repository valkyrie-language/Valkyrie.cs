# std.collections: Map<K, V> — 关联映射

struct Entry<K, V> {
    key: K
    value: V
}

struct Map<K, V> {
    entries: List<Entry<K, V>>
}

micro new(): Map<K, V> {
    return Map { entries: List.new() }
}

micro get(map: Map<K, V>, key: K): Option<V> {
    let mut i: i32 = 0
    while i < List.len(map.entries) {
        let entry: Option<Entry<K, V>> = List.get(map.entries, i)
        if !entry.is_none() && entry.unwrap().key == key {
            return Some { value: entry.unwrap().value }
        }
        i = i + 1
    }
    return None
}

micro set(map: Map<K, V>, key: K, value: V): void {
    let mut i: i32 = 0
    while i < List.len(map.entries) {
        let entry: Option<Entry<K, V>> = List.get(map.entries, i)
        if !entry.is_none() && entry.unwrap().key == key {
            let updated: Entry<K, V> = Entry { key: key, value: value }
            List.set(map.entries, i, updated)
            return
        }
        i = i + 1
    }
    List.push(map.entries, Entry { key: key, value: value })
}

micro remove(map: Map<K, V>, key: K): bool {
    let mut i: i32 = 0
    while i < List.len(map.entries) {
        let entry: Option<Entry<K, V>> = List.get(map.entries, i)
        if !entry.is_none() && entry.unwrap().key == key {
            List.remove(map.entries, i)
            return true
        }
        i = i + 1
    }
    return false
}

micro contains(map: Map<K, V>, key: K): bool {
    let mut i: i32 = 0
    while i < List.len(map.entries) {
        let entry: Option<Entry<K, V>> = List.get(map.entries, i)
        if !entry.is_none() && entry.unwrap().key == key {
            return true
        }
        i = i + 1
    }
    return false
}

micro len(map: Map<K, V>): i32 {
    return List.len(map.entries)
}

micro is_empty(map: Map<K, V>): bool {
    return List.is_empty(map.entries)
}

micro clear(map: Map<K, V>): void {
    List.clear(map.entries)
}

micro keys(map: Map<K, V>): List<K> {
    let result: List<K> = List.new()
    let mut i: i32 = 0
    while i < List.len(map.entries) {
        let entry: Option<Entry<K, V>> = List.get(map.entries, i)
        if !entry.is_none() {
            List.push(result, entry.unwrap().key)
        }
        i = i + 1
    }
    return result
}

micro values(map: Map<K, V>): List<V> {
    let result: List<V> = List.new()
    let mut i: i32 = 0
    while i < List.len(map.entries) {
        let entry: Option<Entry<K, V>> = List.get(map.entries, i)
        if !entry.is_none() {
            List.push(result, entry.unwrap().value)
        }
        i = i + 1
    }
    return result
}

micro iter(map: Map<K, V>, f: func(K, V): void): void {
    let mut i: i32 = 0
    while i < List.len(map.entries) {
        let entry: Option<Entry<K, V>> = List.get(map.entries, i)
        if !entry.is_none() {
            f(entry.unwrap().key, entry.unwrap().value)
        }
        i = i + 1
    }
}

micro find(map: Map<K, V>, pred: func(K, V): bool): Option<V> {
    let mut i: i32 = 0
    while i < List.len(map.entries) {
        let entry: Option<Entry<K, V>> = List.get(map.entries, i)
        if !entry.is_none() && pred(entry.unwrap().key, entry.unwrap().value) {
            return Some { value: entry.unwrap().value }
        }
        i = i + 1
    }
    return None
}

micro map_values(map: Map<K, V>, f: func(K, V): U): Map<K, U> {
    let result: Map<K, U> = new()
    let mut i: i32 = 0
    while i < List.len(map.entries) {
        let entry: Option<Entry<K, V>> = List.get(map.entries, i)
        if !entry.is_none() {
            set(result, entry.unwrap().key, f(entry.unwrap().key, entry.unwrap().value))
        }
        i = i + 1
    }
    return result
}

micro filter(map: Map<K, V>, pred: func(K, V): bool): Map<K, V> {
    let result: Map<K, V> = new()
    let mut i: i32 = 0
    while i < List.len(map.entries) {
        let entry: Option<Entry<K, V>> = List.get(map.entries, i)
        if !entry.is_none() && pred(entry.unwrap().key, entry.unwrap().value) {
            set(result, entry.unwrap().key, entry.unwrap().value)
        }
        i = i + 1
    }
    return result
}