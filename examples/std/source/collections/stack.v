# std.collections: Stack — LIFO 栈

class Stack<T> {
    data: List<T>
}

imply Stack<T> {
    micro new(): Self {
        return Self { data: ArrayList.new(0) }
    }
    micro push(mut self, value: T) {
        self.data.push(value)
    }
    micro pop(mut self): Option<T> {
        return self.data.pop()
    }
    micro peek(self): Option<T> {
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
        let mut i: i32 = self.data.len() as i32 - 1
        while i >= 0 {
            f(self.data.get(i as usize).unwrap())
            i = i - 1
        }
    }
}