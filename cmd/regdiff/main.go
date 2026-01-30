package main

import (
	"flag"
	"fmt"
	"os"
	"strings"

	"github.com/gersonkurz/go-regdiff/diff"
	"github.com/gersonkurz/go-regdiff/internal/registry"
	"github.com/gersonkurz/go-regis3"
)

type cliArgs struct {
	mergeFile   string
	diffFile    string
	quiet       bool
	noEmptyKeys bool
	aliases     []string
	format4     bool
	comments    bool
	nocase      bool // Not used explicitly as go-regis3 is case-insensitive by default
	paramsFile  string
	
	// Registry flags
	useRegistry bool
	write       bool
	allAccess   bool
	view32      bool
	view64      bool
}

func main() {
	args := parseArgs()

	if len(flag.Args()) < 1 {
		printUsage()
		os.Exit(10)
	}

	filenames := flag.Args()
	files := make([]*regis3.KeyEntry, 0, len(filenames))

	// Import Options
	importOpts := regis3.ImportOptionsNone
	if args.comments {
		importOpts |= regis3.AllowHashtagComments | regis3.AllowSemicolonComments
	}
	importOpts |= regis3.AllowVariableNamesForNonStringVariables
	
	// Export Options
	exportOpts := regis3.ExportOptionsNone
	if args.noEmptyKeys {
		exportOpts |= regis3.NoEmptyKeys
	}

	// Parse Aliases
	aliasMap := make(map[string]string)
	for _, a := range args.aliases {
		parts := strings.SplitN(a, "=", 2)
		if len(parts) == 2 {
			aliasMap[parts[0]] = parts[1]
		} else {
			fmt.Println("ERROR, the /alias option must be of the form FOO=BAR")
			os.Exit(10)
		}
	}

	// Load Params
	var params map[string]string
	if args.paramsFile != "" {
		var err error
		params, err = diff.LoadParams(args.paramsFile)
		if err != nil {
			fmt.Printf("Error reading params file %s: %v\n", args.paramsFile, err)
			os.Exit(10)
		}
	}

	// Load Files
	for _, filename := range filenames {
		if !args.quiet {
			fmt.Printf("Reading %s...\n", filename)
		}
		
		if strings.HasSuffix(strings.ToLower(filename), ".xml") {
			fmt.Printf("XML format not yet supported: %s\n", filename)
			os.Exit(10)
		}

		key, err := regis3.ParseFile(filename, importOpts)
		if err != nil {
			fmt.Printf("Error reading %s: %v\n", filename, err)
			os.Exit(10)
		}

		if len(params) > 0 {
			diff.ApplyParams(key, params)
		}

		files = append(files, key)
	}
	
	// If /REGISTRY is specified, we compare the first file against the live registry.
	if args.useRegistry {
		if len(files) != 1 {
			fmt.Println("Error: /REGISTRY requires exactly one input file.")
			os.Exit(10)
		}
		
		fileKey := files[0]
		liveRoot := regis3.NewKeyEntry(nil, "")
		
		// Access flags
		access := uint32(registry.AccessRead)
		if args.view32 { access |= registry.View32 }
		if args.view64 { access |= registry.View64 }
		
		err := registry.LoadLiveRegistry(liveRoot, fileKey, access)
		if err != nil {
			fmt.Printf("Error reading registry: %v\n", err)
			os.Exit(10)
		}
		
		files = append(files, liveRoot)
		filenames = append(filenames, "REGISTRY")
	}

	if !args.quiet {
		fmt.Println()
	}

	// Single File Mode
	if len(files) == 1 {
		if args.write {
			if !args.quiet {
				fmt.Println("Writing to registry...")
			}
		
		access := uint32(registry.AccessWrite)
		if args.view32 { access |= registry.View32 }
		if args.view64 { access |= registry.View64 }
		if args.allAccess { access |= registry.AccessAll }
		
			err := registry.WriteToRegistry(files[0], access)
			if err != nil {
				fmt.Printf("Error writing to registry: %v\n", err)
				os.Exit(10)
			}
			return
		}
		
		if args.mergeFile != "" {
			writeOutput(args.mergeFile, files[0], args.format4, exportOpts, args.quiet)
		}
		return
	}

	// Multi File Mode
	for i := 0; i < len(files); i++ {
		for j := i + 1; j < len(files); j++ {
			f1 := files[i]
			f2 := files[j]
			name1 := filenames[i]
			name2 := filenames[j]

			rd := diff.NewRegDiff(f1, name1, f2, name2, aliasMap)
			
			if !args.quiet {
				fmt.Println(rd.String())
			}

			if args.diffFile != "" {
				diffKey := rd.CreateDiffKeyEntry()
				writeOutput(args.diffFile, diffKey, args.format4, exportOpts, args.quiet)
				
				if args.write {
					access := uint32(registry.AccessWrite)
					if args.view32 { access |= registry.View32 }
					if args.view64 { access |= registry.View64 }
					if args.allAccess { access |= registry.AccessAll }
					
					if !args.quiet { fmt.Println("Applying DIFF to registry...") }
					if err := registry.WriteToRegistry(diffKey, access); err != nil {
						fmt.Printf("Error writing to registry: %v\n", err)
						os.Exit(10)
					}
				}
			}

			if args.mergeFile != "" {
				mergeKey := rd.CreateMergeKeyEntry()
				writeOutput(args.mergeFile, mergeKey, args.format4, exportOpts, args.quiet)
				
				if args.write && args.diffFile == "" {
					access := uint32(registry.AccessWrite)
					if args.view32 { access |= registry.View32 }
					if args.view64 { access |= registry.View64 }
					if args.allAccess { access |= registry.AccessAll }
					
					if !args.quiet { fmt.Println("Applying MERGE to registry...") }
					if err := registry.WriteToRegistry(mergeKey, access); err != nil {
						fmt.Printf("Error writing to registry: %v\n", err)
						os.Exit(10)
					}
				}
			}
			
			if args.write && args.diffFile == "" && args.mergeFile == "" {
				mergeKey := rd.CreateMergeKeyEntry()
				access := uint32(registry.AccessWrite)
				if args.view32 { access |= registry.View32 }
				if args.view64 { access |= registry.View64 }
				if args.allAccess { access |= registry.AccessAll }
				
				if !args.quiet { fmt.Println("Applying changes to registry...") }
				if err := registry.WriteToRegistry(mergeKey, access); err != nil {
					fmt.Printf("Error writing to registry: %v\n", err)
					os.Exit(10)
				}
			}
		}
	}
}

