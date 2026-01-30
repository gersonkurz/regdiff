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
- Support for both ANSI (REGEDIT4) and Unicode (Windows Registry Editor Version 5.00) .REG files
- Variable substitution with `$$VAR$$` syntax
- Parameter files (.INI or .XML format)
- Key aliasing for comparing renamed keys
- Cross-platform .REG file processing (Windows registry access requires Windows)

## How to compare two .REG files

The most basic usage is specifying two filenames.

```
regdiff foo.reg bar.reg
```

## How to compare a registry key with a given .REG file

You can use regdiff to compare any registry key with a given .REG file.

```
regdiff HKEY_LOCAL_MACHINE\SOFTWARE hklm_software.reg
```

## How to create a diff file

Use the `/DIFF` option to create a registry file containing only the differences between two files. If comparing files A and B:

- Keys missing in A are marked for addition
- Keys missing in B are marked for removal
- Values missing in A are marked for addition
- Values missing in B are marked for removal
- Changed values use data from B

```
regdiff HKEY_LOCAL_MACHINE\SOFTWARE hklm_software.reg /DIFF:differences.reg
```

## How to create a merge file

Use the `/MERGE` option to create a registry file with merged content. If comparing files A and B:

- Includes all information from B
- Keys existing only in A are marked for removal
- Values existing only in A are marked for removal
- Changed values use data from B

```
regdiff HKEY_LOCAL_MACHINE\SOFTWARE hklm_software.reg /MERGE:merged.reg
```

## How to export a .REG file

Create a .REG file from an existing registry key:

```
regdiff HKEY_LOCAL_MACHINE\SOFTWARE /MERGE:hklm_software.reg
```

## How to create a sorted .REG file

Specify a single input file with `/MERGE` to create a sorted output (keys and values sorted alphanumerically, case-insensitive):

```
regdiff example.reg /MERGE:sorted_example.reg
```

## How to remove empty keys from the output

Use `/NO-EMPTY-KEYS` to exclude keys without values:

```
regdiff example.reg /MERGE:sorted_example.reg /NO-EMPTY-KEYS
```

## How to compare the current registry with a .REG file

Use `/REGISTRY` to compare a .REG file against the live registry:

```
regdiff hklm_software.reg /REGISTRY
```

The difference between `HKEY_*` syntax and `/REGISTRY`:

- `/REGISTRY` checks only keys mentioned in the .REG file
- `HKEY_*` syntax checks all keys under the specified path

## How to compare renamed keys

Use `/ALIAS` to compare keys with different names:

```
regdiff HKEY_LOCAL_MACHINE\Software\MyProduct.v1 HKEY_LOCAL_MACHINE\Software\MyProduct.v2 /ALIAS:MyProduct.v1=MyProduct.v2
```

## The .REG file format

The default output format is Unicode (Windows Registry Editor Version 5.00). Use `/4` for the ANSI format (REGEDIT4):

```
regdiff HKEY_LOCAL_MACHINE\SOFTWARE /4 /MERGE:hklm_software.reg
```

## Writing to the registry

Use `/WRITE` to apply a .REG file to the registry:

```
regdiff settings.reg /WRITE
```

The .REG file can contain:

- Comments starting with `#` or `;` (when `/COMMENTS` is specified)
- Variables using `$$VARIABLE$$` syntax

Example with variables:

```
[HKEY_LOCAL_MACHINE\Software\MySuperCompany\Product\$$VERSION$$]
"SomeOption"="$$OPTIONVALUE$$"
"SomeInt"=dword:$$NUMBER$$
"$$VARIABLENAME$$"="Something else"
```

Variables can be defined via:

- Environment variables
- XML parameter files
- INI parameter files

### XML parameter file format

```xml
<?xml version="1.0" encoding="utf-8"?>
<values>
  <value name="VERSION">4.0</value>
  <value name="CONFIGURATION">blub</value>
  <value name="OPTIONVALUE">blabla</value>
</values>
```

### INI parameter file format

```ini
; comments are allowed
# this style too

[SectionHeadersAreIgnored]

VERSION = 4.0
CONFIGURATION = Some text here
OPTIONVALUE = Some more text there
NUMBER = 0x1860 # inline comment
```

## Security considerations

With `/WRITE`, default security attributes are used. The `/ALLACCESS` option grants full control to everyone - use with caution.

## Distinguishing 32-bit/64-bit registry

On 64-bit Windows:

- `/32` - Access 32-bit registry view (WOW6432Node)
- `/64` - Access 64-bit registry view

## Options overview

```
REGDIFF - Version 5.0.0
Freeware written by Gerson Kurz (http://p-nand-q.com) [windows/amd64]

Usage: regdiff [OPTIONS] FILE {FILE}

OPTIONS:
        /MERGE:<file>   create merged output file
         /DIFF:<file>   create diff output file
     /REGISTRY          compare with the current registry
            /4          use .REG format 4 (non-unicode)
        /QUIET          don't show diff on console
      /COMMENTS         support semicolon and hashtag comments
        /WRITE          write keys/values to registry
    /ALLACCESS          grant all access to everyone (with /WRITE)
 /PARAMS:<file>         read value params from file (with /WRITE)
  /ALIAS:FOO=BAR        alias key names for comparison
/NO-EMPTY-KEYS          don't create empty keys
           /32          use 32-bit registry view
           /64          use 64-bit registry view
```

## Related Projects

- [go-regis3](https://github.com/gersonkurz/go-regis3) - The underlying Go library for .REG file parsing
- [regdiff (C#)](https://github.com/gersonkurz/regdiff/tree/master) - The original .NET implementation (version 4.x, on master branch)
- [pnq](https://github.com/gersonkurz/pnq) - C++ header-only library including `pnq::regis3`

## License

MIT License - see LICENSE file for details.
