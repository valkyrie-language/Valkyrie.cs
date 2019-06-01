# std.collections: BTreeMap — B 树有序映射

class BTreeNode<K, V> {
    keys: List<K>
    values: List<V>
    children: List<BTreeNode<K, V>>
    is_leaf: bool
}

class BTreeMap<K, V> {
    root: BTreeNode<K, V>
}

imply Dict<K, V> for BTreeMap<K, V> {
    micro get(self, key: K): Option<V> {
        return self.search_node(self.root, key)
    }
    micro insert(mut self, key: K, value: V): Option<V> {
        let root: BTreeNode<K, V> = self.root
        if root.keys.len() == 4 {
            let mut new_root: BTreeNode<K, V> = BTreeNode {
                keys: ArrayList.new(0),
                values: ArrayList.new(0),
                children: ArrayList.new(0),
                is_leaf: false
            }
            new_root.children.push(root)
            self.split_child(new_root, 0)
            self.root = new_root
            self.insert_non_full(new_root, key, value)
            return None
        }
        return self.insert_non_full(root, key, value)
    }
    micro remove(mut self, key: K): Option<V> {
        return None
    }
    micro contains_key(self, key: K): bool {
        return self.get(key).is_some()
    }
    micro keys(self): List<K> {
        let mut result: List<K> = ArrayList.new(0)
        self.iter(func(key: K, _: V): void { result.push(key) })
        return result
    }
    micro values(self): List<V> {
        let mut result: List<V> = ArrayList.new(0)
        self.iter(func(_: K, value: V): void { result.push(value) })
        return result
    }
    micro len(self): usize {
        return self.count_keys(self.root)
    }
    micro is_empty(self): bool {
        return self.root.keys.is_empty()
    }
    micro clear(mut self) {
        self.root = BTreeNode {
            keys: ArrayList.new(0),
            values: ArrayList.new(0),
            children: ArrayList.new(0),
            is_leaf: true
        }
    }
    micro iter(self, f: func(K, V): void) {
        self.iterate_node(self.root, f)
    }
}

imply BTreeMap<K, V> {
    micro new(): Self {
        return Self {
            root: BTreeNode {
                keys: ArrayList.new(0),
                values: ArrayList.new(0),
                children: ArrayList.new(0),
                is_leaf: true
            }
        }
    }
    micro search_node(self, node: BTreeNode<K, V>, key: K): Option<V> {
        let mut i: usize = 0
        while i < node.keys.len() {
            let current_key: K = node.keys.get(i).unwrap()
            if key == current_key {
                return node.values.get(i)
            }
            if key < current_key {
                if node.is_leaf {
                    return None
                }
                return self.search_node(node.children.get(i).unwrap(), key)
            }
            i = i + 1
        }
        if node.is_leaf {
            return None
        }
        return self.search_node(node.children.get(i).unwrap(), key)
    }
    micro insert_non_full(mut self, node: BTreeNode<K, V>, key: K, value: V): Option<V> {
        let mut i: i32 = node.keys.len() as i32 - 1
        if node.is_leaf {
            while i >= 0 && key < node.keys.get(i as usize).unwrap() {
                i = i - 1
            }
            i = i + 1
            node.keys.insert(i as usize, key)
            node.values.insert(i as usize, value)
            return None
        }
        while i >= 0 && key < node.keys.get(i as usize).unwrap() {
            i = i - 1
        }
        i = i + 1
        let child: BTreeNode<K, V> = node.children.get(i as usize).unwrap()
        if child.keys.len() == 4 {
            self.split_child(node, i as usize)
            if key > node.keys.get(i as usize).unwrap() {
                i = i + 1
            }
        }
        return self.insert_non_full(node.children.get(i as usize).unwrap(), key, value)
    }
    micro split_child(mut self, parent: BTreeNode<K, V>, idx: usize) {
        let child: BTreeNode<K, V> = parent.children.get(idx).unwrap()
        let mut new_child: BTreeNode<K, V> = BTreeNode {
            keys: ArrayList.new(0),
            values: ArrayList.new(0),
            children: ArrayList.new(0),
            is_leaf: child.is_leaf
        }
        let mid: usize = 2
        let mut i: usize = mid
        while i < child.keys.len() {
            new_child.keys.push(child.keys.get(i).unwrap())
            new_child.values.push(child.values.get(i).unwrap())
            i = i + 1
        }
        while child.keys.len() > mid {
            child.keys.remove(mid)
            child.values.remove(mid)
        }
        if !child.is_leaf {
            i = mid + 1
            while i < child.children.len() {
                new_child.children.push(child.children.get(i).unwrap())
                i = i + 1
            }
            while child.children.len() > mid + 1 {
                child.children.remove(mid + 1)
            }
        }
        parent.keys.insert(idx, child.keys.get(mid - 1).unwrap())
        parent.values.insert(idx, child.values.get(mid - 1).unwrap())
        child.keys.remove(mid - 1)
        child.values.remove(mid - 1)
        parent.children.insert(idx + 1, new_child)
    }
    micro count_keys(self, node: BTreeNode<K, V>): usize {
        let mut count: usize = node.keys.len()
        if !node.is_leaf {
            let mut i: usize = 0
            while i < node.children.len() {
                count = count + self.count_keys(node.children.get(i).unwrap())
                i = i + 1
            }
        }
        return count
    }
    micro iterate_node(self, node: BTreeNode<K, V>, f: func(K, V): void) {
        let mut i: usize = 0
        while i < node.keys.len() {
            if !node.is_leaf {
                self.iterate_node(node.children.get(i).unwrap(), f)
            }
            f(node.keys.get(i).unwrap(), node.values.get(i).unwrap())
            i = i + 1
        }
        if !node.is_leaf {
            self.iterate_node(node.children.get(i).unwrap(), f)
        }
    }
}