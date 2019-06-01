# std.collections: Tuple — 元组类型

union Tuple {
    Tuple2 { a: Any, b: Any }
    Tuple3 { a: Any, b: Any, c: Any }
    Tuple4 { a: Any, b: Any, c: Any, d: Any }
    Tuple5 { a: Any, b: Any, c: Any, d: Any, e: Any }
}

imply Tuple {
    micro tuple2(a: Any, b: Any): Self {
        return Tuple2 { a: a, b: b }
    }
    micro tuple3(a: Any, b: Any, c: Any): Self {
        return Tuple3 { a: a, b: b, c: c }
    }
    micro tuple4(a: Any, b: Any, c: Any, d: Any): Self {
        return Tuple4 { a: a, b: b, c: c, d: d }
    }
    micro tuple5(a: Any, b: Any, c: Any, d: Any, e: Any): Self {
        return Tuple5 { a: a, b: b, c: c, d: d, e: e }
    }
    micro get(self, index: usize): Any {
        <% Match self %>
            <% case Tuple2(a, b) %>
                if index == 0 { return a }
                if index == 1 { return b }
                return nil
            <% case Tuple3(a, b, c) %>
                if index == 0 { return a }
                if index == 1 { return b }
                if index == 2 { return c }
                return nil
            <% case Tuple4(a, b, c, d) %>
                if index == 0 { return a }
                if index == 1 { return b }
                if index == 2 { return c }
                if index == 3 { return d }
                return nil
            <% case Tuple5(a, b, c, d, e) %>
                if index == 0 { return a }
                if index == 1 { return b }
                if index == 2 { return c }
                if index == 3 { return d }
                if index == 4 { return e }
                return nil
        <% end match %>
    }
    micro len(self): usize {
        <% Match self %>
            <% case Tuple2(_, _) %> return 2
            <% case Tuple3(_, _, _) %> return 3
            <% case Tuple4(_, _, _, _) %> return 4
            <% case Tuple5(_, _, _, _, _) %> return 5
        <% end match %>
    }
}