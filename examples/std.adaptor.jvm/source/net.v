# 网络 API

[jvm("java.net.URL", "openConnection")]
micro jvm_url_open_connection(url: string): i32

[jvm("java.net.HttpURLConnection", "getInputStream")]
micro jvm_http_get_input_stream(conn: i32): i32

[jvm("java.net.HttpURLConnection", "setRequestMethod")]
micro jvm_http_set_request_method(conn: i32, method: string): void

[jvm("java.net.HttpURLConnection", "setRequestProperty")]
micro jvm_http_set_request_property(conn: i32, key: string, value: string): void

[jvm("java.net.HttpURLConnection", "getResponseCode")]
micro jvm_http_get_response_code(conn: i32): i32
