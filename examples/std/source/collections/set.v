# std.collections: Set — 集合 trait + HashSet 实现

trait Set<T> {
    micro insert(mut self, value: T): bool
    micro remove(mut self, value: T): bool
    micro contains(self, value: T): bool
    micro len(self): usize
    micro is_empty(self): bool
    micro clear(mut self)
    micro iter(self, f: func(T): void)
    micro to_list(self): List<T>
    micro from_list(list: List<T>): Self
}

class HashSet<T> {
    map: Dict<T, bool>
}

imply Set<T> for HashSet<T> {
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

imply HashSet<T> {
    micro new(): Self {
        return Self { map: HashMap.new(16) }
    }
}