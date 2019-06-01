# 文件系统 API

[clr("System.IO.File", "ReadAllText")]
micro file_read_all_text(path: string): string

[clr("System.IO.File", "WriteAllText")]
micro file_write_all_text(path: string, content: string): void

[clr("System.IO.File", "Exists")]
micro file_exists(path: string): bool

[clr("System.IO.File", "Delete")]
micro file_delete(path: string): void

[clr("System.IO.File", "AppendAllText")]
micro file_append_all_text(path: string, content: string): void

[clr("System.IO.Directory", "Exists")]
micro directory_exists(path: string): bool

[clr("System.IO.Directory", "CreateDirectory")]
micro directory_create(path: string): void

[clr("System.IO.Directory", "Delete")]
micro directory_delete(path: string, recursive: bool): void

[clr("System.IO.Directory", "GetFiles")]
micro directory_get_files(path: string): i32

[clr("System.IO.Path", "Combine")]
micro path_combine(path1: string, path2: string): string

[clr("System.IO.Path", "GetExtension")]
micro path_get_extension(path: string): string

[clr("System.IO.Path", "GetFileName")]
micro path_get_file_name(path: string): string
