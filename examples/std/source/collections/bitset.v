# std.collections: BitSet — 位图

structure BitSet {
    words: List<u64>
}

imply BitSet {
    micro new(): Self {
        return Self { words: ArrayList.new(0) }
    }
    micro with_capacity(bits: usize): Self {
        let words_needed: usize = (bits + 63) / 64
        let mut words: List<u64> = ArrayList.new(0)
        let mut i: usize = 0
        while i < words_needed {
            words.push(0u64)
            i = i + 1
        }
        return Self { words: words }
    }
    micro set(mut self, index: usize) {
        let word_idx: usize = index / 64
        let bit_idx: usize = index % 64
        self.ensure_capacity(word_idx + 1)
        let word: u64 = self.words.get(word_idx).unwrap()
        self.words.set(word_idx, word | (1u64 << bit_idx))
    }
    micro clear_bit(mut self, index: usize) {
        let word_idx: usize = index / 64
        let bit_idx: usize = index % 64
        if word_idx >= self.words.len() {
            return
        }
        let word: u64 = self.words.get(word_idx).unwrap()
        self.words.set(word_idx, word & ~(1u64 << bit_idx))
    }
    micro get(self, index: usize): bool {
        let word_idx: usize = index / 64
        let bit_idx: usize = index % 64
        if word_idx >= self.words.len() {
            return false
        }
        let word: u64 = self.words.get(word_idx).unwrap()
        return (word & (1u64 << bit_idx)) != 0u64
    }
    micro toggle(mut self, index: usize) {
        if self.get(index) {
            self.clear_bit(index)
        } else {
            self.set(index)
        }
    }
    micro intersect(mut self, other: BitSet) {
        let min_len: usize = if self.words.len() < other.words.len() { self.words.len() } else { other.words.len() }
        let mut i: usize = 0
        while i < min_len {
            let a: u64 = self.words.get(i).unwrap()
            let b: u64 = other.words.get(i).unwrap()
            self.words.set(i, a & b)
            i = i + 1
        }
        while i < self.words.len() {
            self.words.set(i, 0u64)
            i = i + 1
        }
    }
    micro union(mut self, other: BitSet) {
        self.ensure_capacity(other.words.len())
        let mut i: usize = 0
        while i < other.words.len() {
            let a: u64 = self.words.get(i).unwrap()
            let b: u64 = other.words.get(i).unwrap()
            self.words.set(i, a | b)
            i = i + 1
        }
    }
    micro difference(mut self, other: BitSet) {
        let min_len: usize = if self.words.len() < other.words.len() { self.words.len() } else { other.words.len() }
        let mut i: usize = 0
        while i < min_len {
            let a: u64 = self.words.get(i).unwrap()
            let b: u64 = other.words.get(i).unwrap()
            self.words.set(i, a & ~b)
            i = i + 1
        }
    }
    micro xor(mut self, other: BitSet) {
        self.ensure_capacity(other.words.len())
        let min_len: usize = if self.words.len() < other.words.len() { self.words.len() } else { other.words.len() }
        let mut i: usize = 0
        while i < min_len {
            let a: u64 = self.words.get(i).unwrap()
            let b: u64 = other.words.get(i).unwrap()
            self.words.set(i, a ^ b)
            i = i + 1
        }
        while i < other.words.len() {
            self.words.set(i, other.words.get(i).unwrap())
            i = i + 1
        }
    }
    micro is_empty(self): bool {
        let mut i: usize = 0
        while i < self.words.len() {
            if self.words.get(i).unwrap() != 0u64 {
                return false
            }
            i = i + 1
        }
        return true
    }
    micro clear_all(mut self) {
        let mut i: usize = 0
        while i < self.words.len() {
            self.words.set(i, 0u64)
            i = i + 1
        }
    }
    micro ensure_capacity(mut self, needed: usize) {
        while self.words.len() < needed {
            self.words.push(0u64)
        }
    }
}