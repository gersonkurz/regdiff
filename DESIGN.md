# go-regdiff Design Document

**Author:** Gemini (Google)
**Date:** 2026-01-28
**Status:** Draft

## 1. Overview

`go-regdiff` is a Go port of the `regdiff` command-line tool. It utilizes the `go-regis3` library to parse, compare, and merge Windows Registry (`.reg`) files.

## 2. Goals

- **Feature Parity:** Replicate all command-line options of the C# `regdiff` tool.
- **Performance:** Efficient processing of large registry files.
- **Single Binary:** Distributable as a standalone executable.

## 3. Command Line Interface

The CLI will support the following operations, matching the C# version:

```
regdiff.exe [OPTIONS] FILE {FILE}
```

### Options

| Option | Description |
|--------|-------------|
| `/MERGE` | Create merged output file |
| `/DIFF` | Create diff output file |
| `/REGISTRY` | Compare with current registry (Windows only) |
| `/4` | Use REGEDIT4 format (ANSI) output |
| `/QUIET` | Don't show diff on console |
| `/XML` | Use XML format output |
| `/NOCASE` | Ignore case (default: case-insensitive) |
| `/WRITE` | Write keys/values to registry |
| `/COMMENTS` | Support semicolon/hashtag comments |
| `/ALLACCESS` | Grant all access (with /WRITE) |
| `/PARAMS` | Read value params from file |
| `/ALIAS` | Alias FOO=BAR for comparison |
| `/NO-EMPTY-KEYS` | Don't create empty keys |
| `/64` / `/32` | Registry view selection |

## 4. Architecture

### 4.1. Package Structure

```
go-regdiff/
├── cmd/
│   └── regdiff/
│       └── main.go       # Entry point, flag parsing
├── diff/
│   ├── regdiff.go        # RegDiff orchestrator and comparison logic
│   ├── mismatch.go       # Mismatch structs and Stringer
│   ├── params.go         # Variable substitution logic
│   └── diff_test.go      # Unit tests
├── internal/
│   └── registry/         # Live registry interaction (Windows only)
├── go.mod
└── go.sum
```

### 4.2. Core Logic

- **Parsing:** Use `go-regis3.ParseFile` to load `.reg` files into `KeyEntry` trees.
- **Comparison Logic:**
    - Iterate trees A and B simultaneously.
    - Track 6 mismatch categories (MissingKeys1/2, MissingValues1/2, DataMismatches, KindMismatches).
    - Handle `/ALIAS` by mapping names during lookup.
    - **Precedence:** File 2 (or Registry) always wins in conflicts (data or type).
- **Diff Mode (`/DIFF`):**
    - Generate a "delta" tree starting from empty.
    - Nodes/Values in 2 but not 1: Add.
    - Nodes/Values in 1 but not 2: Mark for removal.
    - Mismatches: Use version from 2.
- **Merge Mode (`/MERGE`):**
    - Clone File 2 as base.
    - Find items in 1 missing from 2 and add them with `RemoveFlag=true`.
    - Produces a file that syncs a state from 1 to 2.
- **Reporting:**
    - Format mismatches into a human-readable summary for console output.
- **Export:**
    - Use `go-regis3.RegWriter` or `go-regis3.WixWriter` depending on target.
    - Preserves casing from File 2 for updates, File 1 for removals.

## 5. Implementation Plan

### Phase 1: Core Diff/Merge Logic
- [x] Implement `diff` package with `Mismatch` tracking.
- [x] Implement `RegDiff` class (logic container).
- [x] Implement `Merge` function.
- [x] Implement `Diff` function.
- [x] Port `RegEnvReplace` for variable handling.
- [x] Unit tests for logic.

### Phase 2: CLI
- [x] Implement `cmd/regdiff/main.go`.
- [x] Support `/` slash-prefix arguments.
- [x] Implement human-readable summary output.
- [x] Integrate with `go-regis3`.
- [x] Implement `/PARAMS` logic.

### Phase 3: Windows Integration
- [ ] Implement live registry reading/writing.
- [ ] Support `/REGISTRY`, `/WRITE`, `/32`, `/64`.

## 6. Dependencies

- `github.com/gersonkurz/go-regis3` (Local dependency)
