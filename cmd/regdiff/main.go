package main

import (
	"flag"
	"fmt"
	"os"
	"path/filepath"
	"strings"

	"github.com/gersonkurz/go-regdiff/diff"
	"github.com/gersonkurz/go-regdiff/internal/registry"
	"github.com/gersonkurz/go-regis3"
)

// ANSI Color codes
const (
	colorReset  = "\033[0m"
	colorRed    = "\033[31m"
	colorGreen  = "\033[32m"
	colorYellow = "\033[33m"
	colorCyan   = "\033[36m"
)

func printError(format string, a ...interface{}) {
	fmt.Fprintf(os.Stderr, colorRed+"ERROR: "+format+colorReset+"\n", a...)
}

func printWarning(format string, a ...interface{}) {
	fmt.Fprintf(os.Stderr, colorYellow+"WARNING: "+format+colorReset+"\n", a...)
}

func printSuccess(format string, a ...interface{}) {
	fmt.Printf(colorGreen+format+colorReset+"\n", a...)
}

func printInfo(format string, a ...interface{}) {
	fmt.Printf(colorCyan+format+colorReset+"\n", a...)
}

// Version is set via ldflags at build time
var Version = "5.0.0"

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

	// Parse Options
	parseOpts := &regis3.ParseOptions{
		AllowHashtagComments:      args.comments,
		AllowSemicolonComments:    args.comments,
		AllowVariableSubstitution: true,
	}

	// Export Options
	exportOpts := regis3.ExportOptions{
		NoEmptyKeys: args.noEmptyKeys,
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
			printError("Reading params file %s: %v", args.paramsFile, err)
			os.Exit(10)
		}
	}
	// When writing to registry, merge environment variables into params
	if args.write {
		if params == nil {
			params = make(map[string]string)
		}
		diff.MergeEnvironmentVariables(params)
	}

	// Access flags for registry operations
	accessRead := uint32(registry.AccessRead)
	if args.view32 {
		accessRead |= registry.View32
	}
	if args.view64 {
		accessRead |= registry.View64
	}

	// Load Files or Registry Keys
	for _, filename := range filenames {
		if !args.quiet {
			printInfo("Reading %s...", filename)
		}

		var key *regis3.KeyEntry
		var err error

		if isRegistryPath(filename) {
			// Read from live registry
			key, err = registry.ReadRegistryPath(filename, accessRead)
			if err != nil {
				printError("Reading registry %s: %v", filename, err)
				os.Exit(10)
			}
		} else {
			// Read from file
			if strings.HasSuffix(strings.ToLower(filename), ".xml") {
				printError("XML format not yet supported: %s", filename)
				os.Exit(10)
			}

			key, err = regis3.ParseFile(filename, parseOpts)
			if err != nil {
				printError("Reading %s: %v", filename, err)
				os.Exit(10)
			}
			if len(params) > 0 {
				diff.ApplyParams(key, params)
			}
		}

		files = append(files, key)
	}

	// If /REGISTRY is specified, we compare the first file against the live registry.
	if args.useRegistry {
		if len(files) != 1 {
			printError("/REGISTRY requires exactly one input file.")
			os.Exit(10)
		}

		fileKey := files[0]
		liveRoot := regis3.NewKeyEntry(nil, "")

		err := registry.LoadLiveRegistry(liveRoot, fileKey, accessRead)
		if err != nil {
			printError("Reading registry: %v", err)
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
				printInfo("Writing to registry...")
			}

			access := uint32(registry.AccessWrite)
			if args.view32 {
				access |= registry.View32
			}
			if args.view64 {
				access |= registry.View64
			}
			if args.allAccess {
				access |= registry.AccessAll
			}

			err := registry.WriteToRegistry(files[0], access)
			if err != nil {
				printError("Writing to registry: %v", err)
				os.Exit(10)
			}
			if !args.quiet {
				printSuccess("Done.")
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
				diffOut := args.diffFile
				if len(files) > 2 {
					diffOut = deriveOutputName(args.diffFile, name1, name2)
				}
				writeOutput(diffOut, diffKey, args.format4, exportOpts, args.quiet)

				if args.write {
					access := uint32(registry.AccessWrite)
					if args.view32 {
						access |= registry.View32
					}
					if args.view64 {
						access |= registry.View64
					}
					if args.allAccess {
						access |= registry.AccessAll
					}

					if !args.quiet {
						fmt.Println("Applying DIFF to registry...")
					}
					if err := registry.WriteToRegistry(diffKey, access); err != nil {
						fmt.Printf("Error writing to registry: %v\n", err)
						os.Exit(10)
					}
				}
			}

			if args.mergeFile != "" {
				mergeKey := rd.CreateMergeKeyEntry()
				mergeOut := args.mergeFile
				if len(files) > 2 {
					mergeOut = deriveOutputName(args.mergeFile, name1, name2)
				}
				writeOutput(mergeOut, mergeKey, args.format4, exportOpts, args.quiet)

				if args.write && args.diffFile == "" {
					access := uint32(registry.AccessWrite)
					if args.view32 {
						access |= registry.View32
					}
					if args.view64 {
						access |= registry.View64
					}
					if args.allAccess {
						access |= registry.AccessAll
					}

					if !args.quiet {
						printInfo("Applying MERGE to registry...")
					}
					if err := registry.WriteToRegistry(mergeKey, access); err != nil {
						printError("Writing to registry: %v", err)
						os.Exit(10)
					}
				}
			}
			if args.write && args.diffFile == "" && args.mergeFile == "" {
				mergeKey := rd.CreateMergeKeyEntry()
				access := uint32(registry.AccessWrite)
				if args.view32 {
					access |= registry.View32
				}
				if args.view64 {
					access |= registry.View64
				}
				if args.allAccess {
					access |= registry.AccessAll
				}

				if !args.quiet {
					printInfo("Applying changes to registry...")
				}
				if err := registry.WriteToRegistry(mergeKey, access); err != nil {
					printError("Writing to registry: %v", err)
					os.Exit(10)
				}
			}
		}
	}
}