func writeOutput(filename string, key *regis3.KeyEntry, format4 bool, opts regis3.ExportOptions, quiet bool) {
	if !quiet {
		fmt.Printf("Writing %s...\n", filename)
	}

	f, err := os.Create(filename)
	if err != nil {
		fmt.Printf("Error creating file %s: %v\n", filename, err)
		os.Exit(10)
	}
	defer f.Close()

	header := regis3.HeaderWindows5
	useUtf16 := true
	if format4 {
		header = regis3.HeaderRegedit4
		useUtf16 = false
	}

	writer := regis3.NewRegWriter(header, opts, useUtf16)
	if err := writer.Write(f, key); err != nil {
		fmt.Printf("Error writing file %s: %v\n", filename, err)
		os.Exit(10)
	}
	
	if !quiet {
		fmt.Println()
	}
}

func parseArgs() *cliArgs {
	newArgs := make([]string, 0, len(os.Args))
	newArgs = append(newArgs, os.Args[0])

	for _, arg := range os.Args[1:] {
		if strings.HasPrefix(arg, "/") {
			if strings.Contains(arg, ":") {
				parts := strings.SplitN(arg, ":", 2)
				key := "-" + parts[0][1:]
				val := parts[1]
				newArgs = append(newArgs, key+"="+val)
			} else {
				newArgs = append(newArgs, "-" + arg[1:])
			}
		} else {
			newArgs = append(newArgs, arg)
		}
	}

	os.Args = newArgs

	args := &cliArgs{}
	var aliases aliasFlags

	flag.StringVar(&args.mergeFile, "merge", "", "create merged output file")
	flag.StringVar(&args.mergeFile, "MERGE", "", "create merged output file")
	flag.StringVar(&args.diffFile, "diff", "", "create diff output file")
	flag.StringVar(&args.diffFile, "DIFF", "", "create diff output file")
	flag.BoolVar(&args.quiet, "quiet", false, "don't show diff on console")
	flag.BoolVar(&args.quiet, "QUIET", false, "don't show diff on console")
	flag.BoolVar(&args.noEmptyKeys, "no-empty-keys", false, "don't create empty keys")
	flag.BoolVar(&args.noEmptyKeys, "NO-EMPTY-KEYS", false, "don't create empty keys")
	flag.BoolVar(&args.format4, "4", false, "use .REG format 4 (non-unicode)")
	flag.BoolVar(&args.comments, "comments", false, "allow line comments")
	flag.BoolVar(&args.comments, "COMMENTS", false, "allow line comments")
	flag.BoolVar(&args.nocase, "nocase", false, "ignore case (default)")
	flag.BoolVar(&args.nocase, "NOCASE", false, "ignore case (default)")
	flag.Var(&aliases, "alias", "alias FOO=BAR")
	flag.Var(&aliases, "ALIAS", "alias FOO=BAR")
	flag.StringVar(&args.paramsFile, "params", "", "params file (.ini or .xml)")
	flag.StringVar(&args.paramsFile, "PARAMS", "", "params file (.ini or .xml)")

	// Registry Flags
	flag.BoolVar(&args.useRegistry, "registry", false, "compare with registry")
	flag.BoolVar(&args.useRegistry, "REGISTRY", false, "compare with registry")
	flag.BoolVar(&args.write, "write", false, "write to registry")
	flag.BoolVar(&args.write, "WRITE", false, "write to registry")
	flag.BoolVar(&args.allAccess, "allaccess", false, "grant all access (requires /WRITE)")
	flag.BoolVar(&args.allAccess, "ALLACCESS", false, "grant all access (requires /WRITE)")
	flag.BoolVar(&args.view32, "32", false, "use 32-bit registry view")
	flag.BoolVar(&args.view64, "64", false, "use 64-bit registry view")

	var dummyBool bool
	flag.BoolVar(&dummyBool, "xml", false, "use xml format (not supported)")
	flag.BoolVar(&dummyBool, "XML", false, "use xml format (not supported)")
	
	flag.Parse()
	args.aliases = aliases
	return args
}

type aliasFlags []string

func (i *aliasFlags) String() string {
	return fmt.Sprint(*i)
}

func (i *aliasFlags) Set(value string) error {
	*i = append(*i, value)
	return nil
}

func printUsage() {
	fmt.Println("regdiff")
	fmt.Println("Usage: regdiff [OPTIONS] FILE {FILE}")
	fmt.Println()
	flag.PrintDefaults()
}