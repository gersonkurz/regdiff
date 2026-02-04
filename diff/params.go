package diff

import (
	"bufio"
	"errors"
	"io"
	"os"
	"regexp"
	"strings"

	"github.com/gersonkurz/go-regis3"
)

var rePlaceholder = regexp.MustCompile(`(?i)\$\$(.*?)\$\$`)

// MergeEnvironmentVariables adds all OS environment variables to the params map.
// Existing params take precedence over environment variables.
func MergeEnvironmentVariables(params map[string]string) {
	for _, env := range os.Environ() {
		parts := strings.SplitN(env, "=", 2)
		if len(parts) == 2 {
			key := strings.ToUpper(parts[0])
			// Don't override existing params
			if _, exists := params[key]; !exists {
				params[key] = parts[1]
			}
		}
	}
}

// LoadParams reads a params file (INI) and returns a map of substitutions.
func LoadParams(filename string) (map[string]string, error) {
	f, err := os.Open(filename)
	if err != nil {
		return nil, err
	}
	defer f.Close()

	if strings.HasSuffix(strings.ToLower(filename), ".xml") {
		return nil, errors.New("XML params are not supported; use .ini")
	}
	return loadParamsINI(f)
}

func loadParamsINI(r io.Reader) (map[string]string, error) {
	params := make(map[string]string)
	scanner := bufio.NewScanner(r)
	for scanner.Scan() {
		line := strings.TrimSpace(scanner.Text())
		// Skip empty lines and comments
		if line == "" || strings.HasPrefix(line, ";") || strings.HasPrefix(line, "#") {
			continue
		}
		// Skip section headers [SECTION]
		if strings.HasPrefix(line, "[") && strings.HasSuffix(line, "]") {
			continue
		}

		parts := strings.SplitN(line, "=", 2)
		if len(parts) == 2 {
			key := strings.TrimSpace(parts[0])
			val := strings.TrimSpace(parts[1])
			// Normalize keys to uppercase for case-insensitive matching
			params[strings.ToUpper(key)] = val
		}
	}
	return params, scanner.Err()
}

// ApplyParams traverses the registry tree and replaces $$VAR$$ with values from params.
func ApplyParams(key *regis3.KeyEntry, params map[string]string) {
	// 1. Process Values
	applyToValue := func(v *regis3.ValueEntry) {
		if v == nil {
			return
		}
		switch v.Kind() {
		case regis3.RegSz:
			str := v.GetString("")
			newStr := replaceString(str, params)
			if str != newStr {
				v.SetString(newStr)
			}
		case regis3.RegExpandSz:
			str := v.GetString("")
			newStr := replaceString(str, params)
			if str != newStr {
				v.SetExpandString(newStr)
			}
		case regis3.RegMultiSz:
			strs := v.GetMultiString()
			changed := false
			newStrs := make([]string, len(strs))
			for i, s := range strs {
				newS := replaceString(s, params)
				newStrs[i] = newS
				if newS != s {
					changed = true
				}
			}
			if changed {
				v.SetMultiString(newStrs)
			}
		case regis3.RegEscapedDword:
			varName := v.GetString("")
			// Keep escaped numeric values as strings; only substitute variables.
			newStr := replaceString(varName, params)
			if newStr != varName {
				v.SetEscapedDwordValue(newStr)
			}

		case regis3.RegEscapedQword:
			varName := v.GetString("")
			newStr := replaceString(varName, params)
			if newStr != varName {
				v.SetEscapedQwordValue(newStr)
			}
		}
	}

	for _, v := range key.Values() {
		applyToValue(v)
	}

	// Handle Default Value
	applyToValue(key.DefaultValue())

	// 2. Process Subkeys
	for _, sk := range key.SubKeys() {
		ApplyParams(sk, params)
	}
}

func replaceString(s string, params map[string]string) string {
	if len(params) == 0 {
		return s
	}
	return rePlaceholder.ReplaceAllStringFunc(s, func(match string) string {
		// Extract key between $$ and $$
		key := strings.ToUpper(match[2 : len(match)-2])
		if val, ok := params[key]; ok {
			return val
		}
		return match
	})
}
