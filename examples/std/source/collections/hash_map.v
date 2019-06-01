# std.collections: Dict — 关联映射 trait + HashMap 实现

trait Dict<K, V> {
    micro get(self, key: K): Option<V>
    micro insert(mut self, key: K, value: V): Option<V>
    micro remove(mut self, key: K): Option<V>
    micro contains_key(self, key: K): bool
    micro keys(self): List<K>
    micro values(self): List<V>
    micro len(self): usize
    micro is_empty(self): bool
    micro clear(mut self)
    micro iter(self, f: func(K, V): void)
}

structure Entry<K, V> {
    key: K
    value: V
}

class HashMap<K, V> {
    buckets: List<List<Entry<K, V>>>
    size: usize
}

imply Dict<K, V> for HashMap<K, V> {
    micro get(self, key: K): Option<V> {
        let idx: usize = self.bucket_idx(key)
        let bucket: List<Entry<K, V>> = self.buckets.get(idx).unwrap()
        let mut i: usize = 0
        while i < bucket.len() {
            let entry: Entry<K, V> = bucket.get(i).unwrap()
            if entry.key == key {
                return Some { value: entry.value }
            }
            i = i + 1
        }
        return None
    }
    micro insert(mut self, key: K, value: V): Option<V> {
        if self.size * 2 >= self.buckets.len() {
            self.resize()
        }
        let idx: usize = self.bucket_idx(key)
        let bucket: List<Entry<K, V>> = self.buckets.get(idx).unwrap()
        let mut i: usize = 0
        while i < bucket.len() {
            if bucket.get(i).unwrap().key == key {
                let old: V = bucket.get(i).unwrap().value
                let entry: Entry<K, V> = Entry { key: key, value: value }
                bucket.set(i, entry)
                return Some { value: old }
            }
            i = i + 1
        }
        bucket.push(Entry { key: key, value: value })
        self.size = self.size + 1
        return None
    }
    micro remove(mut self, key: K): Option<V> {
        let idx: usize = self.bucket_idx(key)
        let bucket: List<Entry<K, V>> = self.buckets.get(idx).unwrap()
        let mut i: usize = 0
        while i < bucket.len() {
            if bucket.get(i).unwrap().key == key {
                self.size = self.size - 1
                let removed: Entry<K, V> = bucket.remove(i).unwrap()
                return Some { value: removed.value }
            }
            i = i + 1
        }
        return None
    }
    micro contains_key(self, key: K): bool {
        return self.get(key).is_some()
    }
    micro keys(self): List<K> {
        let mut result: List<K> = ArrayList.new(0)
        let mut i: usize = 0
        while i < self.buckets.len() {
            let bucket: List<Entry<K, V>> = self.buckets.get(i).unwrap()
            let mut j: usize = 0
            while j < bucket.len() {
                result.push(bucket.get(j).unwrap().key)
                j = j + 1
            }
            i = i + 1
        }
        return result
    }
    micro values(self): List<V> {
        let mut result: List<V> = ArrayList.new(0)
        let mut i: usize = 0
        while i < self.buckets.len() {
            let bucket: List<Entry<K, V>> = self.buckets.get(i).unwrap()
            let mut j: usize = 0
            while j < bucket.len() {
                result.push(bucket.get(j).unwrap().value)
                j = j + 1
            }
            i = i + 1
        }
        return result
    }
    micro len(self): usize {
        return self.size
    }
    micro is_empty(self): bool {
        return self.size == 0
    }
    micro clear(mut self) {
        let mut i: usize = 0
        while i < self.buckets.len() {
            self.buckets.get(i).unwrap().clear()
            i = i + 1
        }
        self.size = 0
    }
    micro iter(self, f: func(K, V): void) {
        let mut i: usize = 0
        while i < self.buckets.len() {
            let bucket: List<Entry<K, V>> = self.buckets.get(i).unwrap()
            let mut j: usize = 0
            while j < bucket.len() {
                let entry: Entry<K, V> = bucket.get(j).unwrap()
                f(entry.key, entry.value)
                j = j + 1
            }
            i = i + 1
        }
    }
}

imply HashMap<K, V> {
    micro new(capacity: usize): Self {
        let mut buckets: List<List<Entry<K, V>>> = ArrayList.new(capacity)
        let mut i: usize = 0
        while i < capacity {
            buckets.push(ArrayList.new(0))
            i = i + 1
        }
        return Self { buckets: buckets, size: 0 }
    }
    micro hash(self, key: K): i32 {
        <% match typeof(K) %>
            <% case "utf8" %>
                return self.hash_string(key as utf8)
            <% case "i32" %>
                return key as i32
            <% case "u32" %>
                return key as i32
            <% case "i64" %>
                return (key as i64) as i32
            <% case "u64" %>
                return (key as u64) as i32
            <% case "f32" %>
                return ((key as f32) * 1000000.0) as i32
            <% case "f64" %>
                return ((key as f64) * 1000000.0) as i32
            <% else %>
                return 0
        <% end match %>
    }
    micro hash_string(self, s: utf8): i32 {
        let mut hash: i32 = 0
        let mut i: usize = 0
        while i < s.length {
            hash = hash * 31 + s[i] as i32
            i = i + 1
        }
        return hash
    }
    micro bucket_idx(self, key: K): usize {
        let h: i32 = self.hash(key)
        let idx: i32 = h % self.buckets.len() as i32
        if idx < 0 {
            return (-idx) as usize
        }
        return idx as usize
    }
    micro resize(mut self) {
        let old_buckets: List<List<Entry<K, V>>> = self.buckets
        let new_size: usize = old_buckets.len() * 2
        let mut new_buckets: List<List<Entry<K, V>>> = ArrayList.new(new_size)
        let mut i: usize = 0
        while i < new_size {
            new_buckets.push(ArrayList.new(0))
            i = i + 1
        }
        self.buckets = new_buckets
        self.size = 0
        let mut j: usize = 0
        while j < old_buckets.len() {
            let bucket: List<Entry<K, V>> = old_buckets.get(j).unwrap()
            let mut k: usize = 0
            while k < bucket.len() {
                let entry: Entry<K, V> = bucket.get(k).unwrap()
                self.insert(entry.key, entry.value)
                k = k + 1
            }
            j = j + 1
        }
    }
}