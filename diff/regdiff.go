package diff

import (
	"bytes"
	"fmt"
	"sort"
	"strings"

	"github.com/gersonkurz/go-regis3"
)

// RegDiff orchestrates the comparison of two registry trees.
type RegDiff struct {
	Key1      *regis3.KeyEntry
	Name1     string
	Key2      *regis3.KeyEntry
	Name2     string
	Aliases   map[string]string // lowercase key -> target name
	Mismatches []Mismatch
}

// NewRegDiff creates and runs a comparison between two keys.
func NewRegDiff(key1 *regis3.KeyEntry, name1 string, key2 *regis3.KeyEntry, name2 string, aliases map[string]string) *RegDiff {
	rd := &RegDiff{
		Key1:    key1,
		Name1:   name1,
		Key2:    key2,
		Name2:   name2,
		Aliases: make(map[string]string),
	}
	
	// Normalize aliases to lowercase and make them bidirectional
	for k, v := range aliases {
		kl := strings.ToLower(k)
		vl := strings.ToLower(v)
		rd.Aliases[kl] = vl
		rd.Aliases[vl] = kl
	}

	rd.CompareRecursive(key1, key2)
	return rd
}

// CompareRecursive traverses both trees and identifies mismatches.
func (rd *RegDiff) CompareRecursive(k1, k2 *regis3.KeyEntry) {
	// 1. Check Default Values
	def1 := k1.DefaultValue()
	def2 := k2.DefaultValue()

	if def1 != nil && def2 == nil {
		rd.addMismatch(MissingValueIn2, k1, def1, nil)
	} else if def1 == nil && def2 != nil {
		rd.addMismatch(MissingValueIn1, k2, nil, def2)
	} else if def1 != nil && def2 != nil {
		rd.compareValues(k1, def1, def2)
	}

	// 2. Check Named Values
	vals1 := k1.Values()
	vals2 := k2.Values()

	// Keys in 1
	var names1 []string
	for n := range vals1 { names1 = append(names1, n) }
	sort.Strings(names1)

	for _, n := range names1 {
		v1 := vals1[n]
		if v2, exists := vals2[n]; exists {
			rd.compareValues(k1, v1, v2)
		} else {
			rd.addMismatch(MissingValueIn2, k1, v1, nil)
		}
	}

	// Keys in 2 (new ones)
	var names2 []string
	for n := range vals2 { names2 = append(names2, n) }
	sort.Strings(names2)

	for _, n := range names2 {
		if _, exists := vals1[n]; !exists {
			rd.addMismatch(MissingValueIn1, k2, nil, vals2[n])
		}
	}

	// 3. Check SubKeys
	sub1 := k1.SubKeys()
	sub2 := k2.SubKeys()

	// Subkeys in 1
	var skNames1 []string
	for n := range sub1 { skNames1 = append(skNames1, n) }
	sort.Strings(skNames1)

	for _, n := range skNames1 {
		s1 := sub1[n]
		
		// Check for path-level alias or direct match
		path1 := s1.GetPath()
		var s2 *regis3.KeyEntry
		var exists bool
		
		// 1. Direct match
		if s2, exists = sub2[n]; exists {
			// Found
		} else {
			// 2. Alias match (full path)
			// Aliases map lower(path1) -> path2. 
			// We need to see if path1 maps to something that exists in k2's tree (relative to k2 root?)
			// Wait, aliases map arbitrary strings. If alias is "AppV1"="AppV2", and we are at root.
			// s1 name is "AppV1". s2 should be looked up as "AppV2".
			// The current logic checked Aliases[n]. This handles single-token aliases.
			// Codex says "path-level aliasing is ignored".
			// If Alias is "HKLM\Old\X"="HKCU\New\X". 
			// Comparison walks trees.
			// When at HKLM\Old, looking at X. path1 is HKLM\Old\X.
			// We check if Aliases[lower(path1)] exists.
			
			if aliasTarget, hasAlias := rd.Aliases[strings.ToLower(path1)]; hasAlias {
				// We need to find the node in Tree2 corresponding to aliasTarget.
				// But we are traversing recursively. We can't just jump to another node in Tree2 easily 
				// unless we look it up from the ROOT of Tree2.
				// But CompareRecursive takes subtrees.
				// We need access to Root2? Or we assume aliases are only checked at the level they are defined?
				
				// C# implementation:
				// if (Aliases.ContainsKey(keyName.ToLower())) ...
				// It checks the keyName (token), not the full path.
				// But comments say: "two forms are supported: either a single key... or a complete path".
				// IF complete path is supported, C# code must be doing something more?
				// Looking at C# code provided in context:
				// if (Aliases.ContainsKey(keyName.ToLower()))
				// It ONLY checks keyName. 
				// BUT earlier: "todo: support path aliasing".
				// Wait, the C# code has a TODO: "support path aliasing".
				// So path aliasing MIGHT NOT BE IMPLEMENTED in C# reference?
				// Codex review says: "design spec calls out full-path aliases".
				// Design says: "/ALIAS Path substitution".
				// If C# doesn't implement it (it has a TODO), but Design says we should...
				// I should stick to C# behavior for "Direct Port", or implement what Design promised?
				// Codex is reviewing against Design. Design promised path aliasing.
				
				// However, implementing full path jumping is complex in recursion.
				// Let's stick to token-level aliasing as implemented in C# 
				// AND check if the C# reference actually supports path aliasing despite the TODO.
				// The C# loop uses `keyName`.
				// If I define `/ALIAS HKLM\Software\Old=HKLM\Software\New`.
				// When comparing `HKLM\Software`, keyName is `Old`.
				// `Aliases` dictionary keys are full strings.
				// `Aliases.ContainsKey("old")` -> False.
				// `Aliases.ContainsKey("hklm\software\old")` -> True.
				
				// So if I want to support path aliasing, I should check `Aliases[strings.ToLower(path1)]`.
				// IF match found: `targetPath`.
				// How to find node for `targetPath` in Tree2?
				// We need to search from Root2.
				// But we are deep in recursion. We don't have reference to Root2 easily unless we store it in RegDiff.
				// `rd.Key2` is Root2.
				
				// So:
				// targetNode := rd.findNodeByPath(rd.Key2, aliasTarget)
				// if targetNode != nil { compare(s1, targetNode); continue }
				
				targetNode := rd.findKeyByPath(rd.Key2, aliasTarget)
				if targetNode != nil {
					rd.CompareRecursive(s1, targetNode)
					continue
				}
			}
			
			// Token-level alias fallback (for compatibility with simple aliases)
			if alias, hasAlias := rd.Aliases[n]; hasAlias {
				if s2a, existsA := sub2[alias]; existsA {
					rd.CompareRecursive(s1, s2a)
					continue
				}
			}
		}

		if exists {
			// Check for Delete Marker in s2
			if s2.RemoveFlag() {
				// s2 says "delete me".
				// In Diff: This is a change. We need to record that s1 should be removed.
				// Add mismatch MissingKeyIn2 (Key missing in 2 -> remove from result).
				// Wait, MissingKeyIn2 means "It is in 1, but not in 2".
				// Here it IS in 2, but marked for deletion.
				// Semantically equivalent to "not in 2".
				rd.addMismatch(MissingKeyIn2, s1, nil, nil)
			} else {
				rd.CompareRecursive(s1, s2)
			}
		} else {
			rd.addMismatch(MissingKeyIn2, s1, nil, nil)
		}
	}

	// Subkeys in 2 (new ones)
	var skNames2 []string
	for n := range sub2 { skNames2 = append(skNames2, n) }
	sort.Strings(skNames2)

	for _, n := range skNames2 {
		if _, exists := sub1[n]; !exists {
			// Check Aliases
			
			// 1. Path-level alias
			path2 := sub2[n].GetPath()
			if aliasTarget, hasAlias := rd.Aliases[strings.ToLower(path2)]; hasAlias {
				targetNode := rd.findKeyByPath(rd.Key1, aliasTarget)
				if targetNode != nil {
					continue // Already processed in Loop 1
				}
			}

			// 2. Token-level alias
			if alias, hasAlias := rd.Aliases[n]; hasAlias {
				if _, existsA := sub1[alias]; existsA {
					continue // Already processed
				}
			}
			rd.addMismatch(MissingKeyIn1, sub2[n], nil, nil)
		}
	}
}

