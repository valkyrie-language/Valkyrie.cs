# std.collections: Queue — FIFO 队列

class Queue<T> {
    data: List<T>
}

imply Queue<T> {
    micro new(): Self {
        return Self { data: ArrayList.new(0) }
    }
    micro enqueue(mut self, value: T) {
        self.data.push(value)
    }
    micro dequeue(mut self): Option<T> {
        if self.data.is_empty() {
            return None
        }
        return self.data.remove(0)
    }
    micro peek(self): Option<T> {
        return self.data.first()
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