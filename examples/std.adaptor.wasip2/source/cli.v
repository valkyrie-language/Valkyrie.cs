# WASI Preview 2 — CLI 命令与环境
# 对标 wasi:cli/environment + wasi:cli/exit + wasi:cli/stdin/stdout/stderr

[wasi_p2("wasi:cli/environment", "get-environment")]
extern wasi_p2_cli_get_environment(): [([utf8, utf8], i32)]

[wasi_p2("wasi:cli/environment", "get-arguments")]
extern wasi_p2_cli_get_arguments(): [utf8]

[wasi_p2("wasi:cli/environment", "initial-cwd")]
extern wasi_p2_cli_initial_cwd(): (utf8, i32)

[wasi_p2("wasi:cli/exit", "exit")]
extern wasi_p2_cli_exit(status: i32): void

[wasi_p2("wasi:cli/stdin", "get-stdin")]
extern wasi_p2_cli_get_stdin(): i32

[wasi_p2("wasi:cli/stdout", "get-stdout")]
extern wasi_p2_cli_get_stdout(): i32

[wasi_p2("wasi:cli/stderr", "get-stderr")]
extern wasi_p2_cli_get_stderr(): i32

[wasi_p2("wasi:cli/terminal-stdin", "if-terminal-input")]
extern wasi_p2_cli_is_terminal_stdin(): bool

[wasi_p2("wasi:cli/terminal-stdout", "if-terminal-output")]
extern wasi_p2_cli_is_terminal_stdout(): bool

[wasi_p2("wasi:cli/terminal-stderr", "if-terminal-error")]
extern wasi_p2_cli_is_terminal_stderr(): bool
