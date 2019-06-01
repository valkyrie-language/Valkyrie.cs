# std.collections: sort — 排序算法

micro quick_sort(list: List<T>, cmp: func(T, T): i32) {
    let len: usize = list.len()
    if len == 0 {
        return
    }
    quick_sort_range(list, 0, len - 1, cmp)
}

micro quick_sort_range(list: List<T>, lo: usize, hi: usize, cmp: func(T, T): i32) {
    if lo < hi {
        let p: usize = partition(list, lo, hi, cmp)
        if p > 0 {
            quick_sort_range(list, lo, p - 1, cmp)
        }
        quick_sort_range(list, p + 1, hi, cmp)
    }
}

micro partition(list: List<T>, lo: usize, hi: usize, cmp: func(T, T): i32): usize {
    let pivot: T = list.get(hi).unwrap()
    let mut i: usize = lo
    let mut j: usize = lo
    while j < hi {
        if cmp(list.get(j).unwrap(), pivot) <= 0 {
            swap(list, i, j)
            i = i + 1
        }
        j = j + 1
    }
    swap(list, i, hi)
    return i
}

micro swap(list: List<T>, a: usize, b: usize) {
    let tmp: T = list.get(a).unwrap()
    let b_val: T = list.get(b).unwrap()
    list.set(a, b_val)
    list.set(b, tmp)
}

micro merge_sort(list: List<T>, cmp: func(T, T): i32) {
    let len: usize = list.len()
    if len == 0 {
        return
    }
    merge_sort_range(list, 0, len - 1, cmp)
}

micro merge_sort_range(list: List<T>, left: usize, right: usize, cmp: func(T, T): i32) {
    if left < right {
        let mid: usize = left + (right - left) / 2
        merge_sort_range(list, left, mid, cmp)
        merge_sort_range(list, mid + 1, right, cmp)
        merge(list, left, mid, right, cmp)
    }
}

micro merge(list: List<T>, left: usize, mid: usize, right: usize, cmp: func(T, T): i32) {
    let mut left_arr: List<T> = ArrayList.new(0)
    let mut right_arr: List<T> = ArrayList.new(0)
    let mut i: usize = left
    while i <= mid {
        left_arr.push(list.get(i).unwrap())
        i = i + 1
    }
    i = mid + 1
    while i <= right {
        right_arr.push(list.get(i).unwrap())
        i = i + 1
    }
    let mut a: usize = 0
    let mut b: usize = 0
    let mut k: usize = left
    while a < left_arr.len() && b < right_arr.len() {
        if cmp(left_arr.get(a).unwrap(), right_arr.get(b).unwrap()) <= 0 {
            list.set(k, left_arr.get(a).unwrap())
            a = a + 1
        } else {
            list.set(k, right_arr.get(b).unwrap())
            b = b + 1
        }
        k = k + 1
    }
    while a < left_arr.len() {
        list.set(k, left_arr.get(a).unwrap())
        a = a + 1
        k = k + 1
    }
    while b < right_arr.len() {
        list.set(k, right_arr.get(b).unwrap())
        b = b + 1
        k = k + 1
    }
}