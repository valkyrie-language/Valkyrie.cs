# std.adaptor.macos: 控制台 IO
# 封装 Foundation NSLog 为统一 print 接口
# NSLog 自动追加换行，并使用 UTF-8 编码，无需额外转换
# 编码：内部 utf8 → macOS Foundation utf8（直接传递）

micro print(msg: utf8) {
    darwin_ns_log(msg)
}

micro print_line(msg: utf8) {
    darwin_ns_log(msg)
}

micro print_err(msg: utf8) {
    darwin_ns_log(msg)
}

micro read_line(): utf8 {
    return ""
}

micro time_ms(): i64 {
    return 0
}

micro sleep_ms(ms: i32) {
    return
}
