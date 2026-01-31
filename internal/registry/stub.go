//go:build !windows

package registry

import (
	"errors"
	"github.com/gersonkurz/go-regis3"
)

type Key = uintptr

const (
	AccessRead      = 0
	AccessWrite     = 0
	AccessAll       = 0
	View32          = 0
	View64          = 0
)

var ErrNotSupported = errors.New("registry operations only supported on windows")

func ParseRegistry(root Key, path string, access uint32) (*regis3.KeyEntry, error) {
	return nil, ErrNotSupported
}

func WriteRegistry(root Key, path string, key *regis3.KeyEntry, access uint32) error {
	return ErrNotSupported
}

func LoadLiveRegistry(liveRoot *regis3.KeyEntry, fileKey *regis3.KeyEntry, access uint32) error {
	return ErrNotSupported
}

func WriteToRegistry(key *regis3.KeyEntry, access uint32) error {
	return ErrNotSupported
}

func ReadRegistryPath(path string, access uint32) (*regis3.KeyEntry, error) {
	return nil, ErrNotSupported
}
