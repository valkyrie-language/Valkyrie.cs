# std.types: Result<T, E>

[tag(ResultKind)]
union Result<T, E> {
    [tag(0)]
    Fine { value: T }
    [tag(1, default)]
    Fail { error: E }
}

imply Result<T, E> {
    micro unwrap(self): T {
        match self {
            case Fine { value }:
                value
            case Fail { .. }:
                T.default
        }
    }

    micro unwrap_err(self): E {
        match self {
            case Fine { .. }:
                E.default
            case Fail { error }:
                error
        }
    }

    micro unwrap_or(self, default: T): T {
        match self {
            case Fine { value }:
                value
            case Fail { .. }:
                default
        }
    }

    micro unwrap_or_else(self, f: micro(E) -> T): T {
        match self {
            case Fine { value }:
                value
            case Fail { error }:
                f(error)
        }
    }

    micro map<U>(self, f: micro(T) -> U): Result<U, E> {
        match self {
            case Fine { value }:
                Fine { value: f(value) }
            case Fail { error }:
                Fail { error: error }
        }
    }

    micro and_then<U>(self, f: micro(T) -> Result<U, E>): Result<U, E> {
        match self {
            case Fine { value }:
                f(value)
            case Fail { error }:
                Fail { error: error }
        }
    }

    micro fold<U>(self, fine: micro(T) -> U, fail: micro(E) -> U): U {
        match self {
            case Fine { value }:
                fine(value)
            case Fail { error }:
                fail(error)
        }
    }
}