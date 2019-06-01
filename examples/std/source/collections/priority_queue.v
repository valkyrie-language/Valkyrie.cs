# std.collections: PriorityQueue — 优先队列（最小堆）

class PriorityQueue<T> {
    data: List<T>
    cmp: func(T, T): i32
}

imply PriorityQueue<T> {
    micro new(cmp: func(T, T): i32): Self {
        return Self { data: ArrayList.new(0), cmp: cmp }
    }
    micro push(mut self, value: T) {
        self.data.push(value)
        self.sift_up(self.data.len() - 1)
    }
    micro pop(mut self): Option<T> {
        if self.data.is_empty() {
            return None
        }
        let len: usize = self.data.len()
        if len == 1 {
            return self.data.remove(0)
        }
        let result: T = self.data.get(0).unwrap()
        let last: T = self.data.remove(len - 1).unwrap()
        self.data.set(0, last)
        self.sift_down(0)
        return Some { value: result }
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
    micro sift_up(mut self, index: usize) {
        let mut i: usize = index
        while i > 0 {
            let parent: usize = (i - 1) / 2
            let current: T = self.data.get(i).unwrap()
            let parent_val: T = self.data.get(parent).unwrap()
            if self.cmp(current, parent_val) >= 0 {
                break
            }
            self.data.set(i, parent_val)
            self.data.set(parent, current)
            i = parent
        }
    }
    micro sift_down(mut self, index: usize) {
        let len: usize = self.data.len()
        let mut i: usize = index
        while true {
            let left: usize = 2 * i + 1
            let right: usize = 2 * i + 2
            let mut smallest: usize = i
            if left < len && self.cmp(self.data.get(left).unwrap(), self.data.get(smallest).unwrap()) < 0 {
                smallest = left
            }
            if right < len && self.cmp(self.data.get(right).unwrap(), self.data.get(smallest).unwrap()) < 0 {
                smallest = right
            }
            if smallest == i {
                break
            }
            let tmp: T = self.data.get(i).unwrap()
            self.data.set(i, self.data.get(smallest).unwrap())
            self.data.set(smallest, tmp)
            i = smallest
        }
    }
}