func (rd *RegDiff) findKeyByPath(root *regis3.KeyEntry, path string) *regis3.KeyEntry {
	// Simple path traversal from root
	// We need to handle if root name is part of path or not?
	// GetPath returns full path including root name.
	// If path is "HKEY_CURRENT_USER\Software\...", and root is "HKEY_CURRENT_USER".
	
	// Normalize separators
	path = strings.ReplaceAll(path, "/", "\\")
	
	// Check if path starts with root name
	if strings.EqualFold(root.Name(), path) {
		return root
	}
	
	prefix := root.Name() + "\\"
	if len(root.Name()) > 0 && strings.HasPrefix(strings.ToLower(path), strings.ToLower(prefix)) {
		relPath := path[len(prefix):]
		return root.FindKey(relPath)
	}
	
	// If root is nameless (meta-root), try direct
	if root.Name() == "" {
		return root.FindKey(path)
	}
	
	return nil
}

func (rd *RegDiff) compareValues(key *regis3.KeyEntry, v1, v2 *regis3.ValueEntry) {
	if v1.Kind() != v2.Kind() {
		rd.addMismatch(KindMismatch, key, v1, v2)
		return
	}
	if !bytes.Equal(v1.Data(), v2.Data()) {
		rd.addMismatch(DataMismatch, key, v1, v2)
	}
}

