package diff

import (
	"fmt"
	"strings"

	"github.com/gersonkurz/go-regis3"
)

// MismatchCategory identifies the type of difference found
type MismatchCategory int

const (
	MissingKeyIn1 MismatchCategory = iota
	MissingKeyIn2
	MissingValueIn1
	MissingValueIn2
	DataMismatch
	KindMismatch
)

// Mismatch represents a single difference between two registry trees.
type Mismatch struct {
	Category MismatchCategory
	Key      *regis3.KeyEntry
	Path     string
	Value1   *regis3.ValueEntry
	Value2   *regis3.ValueEntry
}

// String returns a human-readable description of the mismatch.
func (m Mismatch) String() string {
	switch m.Category {
	case MissingKeyIn1:
		return fmt.Sprintf("Key missing in 1: %s", m.Path)
	case MissingKeyIn2:
		return fmt.Sprintf("Key missing in 2: %s", m.Path)
	case MissingValueIn1:
		return fmt.Sprintf("Value missing in 1: %s [%s]", m.Path, m.valueName())
	case MissingValueIn2:
		return fmt.Sprintf("Value missing in 2: %s [%s]", m.Path, m.valueName())
	case DataMismatch:
		return m.formatDataMismatch()
	case KindMismatch:
		return fmt.Sprintf("Type mismatch: %s [%s] (%d <> %d)", m.Path, m.valueName(), m.Value1.Kind(), m.Value2.Kind())
	}
	return "Unknown mismatch"
}

// formatDataMismatch provides detailed byte-level comparison for data mismatches
func (m Mismatch) formatDataMismatch() string {
	var sb strings.Builder
	sb.WriteString(fmt.Sprintf("Data mismatch: %s [%s]", m.Path, m.valueName()))

	if m.Value1 == nil || m.Value2 == nil {
		return sb.String()
	}

	data1 := m.Value1.Data()
	data2 := m.Value2.Data()

	// For string types, show string comparison
	if regis3.IsStringType(m.Value1.Kind()) && regis3.IsStringType(m.Value2.Kind()) {
		str1 := m.Value1.GetString("")
		str2 := m.Value2.GetString("")
		sb.WriteString(fmt.Sprintf("\n    Value 1: %q", str1))
		sb.WriteString(fmt.Sprintf("\n    Value 2: %q", str2))
		return sb.String()
	}

	// For DWORD, show numeric values
	if m.Value1.Kind() == regis3.RegDword && m.Value2.Kind() == regis3.RegDword {
		sb.WriteString(fmt.Sprintf("\n    Value 1: 0x%08x (%d)", m.Value1.GetDword(0), m.Value1.GetDword(0)))
		sb.WriteString(fmt.Sprintf("\n    Value 2: 0x%08x (%d)", m.Value2.GetDword(0), m.Value2.GetDword(0)))
		return sb.String()
	}

	// For QWORD, show numeric values
	if m.Value1.Kind() == regis3.RegQword && m.Value2.Kind() == regis3.RegQword {
		sb.WriteString(fmt.Sprintf("\n    Value 1: 0x%016x (%d)", m.Value1.GetQword(0), m.Value1.GetQword(0)))
		sb.WriteString(fmt.Sprintf("\n    Value 2: 0x%016x (%d)", m.Value2.GetQword(0), m.Value2.GetQword(0)))
		return sb.String()
	}

	// For binary/other types, show byte-level comparison
	len1 := len(data1)
	len2 := len(data2)

	if len1 != len2 {
		sb.WriteString(fmt.Sprintf("\n    Size differs: %d bytes vs %d bytes", len1, len2))
	}

	// Find first differing position
	minLen := len1
	if len2 < minLen {
		minLen = len2
	}

	firstDiff := -1
	for i := 0; i < minLen; i++ {
		if data1[i] != data2[i] {
			firstDiff = i
			break
		}
	}

	if firstDiff == -1 && len1 != len2 {
		// Data matches up to the shorter length, difference is in length
		firstDiff = minLen
	}

	if firstDiff >= 0 {
		sb.WriteString(fmt.Sprintf("\n    First difference at offset %d (0x%x)", firstDiff, firstDiff))

		// Show bytes around the difference
		start := firstDiff - 4
		if start < 0 {
			start = 0
		}
		end := firstDiff + 12
		if end > len1 {
			end = len1
		}

		if end > start {
			sb.WriteString(fmt.Sprintf("\n    Value 1 [%d:%d]: %s", start, end, formatBytes(data1[start:end])))
		}

		end = firstDiff + 12
		if end > len2 {
			end = len2
		}
		if end > start {
			sb.WriteString(fmt.Sprintf("\n    Value 2 [%d:%d]: %s", start, end, formatBytes(data2[start:end])))
		}
	}

	return sb.String()
}

// formatBytes formats a byte slice as hex
func formatBytes(data []byte) string {
	if len(data) == 0 {
		return "(empty)"
	}
	parts := make([]string, len(data))
	for i, b := range data {
		parts[i] = fmt.Sprintf("%02x", b)
	}
	return strings.Join(parts, " ")
}

func (m Mismatch) valueName() string {
	if m.Value1 != nil {
		if m.Value1.IsDefaultValue() {
			return "(Default)"
		}
		return m.Value1.Name()
	}
	if m.Value2 != nil {
		if m.Value2.IsDefaultValue() {
			return "(Default)"
		}
		return m.Value2.Name()
	}
	return ""
}
