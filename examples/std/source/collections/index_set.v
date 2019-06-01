# std.collections: IndexSet — 保持插入顺序的集合

class IndexSet<T> {
    map: Dict<T, bool>
}

imply Set<T> for IndexSet<T> {
    micro insert(mut self, value: T): bool {
        if self.map.contains_key(value) {
            return false
        }
        self.map.insert(value, true)
        return true
    }
    micro remove(mut self, value: T): bool {
        return self.map.remove(value).is_some()
    }
    micro contains(self, value: T): bool {
        return self.map.contains_key(value)
    }
    micro len(self): usize {
        return self.map.len()
    }
    micro is_empty(self): bool {
        return self.map.is_empty()
    }
    micro clear(mut self) {
        self.map.clear()
    }
    micro iter(self, f: func(T): void) {
        self.map.iter(func(key: T, _: bool): void { f(key) })
    }
    micro to_list(self): List<T> {
        return self.map.keys()
    }
    micro from_list(list: List<T>): Self {
        let mut result: Self = Self.new()
        list.iter(func(value: T): void { result.insert(value) })
        return result
    }
}

imply IndexSet<T> {
    micro new(): Self {
        return Self { map: IndexMap.new() }
    }
}