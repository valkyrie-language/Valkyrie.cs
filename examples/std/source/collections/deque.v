# std.collections: Deque — 双端队列

class Deque<T> {
    data: List<T>
}

imply Deque<T> {
    micro new(): Self {
        return Self { data: ArrayList.new(0) }
    }
    micro push_front(mut self, value: T) {
        self.data.insert(0, value)
    }
    micro push_back(mut self, value: T) {
        self.data.push(value)
    }
    micro pop_front(mut self): Option<T> {
        if self.data.is_empty() {
            return None
        }
        return self.data.remove(0)
    }
    micro pop_back(mut self): Option<T> {
        return self.data.pop()
    }
    micro peek_front(self): Option<T> {
        return self.data.first()
    }
    micro peek_back(self): Option<T> {
        return self.data.last()
    }
    micro len(self): usize {
        return self.data.len()
    }
    micro is_empty(self): bool {
        return self.data.is_empty()
    }
    micro clear(mut self) {
        self.data.clear()
    }
    micro iter(self, f: func(T): void) {
        self.data.iter(f)
    }
}