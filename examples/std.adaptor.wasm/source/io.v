# std.web_sdk: 控制台 IO
# 封装 console / performance / timer API 为统一 print 接口
# 浏览器端 JS 自动换行，print 即为 println
# 编码：内部 utf8 → JS 端 utf16

#region 输出

micro print(msg: utf8) {
    let msg16: utf16 = utf8_to_utf16(msg)
    console_log(msg16)
}

micro print_line(msg: utf8) {
    print(msg)
}

micro print_err(msg: utf8) {
    let msg16: utf16 = utf8_to_utf16(msg)
    console_error(msg16)
}

#endregion

#region 输入

micro read_line(): utf8 {
    return ""
}

#endregion

#region 计时

micro time_ms(): i64 {
    return i64(perf_now())
}

micro sleep_ms(ms: i32) {
    set_timeout(0, ms)
}

#endregion
