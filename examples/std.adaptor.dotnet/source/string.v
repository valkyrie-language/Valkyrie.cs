# 字符串 API

[clr("System.String", "Format"), pure]
micro string_format(template: string, arg0: string): string

[clr("System.String", "Concat"), pure]
micro string_concat(a: string, b: string): string

[clr("System.String", "Substring"), pure]
micro string_substring(s: string, start: i32, len: i32): string

[clr("System.String", "IndexOf"), pure]
micro string_index_of(s: string, value: string): i32

[clr("System.String", "Replace"), pure]
micro string_replace(s: string, old_value: string, new_value: string): string

[clr("System.String", "ToUpper"), pure]
micro string_to_upper(s: string): string

[clr("System.String", "ToLower"), pure]
micro string_to_lower(s: string): string

[clr("System.String", "Split"), pure]
micro string_split(s: string, separator: string): i32

[clr("System.String", "Trim"), pure]
micro string_trim(s: string): string

[clr("System.String", "get_Length"), pure]
micro string_length(s: string): i32
