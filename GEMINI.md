# GEMINI.md - Project Context for `go-regdiff`

## Project Overview
`go-regdiff` is a command-line tool written in Go for managing and comparing Windows Registry files (.REG) and the live Windows Registry. It is a version 5.0 rewrite of the original C# `regdiff` tool, providing cross-platform support for .REG file operations while maintaining native Windows registry access for live operations.

### Key Features
- **Comparison & Diffing:** Compare two .REG files, a .REG file against the live registry, or two registry keys.
- **Merging:** Create merged .REG files from multiple sources.
- **Exporting:** Export live registry keys directly to .REG files (ANSI or Unicode).
- **Writing:** Apply .REG files or diffs directly to the live Windows Registry (Windows only).
- **Variable Substitution:** Support for `$$VAR$$` syntax in .REG files with parameter files (.INI) or environment variables.
- **Key Aliasing:** Compare keys that have been renamed or relocated.

## Architecture
- **`cmd/regdiff/`**: CLI entry point (`main.go`). Handles CLI argument parsing (supporting both `/FLAG` and `-flag` styles) and orchestrates operations.
- **`diff/`**: Core comparison engine.
    - `regdiff.go`: `RegDiff` orchestrator for tree traversal and mismatch identification.
    - `params.go`: Handles `$$VAR$$` substitution and parameter file parsing.
    - `mismatch.go`: Defines mismatch categories (MissingKey, DataMismatch, etc.).
- **`internal/registry/`**: Registry abstraction layer.
    - `registry.go`: Live registry reading (Windows-only, `//go:build windows`).
    - `writer.go`: Low-level registry writing using syscalls (Windows-only).
    - `ops.go`: Higher-level registry operations like `LoadLiveRegistry` and `WriteToRegistry`.
    - `stub.go`: No-op stubs for non-Windows platforms (`//go:build !windows`).
- **External Dependency:** `github.com/gersonkurz/go-regis3`. This library handles all .REG file parsing, `KeyEntry`/`ValueEntry` structures, and .REG writing. It is typically referenced via a local `replace` in `go.mod`.

## Building and Running
The project uses `just` as a command runner.

### Key Commands
- **Build:** `just build` (current platform) or `just build-all` (cross-platform).
- **Test:** `just test` (runs Go unit tests) or `go test ./...`.
- **Check:** `just check` (runs fmt, vet, and tests).
- **Format:** `just fmt` (runs `gofmt -w .`).

## Development Conventions
- **Standard Go Style:** Follows standard Go structure; `gofmt` is enforced.
- **Case Insensitivity:** Registry keys and values are treated case-insensitively, matching Windows behavior.
- **Cross-Platform Safety:** Core logic in `diff/` is platform-independent. Registry-specific code is isolated using build tags.
- **Testing:** Tests reside in `diff/` (e.g., `diff_test.go`). Run with `just test`.
- **CLI Style:** Supports Windows-style `/FLAG` and POSIX-style `-flag`.

## Key Files & Documentation
- `cmd/regdiff/main.go`: Main orchestration logic.
- `AGENTS.md`: Detailed repository guidelines and coding standards.
- `CLAUDE.md`: Quick reference for build/test commands and architecture.
- `docs/manual.md`: Comprehensive user manual for CLI usage.
- `docs/overview.md`: Technical overview of data flow and package responsibilities.
- `justfile`: Automation scripts.