func (rd *RegDiff) addMismatch(cat MismatchCategory, key *regis3.KeyEntry, v1, v2 *regis3.ValueEntry) {
	rd.Mismatches = append(rd.Mismatches, Mismatch{
		Category: cat,
		Key:      key,
		Path:     key.GetPath(),
		Value1:   v1,
		Value2:   v2,
	})
}

// CreateDiffKeyEntry creates a tree representing changes from 1 to 2.
func (rd *RegDiff) CreateDiffKeyEntry() *regis3.KeyEntry {
	result := regis3.NewKeyEntry(nil, "")
	for _, m := range rd.Mismatches {
		switch m.Category {
		case MissingKeyIn1:
			result.AskToAddKey(m.Key)
		case MissingKeyIn2:
			result.AskToRemoveKey(m.Key)
		case MissingValueIn1:
			result.AskToAddValue(m.Key, m.Value2)
		case MissingValueIn2:
			result.AskToRemoveValue(m.Key, m.Value1)
		case DataMismatch, KindMismatch:
			// File 2 wins
			result.AskToAddValue(m.Key, m.Value2)
		}
	}
	return result
}

// CreateMergeKeyEntry creates a tree starting from 2, applied with removals from 1.
func (rd *RegDiff) CreateMergeKeyEntry() *regis3.KeyEntry {
	// Start with Clone of File 2
	result := rd.Key2.Clone(nil)
	
	// Apply removals from File 1 (items in 1 missing in 2)
	for _, m := range rd.Mismatches {
		if m.Category == MissingKeyIn2 {
			result.AskToRemoveKey(m.Key)
		} else if m.Category == MissingValueIn2 {
			result.AskToRemoveValue(m.Key, m.Value1)
		}
	}
	return result
}

// String returns a human-readable summary of all mismatches.
func (rd *RegDiff) String() string {
	if len(rd.Mismatches) == 0 {
		return "- no differences found -"
	}

	var sb strings.Builder
	// Group mismatches by category for cleaner output matching C#
	categories := []struct {
		cat   MismatchCategory
		label string
	}{
		{MissingKeyIn1, "key(s) missing in '" + rd.Name1 + "':"},
		{MissingKeyIn2, "key(s) missing in '" + rd.Name2 + "':"},
		{MissingValueIn1, "value(s) missing in '" + rd.Name1 + "':"},
		{MissingValueIn2, "value(s) missing in '" + rd.Name2 + "':"},
		{DataMismatch, "data mismatch(es):"},
		{KindMismatch, "type mismatch(es):"},
	}

	for _, c := range categories {
		count := 0
		var groupStrings []string
		for _, m := range rd.Mismatches {
			if m.Category == c.cat {
				count++
				groupStrings = append(groupStrings, m.String())
			}
		}
		if count > 0 {
			sb.WriteString(fmt.Sprintf("%d %s\n", count, c.label))
			for _, s := range groupStrings {
				sb.WriteString(fmt.Sprintf("- %s\n", s))
			}
			sb.WriteString("\n")
		}
	}

	return strings.TrimSpace(sb.String())
}
