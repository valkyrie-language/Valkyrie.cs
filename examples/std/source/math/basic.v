# std.math: basic — 基础数学函数

micro abs(value: f64): f64 {
    <% match arch %>
        <% case "clr" %>
        return std.adaptor.dotnet.math.math_abs_f64(value)
        <% case "jvm" %>
        return std.adaptor.jvm.math.jvm_math_abs_f64(value)
        <% case "wasm32" %>
        return std.adaptor.wasm.math.math_abs(value)
        <% else %>
        if value < 0.0 { return -value }
        return value
    <% end match %>
}

micro max(a: f64, b: f64): f64 {
    <% match arch %>
        <% case "wasm32" %>
        return std.adaptor.wasm.math.math_max(a, b)
        <% else %>
        if a > b { return a }
        return b
    <% end match %>
}

micro min(a: f64, b: f64): f64 {
    <% match arch %>
        <% case "wasm32" %>
        return std.adaptor.wasm.math.math_min(a, b)
        <% else %>
        if a < b { return a }
        return b
    <% end match %>
}

micro sqrt(value: f64): f64 {
    <% match arch %>
        <% case "clr" %>
        return std.adaptor.dotnet.math.math_sqrt(value)
        <% case "jvm" %>
        return std.adaptor.jvm.math.jvm_math_sqrt(value)
        <% case "wasm32" %>
        return std.adaptor.wasm.math.math_sqrt(value)
        <% else %>
        return 0.0
    <% end match %>
}

micro pow(base: f64, exp: f64): f64 {
    <% match arch %>
        <% case "clr" %>
        return std.adaptor.dotnet.math.math_pow(base, exp)
        <% case "jvm" %>
        return std.adaptor.jvm.math.jvm_math_pow(base, exp)
        <% case "wasm32" %>
        return std.adaptor.wasm.math.math_pow(base, exp)
        <% else %>
        return 0.0
    <% end match %>
}

micro floor(value: f64): f64 {
    <% match arch %>
        <% case "clr" %>
        return std.adaptor.dotnet.math.math_floor(value)
        <% case "jvm" %>
        return std.adaptor.jvm.math.jvm_math_floor(value)
        <% case "wasm32" %>
        return f64(std.adaptor.wasm.math.math_floor(value))
        <% else %>
        return 0.0
    <% end match %>
}

micro ceil(value: f64): f64 {
    <% match arch %>
        <% case "clr" %>
        return std.adaptor.dotnet.math.math_ceiling(value)
        <% case "jvm" %>
        return std.adaptor.jvm.math.jvm_math_ceil(value)
        <% case "wasm32" %>
        return f64(std.adaptor.wasm.math.math_ceil(value))
        <% else %>
        return 0.0
    <% end match %>
}

micro round(value: f64): f64 {
    <% match arch %>
        <% case "clr" %>
        return std.adaptor.dotnet.math.math_round(value)
        <% case "jvm" %>
        return f64(std.adaptor.jvm.math.jvm_math_round(value))
        <% case "wasm32" %>
        return f64(std.adaptor.wasm.math.math_round(value))
        <% else %>
        return 0.0
    <% end match %>
}

micro sin(value: f64): f64 {
    <% match arch %>
        <% case "clr" %>
        return std.adaptor.dotnet.math.math_sin(value)
        <% case "jvm" %>
        return std.adaptor.jvm.math.jvm_math_sin(value)
        <% case "wasm32" %>
        return std.adaptor.wasm.math.math_sin(value)
        <% else %>
        return 0.0
    <% end match %>
}

micro cos(value: f64): f64 {
    <% match arch %>
        <% case "clr" %>
        return std.adaptor.dotnet.math.math_cos(value)
        <% case "jvm" %>
        return std.adaptor.jvm.math.jvm_math_cos(value)
        <% case "wasm32" %>
        return std.adaptor.wasm.math.math_cos(value)
        <% else %>
        return 0.0
    <% end match %>
}

micro tan(value: f64): f64 {
    <% match arch %>
        <% case "wasm32" %>
        return std.adaptor.wasm.math.math_tan(value)
        <% else %>
        return 0.0
    <% end match %>
}

micro log(value: f64): f64 {
    <% match arch %>
        <% case "clr" %>
        return std.adaptor.dotnet.math.math_log(value)
        <% case "wasm32" %>
        return std.adaptor.wasm.math.math_log(value)
        <% else %>
        return 0.0
    <% end match %>
}

micro log2(value: f64): f64 {
    <% match arch %>
        <% case "wasm32" %>
        return std.adaptor.wasm.math.math_log2(value)
        <% else %>
        return 0.0
    <% end match %>
}

micro log10(value: f64): f64 {
    <% match arch %>
        <% case "wasm32" %>
        return std.adaptor.wasm.math.math_log10(value)
        <% else %>
        return 0.0
    <% end match %>
}

micro clamp(value: f64, lo: f64, hi: f64): f64 {
    if value < lo { return lo }
    if value > hi { return hi }
    return value
}

micro lerp(a: f64, b: f64, t: f64): f64 {
    return a + (b - a) * t
}