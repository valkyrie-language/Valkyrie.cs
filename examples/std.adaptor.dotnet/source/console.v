# 控制台 API

[clr("System.Console", "Write")]
micro console_write(value: string): void

[clr("System.Console", "WriteLine")]
micro console_write_line(value: string): void

[clr("System.Console", "ReadLine")]
micro console_read_line(): string

[clr("System.Console", "ReadKey")]
micro console_read_key(): i32

[clr("System.Console", "Clear")]
micro console_clear(): void

[clr("System.Console", "set_ForegroundColor")]
micro console_set_fg(color: i32): void

[clr("System.Console", "set_BackgroundColor")]
micro console_set_bg(color: i32): void

[clr("System.Console", "ResetColor")]
micro console_reset_color(): void
