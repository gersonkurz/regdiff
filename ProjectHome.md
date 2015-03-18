# REGDIFF - Version 4.1 #

regdiff is a small command line tool to compare two registry files, export the registry, merge .REG files and much more.
Version 4.0 has been completely rewritten in C#, with a much better parser than before (I always say that, don't I?).

<tt>regdiff.exe</tt> is freeware with a very liberal BSD-style license (i.e. free for any use including commercial).

## Download ##

You can [download a binary version 4.1 here](http://regdiff.googlecode.com/svn/trunk/regdiff/releases/regdiff-4.1.zip).
The full sourcecode is available [on Google Code](http://code.google.com/p/regdiff/source/browse)

## Requirements ##

  * .NET 4.5 - If you're still on Windows XP, you're out of luck.
  * Administrative rights. Duh, you're accessing the registry, dude!

## Features ##

The marketing department tells me to write about its most outstanding features, so here they are:

  * Compare, diff and merge .REG files
  * Compare, diff and merge registry values encoded in .XML files
  * Compare, diff and merge the registry
  * Support for both ANSI and UNICODE style .REG files
  * It's free!

## How to compare two <tt>.REG</tt> files ##
The most basic usage is just specifying two filenames.

<em>Example:</em>
<pre>
regdiff.exe foo.reg bar.reg<br>
</pre>

## How to compare a registry key with a given <tt>.REG</tt> file ##
You can use <tt>regdiff.exe</tt> to compare any registry key with
a given <tt>.REG</tt> file.

<em>Example:</em>
<pre>
regdiff.exe HKEY_LOCAL_MACHINE\SOFTWARE hklm_software.reg<br>
</pre>

## How to create a diff file ##
You can use <tt>regdiff.exe</tt> to create a registry file that contains only the differences between to files using the /diff option.
If you are comparing two files A and B, then the diff file will follow these rules:

  * if a key is missing in A, it is to be added
  * if a key is missing in B, it is to be removed
  * if a value is missing in A, it is to be added
  * if a value is missing in B, it is to be removed
  * if a value has changed, use the data from B

<em>Example:</em>
<pre>
regdiff.exe HKEY_LOCAL_MACHINE\SOFTWARE hklm_software.reg /diff differences.reg<br>
</pre>

## How to create a merge file ##
You can use <tt>regdiff.exe</tt> to create a registry file that contains the merged content of two files using the /merge option.
If you are comparing two files A and B, then the merge file will follow these rules:

  * includes all information from key B
  * if a key exists in A but is missing in B, it is to be removed
  * if a value exists in A but is missing in B, it is to be removed
  * if a value has changed from A relative to B, use the data from B

<em>Example:</em>
<pre>
regdiff.exe HKEY_LOCAL_MACHINE\SOFTWARE hklm_software.reg /merge merged.reg<br>
</pre>

## How to export a <tt>.REG</tt> file ##
You can use <tt>regdiff.exe</tt> to create a <tt>.REG</tt> file from
an existing registry key

<em>Example:</em>
<pre>
regdiff.exe HKEY_LOCAL_MACHINE\SOFTWARE /merge hklm_software.reg<br>
</pre>

## How to create a sorted <tt>.REG</tt> file ##

You can use <tt>regdiff.exe</tt> to create a sorted <tt>.REG</tt> file.
Here, "sorted" means that all keys and all values inside keys are
alphanumerically sorted (not case-sensitive). To do so, you must specify
a single input file and use the <tt>/merge</tt> option.

<em>Example:</em>
<pre>
regdiff.exe example.reg /merge sorted_example.reg<br>
</pre>

## How to compare the current registry settings with a given <tt>.REG</tt> file ##

You can use <tt>regdiff.exe</tt> to compare the current registry with
a given <tt>.REG</tt> file using the <tt>/registry</tt> option.

<em>Example:</em>
<pre>
regdiff.exe hklm_software.reg /registry<br>
</pre>

## The difference between the HKEY\_LOCAL\_MACHINE-syntax and the <tt>/registry</tt> parameter ##
<ul>
<li>The <tt>/registry</tt> parameter checks all registry keys that are <b>mentioned in the <tt>.REG</tt></b> file.</li>
<li>The <tt>HKEY_LOCAL_MACHINE-syntax</tt> syntax checks all registry keys that <b>exist under the given registry key</b>.</li>
</ul>

<em>Example:</em> say, you have a registry that has the following keys:
<pre>
HKEY_LOCAL_MACHINE\Software\foo<br>
HKEY_LOCAL_MACHINE\Software\foo\test\one<br>
HKEY_LOCAL_MACHINE\Software\foo\test\two<br>
HKEY_LOCAL_MACHINE\Software\foo\hidden<br>
</pre>
You want to compare this registry with a <tt>.REG</tt> file that contains
<pre>
HKEY_LOCAL_MACHINE\Software\foo\test<br>
</pre>
Here, using <tt>/r</tt> will not find <tt>foo\hidden</tt>, whereas <tt>HKEY_LOCAL_MACHINE\Software\foo</tt> will do so.

## The <tt>.REG</tt> file format ##
The default file format is the unicode format introduced with Windows 2000.
Its header reads <tt>Windows Registry Editor Version 5.00</tt>. You can also
use the ANSI format <tt>REGEDIT4</tt> used in earlier versions of Windows NT 4
by using the <tt>/4</tt> option.
<em>Example:</em> Export the registry in REGEDIT4 format:
<pre>
regdiff.exe HKEY_LOCAL_MACHINE\SOFTWARE /4 /merge hklm_software.reg<br>
</pre>
<em>Note:</em> The <tt>/4</tt> option can be combined with any of the above options.

## Writing back the registry ##
Given a (single) .REG file, you can use the /WRITE option to write back the contents to the registry

<em>Example:</em>
<pre>
regdiff.exe settings.reg /WRITE<br>
</pre>

The .REG file can contain comments starting with either '#' or ';'

The .REG file can contain variables in the following syntax

<pre>
$$VARIABLE$$<br>
</pre>

For example, the following lines are valid:

<pre>
[HKEY_LOCAL_MACHINE\Software\MySuperCompany\Product\$$VERSION$$]<br>
"SomeOption"="$$OPTIONVALUE$$"<br>
"SomeInt"=dword:$$NUMBER$$<br>
"$$VARIABLENAME$$"="Something else"<br>
</pre>

The following lines are not valid:

<pre>
; missing $$ at the end<br>
[HKEY_LOCAL_MACHINE\Software\MySuperCompany\Product\$$VERSION]<br>
<br>
; string option is not enclosed in quotation marks<br>
"SomeOption"=$$OPTIONVALUE$$<br>
<br>
; integer option doesn't support digit-level replacement<br>
"SomeInt"=dword:005$$NUMBER$$<br>
</pre>

If you are using this variable replacement, then you must use specify them. There are three ways of doing to:

  * By setting environment variables. For example, the code above would work if you had the environment variables <tt>VERSION</tt> and <tt>OPTIONVALUE</tt> defined.
  * By using an .XML file (see below)
  * By using a .INI file

Example scenario: you're writing an installer, and you have a ton of registry parameters, most of which are fixed (so .REG is suitable), but some of which aren't (for example, the installation path).

### Defining replacement variables in .XML files ###

<em>Example:</em>

```xml
<?xml version="1.0" encoding="utf-8"?>
<values>

<value name="VERSION">4.0

Unknown end tag for &lt;/value&gt;



<value name="CONFIGURATION">blub

Unknown end tag for &lt;/value&gt;



<value name="OPTIONVALUE">blabla

Unknown end tag for &lt;/value&gt;





Unknown end tag for &lt;/values&gt;



```

### Defining replacement variables in .INI files ###

<em>Example:</em>

<pre>
; please ignore this comment<br>
# ignore this as well<br>
<br>
[Ignored]<br>
<br>
VERSION = 4.0<br>
CONFIGURATION = Some text here<br>
OPTIONVALUE = Some more text there<br>
NUMBER = 0x1860 # here is a comment<br>
<br>
</pre>

Note that in the example above, section headers are ignored.

## Playing with fire ##

When you write back the registry using <tt>/WRITE</tt>, the default option is to not specify any security attributes, so the default
security attributes are going to be used instead. If you specify the <tt>/ALLACCESS</tt> option, then a very fine "grant full control to
everyone" attribute is going to be set - on everything you're writing. Some people will argue this is a security risk, so viewer discretion is advised.

## Using the XML format ##

And finally, version 4.0 allows you to export the registry in .XML format using the <tt>/XML</tt> switch, or by specifying a filename that ends in .XML.

## Distinguishing 32-bit/64-bit registry ##

  * If you are running a 32-bit operating system, you only have a 32-bit registry
  * If you are running a 64-bit operating system:
    1. If your process is 32-bit (default), then you see the 32-bit registry by default. However, if you specify /64, you get the 64-bit registry instead.
    1. If your process is 64-bit, then you see the 64-bit registry by default. However, if you specify /32, you get the 32-bit registry instead.

## Options overview ##

You can use the /? option to get a list of all command line parameters.

<pre>
...\regdiff\bin\Release>regdiff /?<br>
REGDIFF - Version 4.1<br>
Freeware written by Gerson Kurz (http://tikumo.com)<br>
<br>
USAGE: REGDIFF.EXE [OPTIONS] FILE {FILE}.<br>
OPTIONS:<br>
/MERGE: create merged output file<br>
/DIFF: create diff output file<br>
/REGISTRY: compare with the current registry value on your local machine<br>
/4: use .REG format 4 (non-unicode)<br>
/QUIET: don't show diff on console<br>
/XML: use .XML format<br>
/NOCASE: ignore case (default: case-sensitive)<br>
/WRITE: write keys/values to registry<br>
/ALLACCESS: grant all access to everyone (when using the /write option)<br>
/PARAMS: read value params from file (when using the /write option)<br>
FILE {FILE}: one or more .REG files<br>
</pre>

## History ##

  * Version 4.0: new release based on C#
  * Version 4.1:
    1. New Feature: added XML import
    1. Fixed Bug: ExpandSZ strings are exported as hex-encoded in .REG files. This is less readable, but retains the data type
    1. New Feature: added options to distinguish 32-bit/64-bit registry
    1. Fixed Bug: Had a problem detecting default values when importing from the registry. This is fixed now.
    1. Fixed Bug: Did not escape strings / value names properly

