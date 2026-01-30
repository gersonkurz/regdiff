package diff

import (
	"strings"
	"testing"

	"github.com/gersonkurz/go-regis3"
)

func TestApplyParams(t *testing.T) {
	input := `Windows Registry Editor Version 5.00

[HKEY_CURRENT_USER\Software\Test]
"Path"="$$INSTALLDIR$$\\Bin"
"Mixed"="$$mixed_case$$"
"Multi"=hex(7):41,00,42,00,00,00,24,00,24,00,56,00,41,00,52,00,24,00,24,00,00,00,00,00
`
	// Multi value decodes to "AB", "$$VAR$$"
	
	key, err := regis3.Parse(input, 0)
	if err != nil {
		t.Fatalf("Parse failed: %v", err)
	}
	
	params := map[string]string{
		"INSTALLDIR": "C:\\Program Files",
		"VAR":        "Replaced",
		"MIXED_CASE": "Works",
	}
	
	ApplyParams(key, params)
	
	sub := key.FindKey("HKEY_CURRENT_USER\\Software\\Test")
	if sub == nil {
		t.Fatal("Subkey not found")
	}
	
	// Check Path
	val := sub.Values()["path"]
	if got := val.GetString(""); got != "C:\\Program Files\\Bin" {
		t.Errorf("Path mismatch. Got %q", got)
	}

	// Check Mixed Case (Source is lowercase, Param is uppercase)
	valMixed := sub.Values()["mixed"]
	if got := valMixed.GetString(""); got != "Works" {
		t.Errorf("Mixed case mismatch. Got %q", got)
	}
	
	// Check Multi
	valMulti := sub.Values()["multi"]
	strs := valMulti.GetMultiString()
	if len(strs) != 2 || strs[1] != "Replaced" {
		t.Errorf("Multi mismatch: %v", strs)
	}
}

func TestApplyParamsEdgeCases(t *testing.T) {
	input := `Windows Registry Editor Version 5.00

[HKEY_CURRENT_USER\Software\Edges]
@="$$DEFAULT$$"
"Escaped"=dword:$$VAR$$
`
	key, err := regis3.Parse(input, regis3.AllowVariableNamesForNonStringVariables)
	if err != nil {
		t.Fatalf("Parse failed: %v", err)
	}
	
	params := map[string]string{
		"DEFAULT": "DefaultValue",
		"VAR":     "1",
	}
	
	ApplyParams(key, params)
	
	sub := key.FindKey("HKEY_CURRENT_USER\\Software\\Edges")
	
	// Check Default Value
	def := sub.DefaultValue()
	if def == nil {
		t.Fatal("Default value missing")
	}
	if got := def.GetString(""); got != "DefaultValue" {
		t.Errorf("Default value mismatch. Got %q", got)
	}
	
	// Check Escaped DWORD (remains escaped string for now, but content replaced)
	esc := sub.Values()["escaped"]
	if esc == nil {
		t.Fatal("Escaped value missing")
	}
	if got := esc.GetString(""); got != "1" {
		t.Errorf("Escaped value mismatch. Got %q", got)
	}
}

func TestLoadParamsINI(t *testing.T) {
	ini := `
; comment
[Section1]
KEY1=Val1
[Section2]
  key2 =  Val 2  
`
	r := strings.NewReader(ini)
	params, err := loadParamsINI(r)
	if err != nil {
		t.Fatalf("loadParamsINI failed: %v", err)
	}
	
	if params["KEY1"] != "Val1" {
		t.Errorf("KEY1 mismatch: %q", params["KEY1"])
	}
	if params["KEY2"] != "Val 2" {
		t.Errorf("KEY2 mismatch: %q", params["KEY2"])
	}
}

func TestLoadParamsXML(t *testing.T) {
	xmlData := `
<parameters>
	<value name="KEY1">Val1</value>
	<value name="key2">Val 2</value>
</parameters>
`
	r := strings.NewReader(xmlData)
	params, err := loadParamsXML(r)
	if err != nil {
		t.Fatalf("loadParamsXML failed: %v", err)
	}
	
	if params["KEY1"] != "Val1" {
		t.Errorf("KEY1 mismatch: %q", params["KEY1"])
	}
	if params["KEY2"] != "Val 2" {
		t.Errorf("KEY2 mismatch: %q", params["KEY2"])
	}
}

