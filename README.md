# REGDIFF - Version 5.0.0

regdiff is a command line tool to compare two registry files, export the registry, merge .REG files and much more. Version 5.0 is a complete rewrite in Go, providing cross-platform support for .REG file operations with native Windows registry access.

regdiff is freeware under the MIT license (free for any use including commercial).

## Installation

```bash
go install github.com/gersonkurz/go-regdiff/cmd/regdiff@latest
```

Or build from source:

```bash
git clone https://github.com/gersonkurz/go-regdiff
cd go-regdiff
go build ./cmd/regdiff
```

## Requirements

- Go 1.21 or higher (for building)
- Administrative rights for registry operations on Windows

## Features

- Compare, diff and merge .REG files
- Compare, diff and merge the live Windows registry
- Export registry keys directly to .REG files
- Support for both ANSI (REGEDIT4) and Unicode (Windows Registry Editor Version 5.00) .REG files
- Variable substitution with `$$VAR$$` syntax
- Parameter files (.INI format)
- Key aliasing for comparing renamed keys
- Cross-platform .REG file processing (Windows registry access requires Windows)

## How to compare two .REG files

The most basic usage is specifying two filenames:

```
regdiff foo.reg bar.reg
```

## How to export a registry key

You can export any registry key directly by specifying a registry path:

```
regdiff HKEY_LOCAL_MACHINE\SOFTWARE /MERGE:hklm_software.reg
```

Short hive names are also supported: `HKLM`, `HKCU`, `HKCR`, `HKU`, `HKCC`.

## How to compare a registry key with a .REG file

Compare a live registry key against a .REG file:

```
regdiff HKEY_LOCAL_MACHINE\SOFTWARE hklm_software.reg
```

## How to create a diff file

Use `/DIFF` to create a registry file containing only the differences between two sources. When comparing A and B:

- Keys/values in B but not in A are marked for addition
- Keys/values in A but not in B are marked for removal
- Changed values use data from B

```
regdiff old_settings.reg new_settings.reg /DIFF:changes.reg
```

## How to create a merge file

Use `/MERGE` to create a registry file with merged content:

```
regdiff base.reg overlay.reg /MERGE:merged.reg
```

## How to create a sorted .REG file

Specify a single input file with `/MERGE` to create a sorted, normalized output:

```
regdiff messy.reg /MERGE:sorted.reg
```

## How to remove empty keys from output

Use `/NO-EMPTY-KEYS` to exclude keys that have no values:

```
regdiff input.reg /MERGE:output.reg /NO-EMPTY-KEYS
```

## How to compare a .REG file against the live registry

Use `/REGISTRY` to compare a .REG file against the current registry state:

```
regdiff expected_settings.reg /REGISTRY
```

**Difference between `HKEY_*` and `/REGISTRY`:**

- `HKEY_*` reads all keys under the specified path from the registry
- `/REGISTRY` reads only the keys mentioned in the .REG file from the registry

## How to compare renamed keys

Use `/ALIAS` to compare keys with different names:

```
regdiff old_version.reg new_version.reg /ALIAS:MyProduct.v1=MyProduct.v2
```

Multiple aliases can be specified by repeating the `/ALIAS` option.

## Output format

The default output format is Unicode (Windows Registry Editor Version 5.00). Use `/4` for the legacy ANSI format (REGEDIT4):

```
regdiff HKEY_CURRENT_USER\Software /4 /MERGE:output.reg
```

## Writing to the registry

Use `/WRITE` to apply changes to the registry:

```
regdiff settings.reg /WRITE
```

When using `/WRITE`, the input file can contain:

- Comments starting with `#` or `;` (always allowed with `/WRITE`, or use `/COMMENTS` for other operations)
- Variables using `$$VARIABLE$$` syntax

Example with variables:

```
[HKEY_LOCAL_MACHINE\Software\MyCompany\Product\$$VERSION$$]
"InstallPath"="$$INSTALLDIR$$"
"Port"=dword:$$PORT$$
```

Variables can be defined via parameter files (`/PARAMS`):

### INI parameter file format

```ini
; comments are allowed
# this style too

[SectionHeadersAreIgnored]
VERSION = 4.0
INSTALLDIR = C:\Program Files\MyApp
PORT = 8080
```

## Security considerations

With `/WRITE`, default security attributes are used. The `/ALLACCESS` option grants full control to everyone - use with caution.

## 32-bit/64-bit registry views

On 64-bit Windows, use `/32` or `/64` to access specific registry views:

- `/32` - Access 32-bit registry view (WOW6432Node)
- `/64` - Access 64-bit registry view

## Options reference

```
REGDIFF - Version 5.0.0
Freeware written by Gerson Kurz (http://p-nand-q.com) [64-bit]

Usage: regdiff [OPTIONS] FILE|HKEY_* {FILE|HKEY_*}

Options:
  /MERGE:<file>      Create merged output file
  /DIFF:<file>       Create diff output file
  /QUIET             Don't show diff on console
  /NO-EMPTY-KEYS     Don't create empty keys in output
  /4                 Use REGEDIT4 format (ANSI, non-unicode)
  /COMMENTS          Allow # and ; line comments in input
  /ALIAS:FOO=BAR     Alias key names for comparison (repeatable)
  /PARAMS:<file>     Parameter file for $$VAR$$ substitution (.ini)
  /REGISTRY          Compare input file against live registry
  /WRITE             Write result to registry (Windows only)
  /ALLACCESS         Grant all access when writing (use with /WRITE)
  /32                Use 32-bit registry view (default: 64-bit)
  /? or /HELP        Show this help

Examples:
  regdiff file1.reg file2.reg                    Compare two .REG files
  regdiff file1.reg file2.reg /DIFF:changes.reg  Create diff file
  regdiff HKEY_CURRENT_USER\Software /MERGE:out.reg  Export registry key
  regdiff settings.reg /REGISTRY                 Compare file with registry
  regdiff settings.reg /WRITE                    Apply .REG file to registry
```

Note: The `/32` or `/64` option shown depends on the process architecture. A 64-bit process shows `/32` to access the 32-bit registry view, while a 32-bit process on 64-bit Windows shows `/64` to access the 64-bit view.

## Related Projects

- [go-regis3](https://github.com/gersonkurz/go-regis3) - The underlying Go library for .REG file parsing
- [regdiff (C#)](https://github.com/gersonkurz/regdiff/tree/master) - The original .NET implementation (version 4.x)
- [pnq](https://github.com/gersonkurz/pnq) - C++ header-only library including `pnq::regis3`

## License

MIT License - see LICENSE file for details.
