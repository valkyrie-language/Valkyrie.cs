# std.text: csv — CSV 解析与生成

micro parse(text: utf8): List<List<utf8>> {
    let rows: List<List<utf8>> = List.new()
    let mut current: List<utf8> = List.new()
    let mut field: utf8 = ""
    let mut in_quotes: bool = false
    let mut i: i32 = 0
    let n: i32 = len(text)
    while i < n {
        let ch: utf8 = substring(text, i, 1)
        if ch == "\"" {
            in_quotes = !in_quotes
        } else if ch == "," and !in_quotes {
            List.push(current, field)
            field = ""
        } else if ch == "\n" and !in_quotes {
            List.push(current, field)
            field = ""
            List.push(rows, current)
            current = List.new()
        } else {
            field = concat(field, ch)
        }
        i = i + 1
    }
    if field != "" or List.len(current) > 0 {
        List.push(current, field)
        List.push(rows, current)
    }
    return rows
}

micro stringify(rows: List<List<utf8>>): utf8 {
    let mut result: utf8 = ""
    let mut i: i32 = 0
    while i < List.len(rows) {
        let row: List<utf8> = List.get(rows, i).unwrap()
        let mut j: i32 = 0
        while j < List.len(row) {
            let field: utf8 = List.get(row, j).unwrap()
            if contains(field, ",") or contains(field, "\"") or contains(field, "\n") {
                result = concat(result, "\"")
                result = concat(result, field)
                result = concat(result, "\"")
            } else {
                result = concat(result, field)
            }
            if j < List.len(row) - 1 {
                result = concat(result, ",")
            }
            j = j + 1
        }
        result = concat(result, "\n")
        i = i + 1
    }
    return result
}