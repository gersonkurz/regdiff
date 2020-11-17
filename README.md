# REGDIFF - Version 4.4

regdiff is a small command line tool to compare two registry files, export the registry, merge .REG files and much more. Version 4.0 has been completely rewritten in C#, with a much better parser than before (I always say that, don't I?). And it is now finally hosted online, over at code.google.com

regdiff.exe is freeware with a very liberal BSD-style license (i.e. free for any use including commercial).

## Download

Note: You'll need the .NET Framework 4.5 for these tools. It comes by default with Windows 8 or higher, but you can manually download it for some older versions of Windows..

You can download a binary version 4.3 here. The full sourcecode is available on github.com

## Requirements

- .NET 4.5 - If you're still on Windows XP, you're out of luck.
- Administrative rights. Duh, you're accessing the registry, dude!

## Features

The marketing department tells me to write about its most outstanding features, so here they are:

- Compare, diff and merge .REG files
- Compare, diff and merge registry values encoded in .XML files
- Compare, diff and merge the registry
- Support for both ANSI and UNICODE style .REG files
- It's free!

## How to compare two .REG files

The most basic usage is just specifying two filenames.

Example:

    regdiff.exe foo.reg bar.reg

## How to compare a registry key with a given .REG file

You can use `regdiff.exe` to compare any registry key with a given .REG file.

Example:

    regdiff.exe HKEY_LOCAL_MACHINE\SOFTWARE hklm_software.reg

## How to create a diff file

You can use `regdiff.exe` to create a registry file that contains only the differences between to files using the `/diff` option. If you are comparing two files A and B, then the diff file will follow these rules:

- if a key is missing in A, it is to be added
- if a key is missing in B, it is to be removed
- if a value is missing in A, it is to be added
- if a value is missing in B, it is to be removed
- if a value has changed, use the data from B

Example:

    regdiff.exe HKEY_LOCAL_MACHINE\SOFTWARE hklm_software.reg /diff differences.reg

## How to create a merge file

You can use `regdiff.exe` to create a registry file that contains the merged content of two files using the `/merge` option. If you are comparing two files A and B, then the merge file will follow these rules:

- includes all information from key B
- if a key exists in A but is missing in B, it is to be removed
- if a value exists in A but is missing in B, it is to be removed
- if a value has changed from A relative to B, use the data from B

Example:

    regdiff.exe HKEY_LOCAL_MACHINE\SOFTWARE hklm_software.reg /merge merged.reg

## How to export a .REG file

You can use `regdiff.exe` to create a .REG file from an existing registry key

Example:

    regdiff.exe HKEY_LOCAL_MACHINE\SOFTWARE /merge hklm_software.reg

## How to create a sorted .REG file

You can use `regdiff.exe` to create a sorted .REG file. Here, "sorted" means that all keys and all values inside keys are alphanumerically sorted (not case-sensitive). To do so, you must specify a single input file and use the `/merge` option.

Example:

    regdiff.exe example.reg /merge sorted_example.reg

## How to remove empty keys from the output .REG file

If you create an output file using `/merge`, it will by default include empty keys. However, you can exclude those by using the `/NO-EMPTY-KEYS` option.

Example:

    regdiff.exe example.reg /merge sorted_example.reg /no-empty-keys
    
## How to compare the current registry settings with a given .REG file

You can use `regdiff.exe` to compare the current registry with a given .REG file using the `/registry` option.

Example:

    regdiff.exe hklm_software.reg /registry

The difference between the HKEY_LOCAL_MACHINE-syntax and the `/registry` parameter:

- The `/registry` parameter checks all registry keys that are mentioned in the .REG file.
- The `HKEY_LOCAL_MACHINE`-syntax syntax checks all registry keys that exist under the given registry key.

Example: say, you have a registry that has the following keys:

    HKEY_LOCAL_MACHINE\Software\foo
    HKEY_LOCAL_MACHINE\Software\foo\test\one
    HKEY_LOCAL_MACHINE\Software\foo\test\two
    HKEY_LOCAL_MACHINE\Software\foo\hidden

You want to compare this registry with a .REG file that contains

    HKEY_LOCAL_MACHINE\Software\foo\test

Here, using `/r` will not find `foo\hidden`, whereas `HKEY_LOCAL_MACHINE\Software\foo` will do so.

## How to compare renamed keys

Sometimes, you have two very similar keys that you want to compare. For example, in my other live I am maintaining a product with a key like this:

    HKEY_LOCAL_MACHINE\Software\MyProductName

Sometimes, I have multiple copies of the softare installed on my machine, with names like this

    HKEY_LOCAL_MACHINE\Software\MyProductName.Version1
    HKEY_LOCAL_MACHINE\Software\MyProductName.Version2
    HKEY_LOCAL_MACHINE\Software\MyProductName.Version3

