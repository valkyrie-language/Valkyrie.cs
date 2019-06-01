# 文件系统 API

[jvm("java.io.File", "exists")]
micro jvm_file_exists(path: string): bool

[jvm("java.io.File", "createNewFile")]
micro jvm_file_create(path: string): bool

[jvm("java.io.File", "delete")]
micro jvm_file_delete(path: string): bool

[jvm("java.io.File", "mkdirs")]
micro jvm_mkdirs(path: string): bool

[jvm("java.io.File", "isDirectory")]
micro jvm_is_directory(path: string): bool

[jvm("java.io.File", "isFile")]
micro jvm_is_file(path: string): bool

[jvm("java.io.File", "length")]
micro jvm_file_length(path: string): i64

[jvm("java.io.File", "list")]
micro jvm_file_list(path: string): i32

[jvm("java.io.File", "renameTo")]
micro jvm_file_rename_to(old_path: string, new_path: string): bool
