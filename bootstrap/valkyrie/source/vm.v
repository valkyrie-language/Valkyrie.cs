# valkyrie-bootstrap: NyarVM 顶层
# 用 Valkyrie 语言实现的 NyarVM — 自举的核心
#
# 自举原理:
#   1. C# 实现的 NyarVM 可以执行 Valkyrie 编译后的字节码
#   2. Valkyrie 语言可以实现 NyarVM 的指令集解释器
#   3. 这个 Valkyrie 实现的 NyarVM 可以运行 Valkyrie 程序
#   4. 任何语言只需实现 NyarVM，就能运行 Valkyrie 程序
#   5. Valkyrie 因此可以在任何地方运行 — 自举完成
#
# NyarVM 状态:
#   - 模块表 (已加载的模块)
#   - 帧栈 (调用栈)
#   - 值栈 (操作数栈)
#   - 堆内存
#   - 程序计数器
#
# VM 内部字符串表示为 UTF-8 编码

micro vm_max_modules(): i32 { return 256 }
micro vm_max_frames(): i32 { return 512 }
micro vm_max_stack(): i32 { return 1024 }

micro vm_create(): i32 {
    return 0
}

micro vm_stack(vm: i32): i32 {
    return 0
}

micro vm_heap(vm: i32): i32 {
    return 0
}

micro vm_frame_count(vm: i32): i32 {
    return 0
}

micro vm_push_frame(vm: i32, frame: i32): i32 {
    return 0
}

micro vm_pop_frame(vm: i32): i32 {
    return 0
}

micro vm_current_frame(vm: i32): i32 {
    return 0
}

micro vm_load_module(vm: i32, module: i32): i32 {
    return 0
}

micro vm_find_module(vm: i32, name: utf8): i32 {
    return -1
}

micro vm_find_function(vm: i32, module_name: utf8, func_name: utf8): i32 {
    let module: i32 = vm_find_module(vm, module_name)
    if module < 0 {
        return -1
    }
    return module_find_function(module, func_name)
}

micro vm_run_function(vm: i32, module_name: utf8, func_name: utf8): i64 {
    let func: i32 = vm_find_function(vm, module_name, func_name)
    if func < 0 {
        return 0
    }

    let frame: i32 = frame_create(func, 0, 0)
    vm_push_frame(vm, frame)

    let stack: i32 = vm_stack(vm)
    let module: i32 = vm_find_module(vm, module_name)
    let mut result: i64 = 0
    let mut running: bool = true

    while running {
        let current: i32 = vm_current_frame(vm)
        let pc: i32 = frame_pc(current)
        let op: i32 = module_read_u8(module, pc)

        if op == op_return() {
            result = stack_peek(stack)
            vm_pop_frame(vm)
            if vm_frame_count(vm) == 0 {
                running = false
            }
        } else {
            if opcode_is_i32_arith(op) {
                executor_dispatch_i32_arith(stack, op)
                frame_set_pc(current, pc + 1)
            } else {
                if opcode_is_i32_cmp(op) {
                    executor_dispatch_i32_cmp(stack, op)
                    frame_set_pc(current, pc + 1)
                } else {
                    if op == op_const() {
                        let const_index: i32 = module_read_i32(module, pc + 1)
                        let value: i64 = const_pool_get_int(module_constant_pool(module), const_index)
                        stack_push(stack, value)
                        frame_set_pc(current, pc + 5)
                    } else {
                        if op == op_load_local() {
                            let index: i32 = module_read_i32(module, pc + 1)
                            executor_step_load_local(stack, current, index)
                            frame_set_pc(current, pc + 5)
                        } else {
                            if op == op_store_local() {
                                let index: i32 = module_read_i32(module, pc + 1)
                                executor_step_store_local(stack, current, index)
                                frame_set_pc(current, pc + 5)
                            } else {
                                if op == op_nop() {
                                    frame_set_pc(current, pc + 1)
                                } else {
                                    frame_set_pc(current, pc + opcode_size(op))
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    return result
}

micro bootstrap_add(a: i32, b: i32): i32 {
    return a + b
}

micro bootstrap_sub(a: i32, b: i32): i32 {
    return a - b
}

micro bootstrap_mul(a: i32, b: i32): i32 {
    return a * b
}

micro bootstrap_factorial(n: i32): i32 {
    if n <= 1 {
        return 1
    } else {
        return n * bootstrap_factorial(n - 1)
    }
}

micro bootstrap_fibonacci(n: i32): i32 {
    if n <= 0 {
        return 0
    } else {
        if n == 1 {
            return 1
        } else {
            return bootstrap_fibonacci(n - 1) + bootstrap_fibonacci(n - 2)
        }
    }
}

micro bootstrap_main(): i32 {
    let a: i32 = bootstrap_add(10, 20)
    let b: i32 = bootstrap_sub(100, 30)
    let c: i32 = bootstrap_mul(6, 7)
    let fact10: i32 = bootstrap_factorial(10)
    let fib10: i32 = bootstrap_fibonacci(10)
    return a + b + c
}
