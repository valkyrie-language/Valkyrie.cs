# 控制台 API

[jvm("java.lang.System", "out.println")]
micro jvm_println(value: string): void

[jvm("java.lang.System", "out.print")]
micro jvm_print(value: string): void

[jvm("java.lang.System", "err.println")]
micro jvm_err_println(value: string): void

[jvm("java.lang.System", "in.read")]
micro jvm_read(): i32

[jvm("java.io.BufferedReader", "readLine")]
micro jvm_read_line(reader: i32): string
