# std.collections: HashSet<T> — 哈希集合

struct HashSet<T> {
    map: HashMap<T, bool>
}

micro new(): HashSet<T> {
    return HashSet { map: HashMap.new() }
}

micro insert(set: HashSet<T>, value: T): bool {
    if HashMap.contains_key(set.map, value) {
        return false
    }
    HashMap.insert(set.map, value, true)
    return true
}

micro remove(set: HashSet<T>, value: T): bool {
    return HashMap.remove(set.map, value).is_some()
}

micro contains(set: HashSet<T>, value: T): bool {
    return HashMap.contains_key(set.map, value)
}

micro len(set: HashSet<T>): i32 {
    return HashMap.len(set.map)
}

micro is_empty(set: HashSet<T>): bool {
    return HashMap.is_empty(set.map)
}

micro clear(set: HashSet<T>): void {
    HashMap.clear(set.map)
}

micro iter(set: HashSet<T>, f: func(T): void): void {
    HashMap.iter(set.map, func(key: T, _: bool): void {
        f(key)
    })
}

micro to_list(set: HashSet<T>): List<T> {
    return HashMap.keys(set.map)
}

micro from_list(list: List<T>): HashSet<T> {
    let result: HashSet<T> = new()
    List.iter(list, func(value: T): void {
        insert(result, value)
    })
    return result
}