func writeOutput(filename string, key *regis3.KeyEntry, format4 bool, opts regis3.ExportOptions, quiet bool) {
	if !quiet {
		printInfo("Writing %s...", filename)
	}

	f, err := os.Create(filename)
	if err != nil {
		printError("Creating file %s: %v", filename, err)
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
		printError("Writing file %s: %v", filename, err)
		os.Exit(10)
	}

	if !quiet {
		printSuccess("Done.")
		fmt.Println()
	}
}
func deriveOutputName(template, name1, name2 string) string {
	ext := filepath.Ext(template)
	base := strings.TrimSuffix(template, ext)
	return fmt.Sprintf("%s-%s-vs-%s%s", base, sanitizeName(name1), sanitizeName(name2), ext)
}

func sanitizeName(name string) string {
	base := filepath.Base(name)
	base = strings.ReplaceAll(base, "\\", "_")
	base = strings.ReplaceAll(base, "/", "_")
	var b strings.Builder
	for _, r := range base {
		if (r >= 'a' && r <= 'z') || (r >= 'A' && r <= 'Z') || (r >= '0' && r <= '9') || r == '_' || r == '-' || r == '.' {
			b.WriteRune(r)
		} else {
			b.WriteRune('_')
		}
	}
	if b.Len() == 0 {
		return "input"
	}
	return b.String()
}

func parseArgs() *cliArgs {
	args := &cliArgs{}
	positionalArgs := []string{}

	// expectValue is set when we need the next arg as a parameter value
	var expectValue *string

	for i := 1; i < len(os.Args); i++ {
		arg := os.Args[i]

		// If we're expecting a value for a previous option
		if expectValue != nil {
			*expectValue = arg
			expectValue = nil
			continue
		}

		if strings.HasPrefix(arg, "/") || strings.HasPrefix(arg, "-") {
			// Normalize: remove leading / or - or --
			arg = strings.TrimPrefix(arg, "/")
			arg = strings.TrimPrefix(arg, "--")
			arg = strings.TrimPrefix(arg, "-")

			// Check for inline value (FLAG:VALUE or FLAG=VALUE)
			var key, val string
			var hasVal bool
			if idx := strings.IndexAny(arg, ":="); idx != -1 {
				key = strings.ToUpper(arg[:idx])
				val = arg[idx+1:]
				hasVal = true
			} else {
				key = strings.ToUpper(arg)
				hasVal = false
			}

			switch key {
			case "MERGE":
				if hasVal {
					args.mergeFile = val
				} else {
					expectValue = &args.mergeFile
				}
			case "DIFF":
				if hasVal {
					args.diffFile = val
				} else {
					expectValue = &args.diffFile
				}
			case "PARAMS":
				if hasVal {
					args.paramsFile = val
				} else {
					expectValue = &args.paramsFile
				}
			case "ALIAS":
				if hasVal {
					args.aliases = append(args.aliases, val)
				} else {
					// Need to collect next arg
					i++
					if i < len(os.Args) {
						args.aliases = append(args.aliases, os.Args[i])
					}
				}
			case "QUIET":
				args.quiet = true
			case "NO-EMPTY-KEYS":
				args.noEmptyKeys = true
			case "4":
				args.format4 = true
			case "COMMENTS":
				args.comments = true
			case "NOCASE":
				args.nocase = true
			case "REGISTRY":
				args.useRegistry = true
			case "WRITE":
				args.write = true
			case "ALLACCESS":
				args.allAccess = true
			case "32":
				args.view32 = true
			case "64":
				args.view64 = true
			case "XML":
				// Ignored, not supported
			case "?", "H", "HELP":
				printUsage()
				os.Exit(0)
			default:
				fmt.Printf("Error, argument '%s' is invalid.\n", key)
				os.Exit(10)
			}
		} else {
			positionalArgs = append(positionalArgs, arg)
		}
	}

	// Replace os.Args for flag.Args() compatibility
	os.Args = append([]string{os.Args[0]}, positionalArgs...)
	flag.Parse()

	return args
}

