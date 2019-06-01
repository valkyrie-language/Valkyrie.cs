# std.collections: LinkedList — 双向链表

class Node<T> {
    value: T
    next: Option<Node<T>>
    prev: Option<Node<T>>
}

class LinkedList<T> {
    head: Option<Node<T>>
    tail: Option<Node<T>>
    length: usize
}

imply LinkedList<T> {
    micro new(): Self {
        return Self { head: None, tail: None, length: 0 }
    }
    micro push_front(mut self, value: T) {
        let mut node: Node<T> = Node { value: value, next: self.head, prev: None }
        if self.head.is_some() {
            self.head.unwrap().prev = Some { value: node }
        } else {
            self.tail = Some { value: node }
        }
        self.head = Some { value: node }
        self.length = self.length + 1
    }
    micro push_back(mut self, value: T) {
        let mut node: Node<T> = Node { value: value, next: None, prev: self.tail }
        if self.tail.is_some() {
            self.tail.unwrap().next = Some { value: node }
        } else {
            self.head = Some { value: node }
        }
        self.tail = Some { value: node }
        self.length = self.length + 1
    }
    micro pop_front(mut self): Option<T> {
        if self.head.is_none() {
            return None
        }
        let node: Node<T> = self.head.unwrap()
        self.head = node.next
        if self.head.is_some() {
            self.head.unwrap().prev = None
        } else {
            self.tail = None
        }
        self.length = self.length - 1
        return Some { value: node.value }
    }
    micro pop_back(mut self): Option<T> {
        if self.tail.is_none() {
            return None
        }
        let node: Node<T> = self.tail.unwrap()
        self.tail = node.prev
        if self.tail.is_some() {
            self.tail.unwrap().next = None
        } else {
            self.head = None
        }
        self.length = self.length - 1
        return Some { value: node.value }
    }
    micro peek_front(self): Option<T> {
        if self.head.is_none() {
            return None
        }
        return Some { value: self.head.unwrap().value }
    }
    micro peek_back(self): Option<T> {
        if self.tail.is_none() {
            return None
        }
        return Some { value: self.tail.unwrap().value }
    }
    micro len(self): usize {
        return self.length
    }
    micro is_empty(self): bool {
        return self.length == 0
    }
    micro clear(mut self) {
        self.head = None
        self.tail = None
        self.length = 0
    }
    micro iter(self, f: func(T): void) {
        let mut current: Option<Node<T>> = self.head
        while current.is_some() {
            f(current.unwrap().value)
            current = current.unwrap().next
        }
    }
    micro iter_reverse(self, f: func(T): void) {
        let mut current: Option<Node<T>> = self.tail
        while current.is_some() {
            f(current.unwrap().value)
            current = current.unwrap().prev
        }
    }
}