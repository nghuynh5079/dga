# 🔬 DevGitAtom — Tiến độ & Kế hoạch

> **Cập nhật lần cuối:** 2026-09-06

---

## 📁 Cấu trúc Project

```
D:\dga\
├── DevGitAtom.Core/        ← Business logic, atoms, engine
├── DevGitAtom.WPF/         ← UI layer (WPF + WebView2 terminal)
├── docs/                   ← Tài liệu dự án
│   ├── progress.md         ← File này
│   └── dev-data/           ← Dữ liệu phát triển nội bộ (gitignored)
└── dga.slnx
```

---

## ✅ Đã Hoàn Thành

### Core Infrastructure
- [x] `IAtom` interface — `Id`, `DisplayName`, `Category`, `Mutating`, `Requires[]`, `Provides[]`, `ExecuteAsync`, `UndoAsync`
- [x] `AtomRegistry` — Register & lookup atoms by ID
- [x] `AtomInvoker` — Run single atom + `RunChainAsync` (dừng khi Failed)
- [x] `WorkflowContext` — Shared state giữa các atoms (`OriginalBranch`, `BaseBranch`, `StashRef`, `Data` dict)
- [x] `GitRunner` — Wrapper chạy `git` subprocess, capture stdout/stderr
- [x] `AppConfig` — `WorkingDir`

### Atoms đã có (5 atoms)
| Atom | Category | Mutating | Ghi chú |
|------|----------|----------|---------|
| `PreconditionAtom` | Safety | ○ | Check git repo, lưu OriginalBranch |
| `FetchAtom` | Sync | ○ | `git fetch origin` |
| `PullAtom` | Sync | ● | `git pull` |
| `StashAtom` | Worktree | ● | Stash thông minh, skip nếu working tree sạch |
| `StatusAtom` | Info | ○ | `git status` |

### WPF UI
- [x] 3-panel layout: Atom list / Chain builder / Terminal
- [x] Drag & drop reorder chain
- [x] xterm.js terminal (WebView2) với ANSI colors
- [x] Branch + dirty indicator tự động refresh
- [x] Browse thư mục Git repo

---

## 🔄 Còn Thiếu

### 🔴 High Priority
- [ ] **Preset Save/Load** — Lưu chain ra JSON (`%AppData%\DevGitAtom\presets\`)
- [ ] **Atom config/params UI** — Input tham số cho atoms (branch name, commit msg...)
- [ ] **Undo/Rollback UI** — Trigger `UndoAsync` khi có lỗi

### 🟡 Medium Priority
- [ ] Thêm atoms: `CheckoutAtom`, `CommitAtom`, `PushAtom`, `RebaseAtom`, `MergeAtom`, `TagAtom`
- [ ] Requires/Provides dependency validation trong AtomInvoker
- [ ] Chain validation UI (warning khi thiếu precondition)

### 🟢 Nice to Have
- [ ] Atom search/filter sidebar
- [ ] Chain run history (timestamp + kết quả)
- [ ] Multi-repo support (tabs)
- [ ] Keyboard shortcuts (Ctrl+R run, Ctrl+L clear)

---

## 🏗️ Kiến Trúc — Data Flow

```
Atom List → click → _chain List<string>
_chain → BtnRun → AtomInvoker.RunChainAsync()
AtomInvoker → IAtom.ExecuteAsync(WorkflowContext, IProgress<string>)
IAtom → GitRunner → git subprocess
IAtom → IProgress → xterm.js Terminal
IAtom → WorkflowContext → Shared State (StashRef, OriginalBranch...)
```
