# VOA 副作用管理

import voa-core.types

struct EffectConfig {
    retry_count: int
    retry_delay_ms: int
    timeout_ms: int
    cache_ttl_ms: int
    dedupe: bool
}

struct EffectEntry {
    id: string
    fn_name: string
    args: list
    status: string
    result: any
    error: string
    created_at: i64
    updated_at: i64
}

micro effect(fn_name: string, args: list): EffectResult {
    return perform_effect(fn_name, args, default_effect_config())
}

micro effect_with_config(fn_name: string, args: list, config: EffectConfig): EffectResult {
    return perform_effect(fn_name, args, config)
}

micro perform_effect(fn_name: string, args: list, config: EffectConfig): EffectResult {
    let entry = EffectEntry {
        id: generate_effect_id()
        fn_name: fn_name
        args: args
        status: "pending"
        result: null
        error: ""
        created_at: current_time_ms()
        updated_at: current_time_ms()
    }

    return EffectResult {
        status: "pending"
        data: null
        error: ""
        entry_id: entry.id
    }
}

micro resolve_effect(result: any, entry_id: string): EffectResult {
    return EffectResult {
        status: "resolved"
        data: result
        error: ""
        entry_id: entry_id
    }
}

micro reject_effect(error: string, entry_id: string): EffectResult {
    return EffectResult {
        status: "rejected"
        data: null
        error: error
        entry_id: entry_id
    }
}

micro is_pending(result: EffectResult): bool {
    return result.status == "pending"
}

micro is_resolved(result: EffectResult): bool {
    return result.status == "resolved"
}

micro is_rejected(result: EffectResult): bool {
    return result.status == "rejected"
}

micro default_effect_config(): EffectConfig {
    return EffectConfig {
        retry_count: 3
        retry_delay_ms: 1000
        timeout_ms: 30000
        cache_ttl_ms: 0
        dedupe: true
    }
}

struct EffectResult {
    status: string
    data: any
    error: string
    entry_id: string
}

[js]
micro generate_effect_id(): string

[js]
micro current_time_ms(): i64
