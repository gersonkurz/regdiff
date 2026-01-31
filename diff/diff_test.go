package diff

import (
	"testing"

	"github.com/gersonkurz/go-regis3"
)

func TestDiff(t *testing.T) {
	// A: Base
	inputA := `Windows Registry Editor Version 5.00

[HKEY_CURRENT_USER\Software\Test]
"Same"="Value"
"ChangeMe"="Old"
"DeleteMe"="Bye"
`
	// B: New State
	inputB := `Windows Registry Editor Version 5.00

[HKEY_CURRENT_USER\Software\Test]
"Same"="Value"
"ChangeMe"="New"
"NewValue"="Hello"
`
	rootA, _ := regis3.Parse(inputA, nil)
	rootB, _ := regis3.Parse(inputB, nil)
	
	rd := NewRegDiff(rootA, "File1", rootB, "File2", nil)
	diff := rd.CreateDiffKeyEntry()
	
	// Verify
	key := diff.FindOrCreateKey("HKEY_CURRENT_USER\\Software\\Test")
	
	// Same should NOT be in diff
	if _, exists := key.Values()["same"]; exists {
		t.Error("Unchanged value 'Same' should not be in diff")
	}
	
	// ChangeMe should be New
	if val := key.Values()["changeme"]; val == nil || val.GetString("") != "New" {
		t.Error("ChangeMe should be 'New'")
	}
	
	// NewValue should be Hello
	if val := key.Values()["newvalue"]; val == nil || val.GetString("") != "Hello" {
		t.Error("NewValue should be 'Hello'")
	}
	
	// DeleteMe should be removed
	if val := key.Values()["deleteme"]; val == nil || !val.RemoveFlag() {
		t.Error("DeleteMe should be marked for removal")
	}
}

func TestMerge(t *testing.T) {
	// A: Base
	inputA := `Windows Registry Editor Version 5.00

[HKEY_CURRENT_USER\Software\Merge]
"Base"="A"
"Conflict"="A"
"ToRemove"="A"
`
	// B: Overlay
	inputB := `Windows Registry Editor Version 5.00

[HKEY_CURRENT_USER\Software\Merge]
"Conflict"="B"
"New"="B"
`
	rootA, _ := regis3.Parse(inputA, nil)
	rootB, _ := regis3.Parse(inputB, nil)
	
	rd := NewRegDiff(rootA, "File1", rootB, "File2", nil)
	result := rd.CreateMergeKeyEntry()
	
	key := result.FindOrCreateKey("HKEY_CURRENT_USER\\Software\\Merge")
	
	// Conflict should be B
	if val := key.Values()["conflict"]; val == nil || val.GetString("") != "B" {
		t.Errorf("Conflict value should be B, got '%v'", key.Values()["conflict"])
	}
	
	// New should be present
	if val := key.Values()["new"]; val == nil || val.GetString("") != "B" {
		t.Error("New value missing")
	}

	// ToRemove should be marked for removal (sync behavior)
	if val := key.Values()["toremove"]; val == nil || !val.RemoveFlag() {
		t.Error("ToRemove value should be marked for removal in merge (sync mode)")
	}
}

func TestAliases(t *testing.T) {
	inputA := `Windows Registry Editor Version 5.00

[HKEY_CURRENT_USER\Software\AppV1]
"Config"="Old"
`
	inputB := `Windows Registry Editor Version 5.00

[HKEY_CURRENT_USER\Software\AppV2]
"Config"="Old"
`
	rootA, _ := regis3.Parse(inputA, nil)
	rootB, _ := regis3.Parse(inputB, nil)
	
	aliases := map[string]string{"AppV1": "AppV2"}
	rd := NewRegDiff(rootA, "F1", rootB, "F2", aliases)
	
	// Should find no differences because of alias
	for _, m := range rd.Mismatches {
		t.Errorf("Unexpected mismatch found with alias: %s", m)
	}
}

func TestPathAliases(t *testing.T) {
	inputA := `Windows Registry Editor Version 5.00

[HKEY_CURRENT_USER\Software\OldPath\Sub]
"Val"="Same"
`
	inputB := `Windows Registry Editor Version 5.00

[HKEY_CURRENT_USER\Software\NewPath\Sub]
"Val"="Same"
`
	rootA, _ := regis3.Parse(inputA, nil)
	rootB, _ := regis3.Parse(inputB, nil)
	
	// Alias full path
	aliases := map[string]string{
		"HKEY_CURRENT_USER\\Software\\OldPath": "HKEY_CURRENT_USER\\Software\\NewPath",
	}
	rd := NewRegDiff(rootA, "F1", rootB, "F2", aliases)
	
	if len(rd.Mismatches) > 0 {
		t.Errorf("Expected 0 mismatches with path alias, got %d", len(rd.Mismatches))
		for _, m := range rd.Mismatches {
			t.Logf("Mismatch: %s", m)
		}
	}
}