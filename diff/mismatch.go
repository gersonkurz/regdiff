package diff

import (
	"fmt"
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
		return fmt.Sprintf("Data mismatch: %s [%s]", m.Path, m.valueName())
	case KindMismatch:
		return fmt.Sprintf("Type mismatch: %s [%s] (%d <> %d)", m.Path, m.valueName(), m.Value1.Kind(), m.Value2.Kind())
	}
	return "Unknown mismatch"
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
