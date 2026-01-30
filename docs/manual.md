# REGDIFF Manual - Version 5.0

regdiff is a command line tool to compare two registry files, export the registry, merge .REG files and much more.

Version 5.0 is a complete rewrite in Go, providing native cross-platform .REG file processing with Windows registry integration.

regdiff is freeware under the MIT license (free for any use including commercial).

## Requirements

- Windows (for live registry operations)
- Administrative rights when accessing the registry

## Features

- Compare, diff and merge .REG files
- Compare, diff and merge the live Windows registry
- Support for both ANSI (REGEDIT4) and Unicode (Windows Registry Editor Version 5.00) formats
- Variable substitution with `$$VAR$$` syntax
- Parameter files in .INI or .XML format

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

Use the `/DIFF` option to create a registry file containing only the differences between two files. When comparing files A and B:

- If a key is missing in A, it is to be added
- If a key is missing in B, it is to be removed
- If a value is missing in A, it is to be added
- If a value is missing in B, it is to be removed
- If a value has changed, use the data from B

```
regdiff HKEY_LOCAL_MACHINE\SOFTWARE hklm_software.reg /DIFF:differences.reg
```

## How to create a merge file

Use the `/MERGE` option to create a registry file with merged content. When comparing files A and B:

- Includes all information from B
- If a key exists in A but is missing in B, it is to be removed
- If a value exists in A but is missing in B, it is to be removed
- If a value has changed from A relative to B, use the data from B

```
regdiff HKEY_LOCAL_MACHINE\SOFTWARE hklm_software.reg /MERGE:merged.reg
```

## How to export a .REG file

Create a .REG file from an existing registry key:

```
regdiff HKEY_LOCAL_MACHINE\SOFTWARE /MERGE:hklm_software.reg
```

## How to create a sorted .REG file

Specify a single input file with `/MERGE` to create a sorted output. "Sorted" means all keys and values are alphanumerically sorted (case-insensitive).

```
regdiff example.reg /MERGE:sorted_example.reg
```

## How to compare the current registry with a .REG file

Use the `/REGISTRY` option to compare a .REG file against the live registry:

```
regdiff hklm_software.reg /REGISTRY
```

### Difference between HKEY_* syntax and /REGISTRY

- `/REGISTRY` checks all registry keys **mentioned in the .REG file**
- `HKEY_*` syntax checks all registry keys **under the given registry key**

Example: Given a registry with these keys:

```
HKEY_LOCAL_MACHINE\Software\foo
HKEY_LOCAL_MACHINE\Software\foo\test\one
HKEY_LOCAL_MACHINE\Software\foo\test\two
HKEY_LOCAL_MACHINE\Software\foo\hidden
```

And a .REG file containing only:

```
HKEY_LOCAL_MACHINE\Software\foo\test
```

Using `/REGISTRY` will **not** find `foo\hidden`, whereas `HKEY_LOCAL_MACHINE\Software\foo` will.

## The .REG file format

The default output format is Unicode (`Windows Registry Editor Version 5.00`). Use `/4` for the ANSI format (`REGEDIT4`):

```
regdiff HKEY_LOCAL_MACHINE\SOFTWARE /4 /MERGE:hklm_software.reg
```

The `/4` option can be combined with any other options.

## Writing to the registry

Use the `/WRITE` option to apply a .REG file to the registry:

```
regdiff settings.reg /WRITE
```

### Comments

When `/COMMENTS` is specified, the .REG file can contain comments starting with `#` or `;`.

### Variable substitution

The .REG file can contain variables using the `$$VARIABLE$$` syntax.

Valid examples:

```
[HKEY_LOCAL_MACHINE\Software\MySuperCompany\Product\$$VERSION$$]
"SomeOption"="$$OPTIONVALUE$$"
"SomeInt"=dword:$$NUMBER$$
"$$VARIABLENAME$$"="Something else"
```

Invalid examples:

```
; missing $$ at the end
[HKEY_LOCAL_MACHINE\Software\MySuperCompany\Product\$$VERSION]

; string option is not enclosed in quotation marks
"SomeOption"=$$OPTIONVALUE$$

; integer option doesn't support digit-level replacement
"SomeInt"=dword:005$$NUMBER$$
```

Variables can be defined via:

- Environment variables
- XML parameter files
- INI parameter files

Use the `/PARAMS` option to specify a parameter file:

```
regdiff settings.reg /WRITE /PARAMS:variables.xml
```

### XML parameter file format

```xml
<?xml version="1.0" encoding="utf-8"?>
<values>
  <value name="VERSION">5.0</value>
  <value name="CONFIGURATION">production</value>
  <value name="OPTIONVALUE">example</value>
</values>
```

### INI parameter file format

```ini
; comments are supported
# this style too

[SectionHeadersAreIgnored]

VERSION = 5.0
CONFIGURATION = Some text here
OPTIONVALUE = Some more text there
NUMBER = 0x1860 # inline comment
```

Note: Section headers in INI files are ignored.

## Security considerations

When writing to the registry with `/WRITE`, default security attributes are used. The `/ALLACCESS` option grants full control to everyone on everything being written. Use with caution.

## 32-bit vs 64-bit registry

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

## Version History

- **Version 5.0** (2026): Complete rewrite in Go
  - Cross-platform .REG file processing
  - Native Windows registry integration
  - MIT license
  - Based on go-regis3 library

- **Version 4.x** (C#/.NET): Original .NET implementation
  - See [master branch](https://github.com/gersonkurz/regdiff/tree/master) for C# version
