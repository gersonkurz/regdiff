# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

go-regdiff is a Go CLI tool for comparing, diffing, and merging Windows Registry files (.REG) and live registry hives. It's a complete rewrite of the original C#/.NET regdiff utility, providing cross-platform .REG file processing with native Windows registry access on Windows.

## Build Commands

This project uses `just` (command runner) for automation:

```bash
just build              # Build for current platform
just build-all          # Build all 6 platform targets
just build-windows      # Build Windows (amd64 + arm64)
just build-linux        # Build Linux (amd64 + arm64)
just build-darwin       # Build macOS (amd64 + arm64)
just release            # Create release packages
```

Individual platform targets: `build-windows-amd64`, `build-windows-arm64`, `build-linux-amd64`, `build-linux-arm64`, `build-darwin-amd64`, `build-darwin-arm64`

## Testing and Code Quality

```bash
just test               # Run all tests
just test-verbose       # Run tests with verbose output
just fmt                # Format code with gofmt
just fmt-check          # Check formatting
just vet                # Run go vet
just check              # Run all checks (fmt, vet, test)
```

Run a single test:
```bash
go test -v -run TestDiff ./diff/
```

## Architecture

### Core Packages

- **cmd/regdiff/main.go** - CLI entry point handling argument parsing (supports `/FLAG` and `-flag`), file loading via go-regis3, and orchestration of diff/merge/registry operations

- **diff/** - Core comparison engine:
  - `regdiff.go` - `RegDiff` struct with `CompareRecursive()`, `CreateDiffKeyEntry()`, `CreateMergeKeyEntry()` for tree traversal and output generation
  - `mismatch.go` - `Mismatch` and `MismatchCategory` types for categorizing differences
  - `params.go` - Parameter substitution (`$$VAR$$` syntax) from INI/XML files

- **internal/registry/** - Windows registry integration (platform-specific with build tags):
  - `registry.go` - `ParseRegistry()` reads live registry into KeyEntry trees
  - `ops.go` - `LoadLiveRegistry()`, `WriteToRegistry()`, hive mapping
  - `writer.go` - Low-level registry writing with syscalls
  - `stub.go` - Non-Windows stubs returning `ErrNotSupported`

### External Dependency

**go-regis3** (`github.com/gersonkurz/go-regis3`) - Handles .REG file parsing, KeyEntry/ValueEntry structures, and value type handling. Referenced via local replace directive in go.mod.

### Platform-Specific Code

Uses Go build tags for Windows-specific registry operations:
- `//go:build windows` for registry reading/writing
- `//go:build !windows` for cross-platform stubs

This allows .REG file processing on any OS while registry operations require Windows.

## Key Behaviors

- Case-insensitive key/value matching (consistent with Windows registry)
- Supports ANSI (REGEDIT4) and Unicode (Windows Registry Editor Version 5.00) formats
- Parameter substitution uses `$$VAR$$` syntax with INI/XML/environment variable support
- Aliasing allows comparing differently-named registry paths