Starting with regdiff version 4.2, you can rename two such keys by using the `/alias` option, like this:

    regdiff.exe  HKEY_LOCAL_MACHINE\Software\MyProductName.Version1 HKEY_LOCAL_MACHINE\Software\MyProductName.Version2 /ALIAS MyProductName.Version1=MyProductName.Version2

This will cause the DIFF logic to ignore the key name and just compare anything below that key.

## The .REG file format

The default file format is the unicode format introduced with Windows 2000. Its header reads Windows Registry Editor Version 5.00. You can also use the ANSI format REGEDIT4 used in earlier versions of Windows NT 4 by using the `/4` option.

Example: Export the registry in REGEDIT4 format:

    regdiff.exe HKEY_LOCAL_MACHINE\SOFTWARE /4 /merge hklm_software.reg

Note: The `/4` option can be combined with any of the above options.

## Writing back the registry

Given a (single) .REG file, you can use the `/WRITE` option to write back the contents to the registry

Example:

    regdiff.exe settings.reg /WRITE

The .REG file can contain comments starting with either '#' or ';'

The .REG file can contain variables in the following syntax

    $$VARIABLE$$

For example, the following lines are valid:

    [HKEY_LOCAL_MACHINE\Software\MySuperCompany\Product\$$VERSION$$]
    "SomeOption"="$$OPTIONVALUE$$"
    "SomeInt"=dword:$$NUMBER$$
    "$$VARIABLENAME$$"="Something else"

The following lines are not valid:

    ; missing $$ at the end
    [HKEY_LOCAL_MACHINE\Software\MySuperCompany\Product\$$VERSION]
    
    ; string option is not enclosed in quotation marks
    "SomeOption"=$$OPTIONVALUE$$
    
    ; integer option doesn't support digit-level replacement
    "SomeInt"=dword:005$$NUMBER$$

If you are using this variable replacement, then you must use specify them. There are three ways of doing to:

- By setting environment variables. For example, the code above would work if you had the environment variables VERSION and OPTIONVALUE defined.
- By using an .XML file (see below)
- By using a .INI file

Example scenario: you're writing an installer, and you have a ton of registry parameters, most of which are fixed (so .REG is suitable), but some of which aren't (for example, the installation path).

### Defining replacement variables in .XML files

Example:

    <?xml version="1.0" encoding="utf-8"?>
    <values>
      <value name="VERSION">4.0</value>
      <value name="CONFIGURATION">blub</value>
      <value name="OPTIONVALUE">blabla</value>
    </values>

### Defining replacement variables in .INI files

Example:

    ; please ignore this comment
    # ignore this as well
    
    [Ignored]
    
    VERSION = 4.0
    CONFIGURATION = Some text here
    OPTIONVALUE = Some more text there
    NUMBER = 0x1860 # here is a comment

Note that in the example above, section headers are ignored.

## Playing with fire

When you write back the registry using `/WRITE`, the default option is to not specify any security attributes, so the default security attributes are going to be used instead. If you specify the `/ALLACCESS` option, then a very fine "grant full control to everyone" attribute is going to be set - on everything you're writing. Some people will argue this is a security risk, so viewer discretion is advised.

## Using the XML format

And finally, version 4.0 allows you to export the registry in .XML format using the `/XML` switch, or by specifying a filename that ends in .XML.

## Handling comments

By default, you cannot pass comments in .REG files; if you specify the `/COMMENTS` parameter, you can use both ';' and '#' comments.

## Distinguishing 32-bit/64-bit registry

If you are running a 32-bit operating system, you only have a 32-bit registry. If you are running a 64-bit operating system:

- If your process is 32-bit (default), then you see the 32-bit registry by default. However, if you specify `/64`, you get the 64-bit registry instead.
- If your process is 64-bit, then you see the 64-bit registry by default. However, if you specify `/32`, you get the 32-bit registry instead.

## Options overview

You can use the `/?` option to get a list of all command line parameters.

    ...\regdiff\bin\Release>regdiff /?
    REGDIFF - Version 4.5
    Freeware written by Gerson Kurz (http://p-nand-q.com) [32-bit process on 64-bit OS]
    
    USAGE: REGDIFF.EXE [OPTIONS] FILE {FILE}.
    OPTIONS:
            /MERGE: create merged output file
             /DIFF: create diff output file
         /REGISTRY: compare with the current registry value on your local machine
                /4: use .REG format 4 (non-unicode)
            /QUIET: don't show diff on console
              /XML: use .XML format
           /NOCASE: ignore case (default: case-sensitive)
            /WRITE: write keys/values to registry
         /COMMENTS: support semicolon and hashtag comments in .REG files
        /ALLACCESS: grant all access to everyone (when using the /write option)
           /PARAMS: read value params from file (when using the /write option)
            /ALIAS: alias FOO=BAR
    /NO-EMPTY-KEYS: don't create empty keys
               /64: use 64-bit registry (default for this process: 32-bit)
       FILE {FILE}: one or more .REG files
       
 
