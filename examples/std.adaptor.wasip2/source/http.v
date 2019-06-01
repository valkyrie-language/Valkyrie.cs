# WASI Preview 2 — HTTP 客户端
# 对标 wasi:http/outgoing-handler + wasi:http/types
# WASI HTTP 是 p2 新增的核心能力（p1 无 HTTP 支持）

[wasi_p2("wasi:http/types", "new-fields")]
extern wasi_p2_http_fields_new(): i32

[wasi_p2("wasi:http/types", "fields-set")]
extern wasi_p2_http_fields_set(fields: i32, name: utf8, value: [u8]): void

[wasi_p2("wasi:http/types", "fields-get")]
extern wasi_p2_http_fields_get(fields: i32, name: utf8): [[u8]]

[wasi_p2("wasi:http/types", "fields-delete")]
extern wasi_p2_http_fields_delete(fields: i32, name: utf8): void

[wasi_p2("wasi:http/types", "fields-entries")]
extern wasi_p2_http_fields_entries(fields: i32): [(utf8, [u8])]

[wasi_p2("wasi:http/types", "fields-drop")]
extern wasi_p2_http_fields_drop(fields: i32): void

[wasi_p2("wasi:http/types", "new-incoming-request")]
extern wasi_p2_http_incoming_request_new(method: i32, path: utf8, headers: i32): i32

[wasi_p2("wasi:http/types", "incoming-request-method")]
extern wasi_p2_http_incoming_request_method(request: i32): i32

[wasi_p2("wasi:http/types", "incoming-request-path")]
extern wasi_p2_http_incoming_request_path(request: i32): (utf8, i32)

[wasi_p2("wasi:http/types", "incoming-request-headers")]
extern wasi_p2_http_incoming_request_headers(request: i32): i32

[wasi_p2("wasi:http/types", "incoming-request-auth")]
extern wasi_p2_http_incoming_request_auth(request: i32): (utf8, i32)

[wasi_p2("wasi:http/types", "incoming-request-consume")]
extern wasi_p2_http_incoming_request_consume(request: i32): (i32, i32)

[wasi_p2("wasi:http/types", "incoming-request-drop")]
extern wasi_p2_http_incoming_request_drop(request: i32): void

[wasi_p2("wasi:http/types", "new-response")]
extern wasi_p2_http_response_new(status: i32, headers: i32): i32

[wasi_p2("wasi:http/types", "response-status")]
extern wasi_p2_http_response_status(response: i32): i32

[wasi_p2("wasi:http/types", "response-headers")]
extern wasi_p2_http_response_headers(response: i32): i32

[wasi_p2("wasi:http/types", "response-consume")]
extern wasi_p2_http_response_consume(response: i32): (i32, i32)

[wasi_p2("wasi:http/types", "response-drop")]
extern wasi_p2_http_response_drop(response: i32): void

[wasi_p2("wasi:http/types", "new-outgoing-request")]
extern wasi_p2_http_outgoing_request_new(method: i32, path: utf8, headers: i32): i32

[wasi_p2("wasi:http/types", "outgoing-request-write")]
extern wasi_p2_http_outgoing_request_write(request: i32): (i32, i32)

[wasi_p2("wasi:http/types", "outgoing-request-drop")]
extern wasi_p2_http_outgoing_request_drop(request: i32): void

[wasi_p2("wasi:http/types", "outgoing-request-body")]
extern wasi_p2_http_outgoing_request_body(request: i32): i32

[wasi_p2("wasi:http/types", "outgoing-response-write")]
extern wasi_p2_http_outgoing_response_write(response: i32): (i32, i32)

[wasi_p2("wasi:http/types", "outgoing-response-drop")]
extern wasi_p2_http_outgoing_response_drop(response: i32): void

[wasi_p2("wasi:http/outgoing-handler", "handle")]
extern wasi_p2_http_outgoing_handle(request: i32): (i32, i32)

[wasi_p2("wasi:http/types", "http-error-code")]
extern wasi_p2_http_error_code(err: i32): i32

[wasi_p2("wasi:http/types", "http-method-get")]
extern wasi_p2_http_method_get(): i32

[wasi_p2("wasi:http/types", "http-method-post")]
extern wasi_p2_http_method_post(): i32

[wasi_p2("wasi:http/types", "http-method-put")]
extern wasi_p2_http_method_put(): i32

[wasi_p2("wasi:http/types", "http-method-delete")]
extern wasi_p2_http_method_delete(): i32
