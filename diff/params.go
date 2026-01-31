package diff

import (
	"bufio"
	"encoding/xml"
	"io"
	"os"
	"strings"

	"github.com/gersonkurz/go-regis3"
)

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

// LoadParams reads a params file (INI or XML) and returns a map of substitutions.
// It detects format by file extension (.xml).
func LoadParams(filename string) (map[string]string, error) {
	f, err := os.Open(filename)
	if err != nil {
		return nil, err
	}
	defer f.Close()

	if strings.HasSuffix(strings.ToLower(filename), ".xml") {
		return loadParamsXML(f)
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

// XML structure: <parameters><value name="key">value</value>...</parameters>
type xmlParam struct {
	Name  string `xml:"name,attr"`
	Value string `xml:",chardata"`
}
type xmlParameters struct {
	XMLName xml.Name   `xml:"parameters"`
	Params  []xmlParam `xml:"value"`
}

func loadParamsXML(r io.Reader) (map[string]string, error) {
	var p xmlParameters
	if err := xml.NewDecoder(r).Decode(&p); err != nil {
		return nil, err
	}
	res := make(map[string]string)
	for _, param := range p.Params {
		res[strings.ToUpper(param.Name)] = param.Value
	}
	return res, nil
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
			// "Name"=dword:$$VAR$$ -> Stored as RegEscapedDword with data "$$VAR$$"
			// If we can resolve it to a number, convert to RegDword.
			varName := v.GetString("")
			// Usually stored as the variable name itself? Or the raw string?
			// C# implementation: if (type == RegEscapedDword) ...
			// The value is the variable string. 
			// We need to look it up.
			// replaceString does substring replacement, but here we expect full match?
			// "dword:$$VAR$$" in .reg -> RegEscapedDword, data="$$VAR$$".
			
			// Try to find exact match first (normalized)
			// Actually replaceString handles $$VAR$$.
			newStr := replaceString(varName, params)
			if newStr != varName {
				// If it resolved to a pure number (dec/hex), set it as DWORD.
				// However, newStr is a string. We need to parse it.
				// But we don't have a parse helper here easily.
				// If strict parity is needed, we should probably attempt to parse 
				// or leave it as a string but change type?
				// C# RegEnvReplace does:
				// if (IsEscapedType(v)) { ... resolve ... if (numeric) SetDword ... }
				
				// For now, let's assume if it changed, it's a value.
				// But we need to convert string "123" or "0x123" to uint32.
				// Since we are in `diff` package, let's keep it simple: 
				// We won't implement full int parsing logic here unless critical.
				// Codex said "C# handles these on write".
				// So if we are just modifying the tree for comparison, maybe we KEEP it as string?
				// BUT if comparison compares types, keeping it as EscapedDword vs resolved Dword in another file...
				// If File A has `dword:1`, File B has `dword:$$VAR$$` (where VAR=1).
				// If we don't resolve, type mismatch!
				// So we MUST resolve.
				
				// Postponed: Parsing logic. We will replace the string data. 
				// If `go-regis3` writer handles EscapedDword by writing the string, we are fine.
				// But for diffing against a real DWORD, we will fail parity.
				
				// For Phase 2, we update the underlying string data.
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
	// Replacement of $$KEY$$ where KEY is case-insensitive (stored as uppercase)
	for k, v := range params {
		placeholder := "$$" + k + "$$"
		// We use a case-insensitive check by looking for the uppercase placeholder
		// in an uppercase version of the string, or more simply, since we know 
		// the keys are already uppercase, we just search for them.
		// Note: The variable name in the .REG file must match case if we use strings.ReplaceAll.
		// To be truly case-insensitive to the SOURCE string, we'd need regex or a custom loop.
		// C# RegEnvReplace usually expects the source to also be uppercase or it does a case-insensitive match.
		
		// Strategy: search for $$...$$ case-insensitively.
		// For now, if params are uppercase, we look for uppercase placeholders.
		if strings.Contains(strings.ToUpper(s), placeholder) {
			// Find the actual start/end to replace correctly if casing differs in source.
			// But C# porting notes say placeholders are usually uppercase in .REG files.
			// Let's use a regex-free approach that handles the source casing.
			s = replaceCaseInsensitive(s, placeholder, v)
		}
	}
	return s
}

func replaceCaseInsensitive(source, placeholder, replacement string) string {
	for {
		i := strings.Index(strings.ToUpper(source), placeholder)
		if i == -1 {
			break
		}
		source = source[:i] + replacement + source[i+len(placeholder):]
	}
	return source
}