// isRegistryPath checks if the path looks like a registry path (HKEY_* or short forms)
func isRegistryPath(path string) bool {
	upper := strings.ToUpper(path)
	prefixes := []string{
		"HKEY_CLASSES_ROOT", "HKEY_CURRENT_USER", "HKEY_LOCAL_MACHINE",
		"HKEY_USERS", "HKEY_CURRENT_CONFIG", "HKEY_PERFORMANCE_DATA",
		"HKCR", "HKCU", "HKLM", "HKU", "HKCC", "HKPD",
	}
	for _, prefix := range prefixes {
		if upper == prefix || strings.HasPrefix(upper, prefix+"\\") {
			return true
		}
	}
	return false
}

func getProcessType() string {
	if regis3.Is64BitProcess() {
		return "64-bit"
	}
	if regis3.Is64BitOperatingSystem() {
		return "32-bit process on 64-bit OS"
	}
	return "32-bit"
}

func printUsage() {
	fmt.Printf("REGDIFF - Version %s\n", Version)
	fmt.Printf("Freeware written by Gerson Kurz (http://p-nand-q.com) [%s]\n", getProcessType())
	fmt.Println()
	fmt.Println("Usage: regdiff [OPTIONS] FILE|HKEY_* {FILE|HKEY_*}")
	fmt.Println()
	fmt.Println("Options:")
	fmt.Println("  /MERGE:<file>      Create merged output file")
	fmt.Println("  /DIFF:<file>       Create diff output file")
	fmt.Println("  /QUIET             Don't show diff on console")
	fmt.Println("  /NO-EMPTY-KEYS     Don't create empty keys in output")
	fmt.Println("  /4                 Use REGEDIT4 format (ANSI, non-unicode)")
	fmt.Println("  /COMMENTS          Allow # and ; line comments in input")
	fmt.Println("  /ALIAS:FOO=BAR     Alias key names for comparison (repeatable)")
	fmt.Println("  /PARAMS:<file>     Parameter file for $$VAR$$ substitution (.ini)")
	fmt.Println("  /REGISTRY          Compare input file against live registry")
	fmt.Println("  /WRITE             Write result to registry (Windows only)")
	fmt.Println("  /ALLACCESS         Grant all access when writing (use with /WRITE)")

	// Show registry view option based on process type
	if regis3.Is64BitProcess() {
		fmt.Println("  /32                Use 32-bit registry view (default: 64-bit)")
	} else if regis3.Is64BitOperatingSystem() {
		fmt.Println("  /64                Use 64-bit registry view (default: 32-bit)")
	}
	// On 32-bit OS, no view switching is available

	fmt.Println("  /? or /HELP        Show this help")
	fmt.Println()
	fmt.Println("Examples:")
	fmt.Println("  regdiff file1.reg file2.reg                    Compare two .REG files")
	fmt.Println("  regdiff file1.reg file2.reg /DIFF:changes.reg  Create diff file")
	fmt.Println("  regdiff HKEY_CURRENT_USER\\Software /MERGE:out.reg  Export registry key")
	fmt.Println("  regdiff settings.reg /REGISTRY                 Compare file with registry")
	fmt.Println("  regdiff settings.reg /WRITE                    Apply .REG file to registry")
}
