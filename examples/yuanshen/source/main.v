# YuanShen — 版本管理工具前端
# 所有创意逻辑 100% GGScript，AWSL 纯渲染
# 后端对接：Olymp.Atlas

struct Repo {
    id: string
    name: string
    description: string
    active: bool
}

struct FileEntry {
    name: string
    isDir: bool
    size: string
    modifiedAt: string
}

struct Branch {
    name: string
    isRemote: bool
    isCurrent: bool
}

struct CommitVersion {
    id: string
    hash: string
    message: string
    author: string
    date: string
    isCurrent: bool
}

struct DiffFile {
    path: string
    status: string
    additions: i32
    deletions: i32
}

let repos: list = [
    Repo { id: "1", name: "NyarVM",      description: "编译器基础设施", active: true  },
    Repo { id: "2", name: "Gnosis",      description: "游戏引擎",       active: false },
    Repo { id: "3", name: "Valkyrie",    description: "语言前端",       active: false }
]

let selected_repo_id: string = "1"

let path_segments: list = ["NyarVM", "src", "core"]

let files: list = [
    FileEntry { name: "..",         isDir: true,  size: "",        modifiedAt: ""            },
    FileEntry { name: "Assembler",  isDir: true,  size: "",        modifiedAt: "2025-04-20"  },
    FileEntry { name: "IR",         isDir: true,  size: "",        modifiedAt: "2025-04-22"  },
    FileEntry { name: "Types",      isDir: true,  size: "",        modifiedAt: "2025-04-18"  },
    FileEntry { name: "VM",         isDir: true,  size: "",        modifiedAt: "2025-04-25"  },
    FileEntry { name: "NyarVM.cs",  isDir: false, size: "12.4 KB", modifiedAt: "2025-04-25"  },
    FileEntry { name: "NyarModule.cs", isDir: false, size: "8.2 KB", modifiedAt: "2025-04-23"  },
    FileEntry { name: "Executor.cs",isDir: false, size: "24.1 KB", modifiedAt: "2025-04-25"  },
    FileEntry { name: "Frame.cs",   isDir: false, size: "6.7 KB",  modifiedAt: "2025-04-21"  }
]

let branches: list = [
    Branch { name: "main",        isRemote: false, isCurrent: true  },
    Branch { name: "develop",     isRemote: false, isCurrent: false },
    Branch { name: "feat/voa-m1", isRemote: false, isCurrent: false },
    Branch { name: "origin/main", isRemote: true,  isCurrent: false },
    Branch { name: "origin/dev",  isRemote: true,  isCurrent: false }
]

let current_branch: string = "main"
let branch_dropdown_open: bool = false

let commits: list = [
    CommitVersion { id: "1", hash: "a1b2c3d", message: "feat: 添加 SSR 渲染器",       author: "dev1", date: "2025-04-25", isCurrent: true  },
    CommitVersion { id: "2", hash: "e4f5g6h", message: "fix: 修复 WasmTargetBuilder",  author: "dev2", date: "2025-04-24", isCurrent: false },
    CommitVersion { id: "3", hash: "i7j8k9l", message: "refactor: 重构表达式求值",     author: "dev1", date: "2025-04-23", isCurrent: false },
    CommitVersion { id: "4", hash: "m0n1o2p", message: "chore: 更新依赖",              author: "dev3", date: "2025-04-22", isCurrent: false },
    CommitVersion { id: "5", hash: "q3r4s5t", message: "docs: 补充 Islands 文档",      author: "dev2", date: "2025-04-21", isCurrent: false }
]

let selected_commit_id: string = "1"

let diffs: list = [
    DiffFile { path: "Compiler/AwslSsrRenderer.cs",   status: "M", additions: 156, deletions: 42  },
    DiffFile { path: "DevServer/VoaDevServer.cs",     status: "M", additions: 38,  deletions: 12  },
    DiffFile { path: "Compiler/WasmTargetBuilder.cs", status: "A", additions: 245, deletions: 0   },
    DiffFile { path: "Commands/BuildCommand.cs",      status: "D", additions: 0,   deletions: 84  },
    DiffFile { path: "runtime/voa-runtime.js",        status: "R", additions: 52,  deletions: 31  }
]

let search_query: string = ""
let active_tab: string = "files"

let version_count: i32 = 5

micro select_repo(id: string): void {
    loop repo in repos {
        repo.active = repo.id == id
    }

    selected_repo_id = id

    let repo_name = ""
    loop repo in repos {
        if (repo.id == id) {
            repo_name = repo.name
        }
    }

    path_segments = [repo_name]
}

micro add_repo(): void {
    let new_id = string(length(repos) + 1)
    repos = [...repos, Repo {
        id: new_id,
        name: "新仓库",
        description: "",
        active: false
    }]
}

micro navigate_to(index: i32): void {
    if (index < length(path_segments)) {
        path_segments = slice_list(path_segments, 0, index + 1)
    }
}

micro open_file(file: FileEntry): void {
    if (file.isDir) {
        path_segments = [...path_segments, file.name]
        load_files()
    }
}

micro upload_file(): void {}

micro new_folder(): void {}

micro toggle_dropdown(): void {
    branch_dropdown_open = !branch_dropdown_open
}

micro switch_branch(name: string): void {
    current_branch = name
    branch_dropdown_open = false

    loop branch in branches {
        branch.isCurrent = branch.name == name
    }
}

micro checkout_version(id: string): void {
    selected_commit_id = id

    loop commit in commits {
        commit.isCurrent = commit.id == id
    }
}

micro on_search(query: string): void {
    search_query = query
}

micro clear_search(): void {
    search_query = ""
}

micro switch_tab(tab: string): void {
    active_tab = tab
}

micro load_files(): void {}

micro get_version_count(): i32 {
    return length(commits)
}

micro init(): void {
    branch_dropdown_open = false
    search_query = ""
    active_tab = "files"
}

micro main(): i32 {
    init()
    return 0
}

micro length(lst: list): i32 { return 0 }
micro string(v: i32): string { return "" }
micro slice_list(lst: list, start: i32, end: i32): list { return lst }
