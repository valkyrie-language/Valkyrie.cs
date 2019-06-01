# 字符串 API

[jvm("java.lang.String", "format"), pure]
micro jvm_string_format(template: string, arg0: string): string

[jvm("java.lang.String", "concat"), pure]
micro jvm_string_concat(a: string, b: string): string

[jvm("java.lang.String", "substring"), pure]
micro jvm_string_substring(s: string, begin: i32, end: i32): string

[jvm("java.lang.String", "indexOf"), pure]
micro jvm_string_index_of(s: string, value: string): i32

[jvm("java.lang.String", "replace"), pure]
micro jvm_string_replace(s: string, old_value: string, new_value: string): string

[jvm("java.lang.String", "toUpperCase"), pure]
micro jvm_string_to_upper(s: string): string

[jvm("java.lang.String", "toLowerCase"), pure]
micro jvm_string_to_lower(s: string): string

[jvm("java.lang.String", "split"), pure]
micro jvm_string_split(s: string, regex: string): i32

[jvm("java.lang.String", "trim"), pure]
micro jvm_string_trim(s: string): string

[jvm("java.lang.String", "length"), pure]
micro jvm_string_length(s: string): i32
