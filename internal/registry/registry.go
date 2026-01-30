//go:build windows

package registry

import (
	"fmt"
	"strings"

	"github.com/gersonkurz/go-regis3"
	"golang.org/x/sys/windows"
	"golang.org/x/sys/windows/registry"
)

// RegistryOptions controls how the registry is accessed.
type RegistryOptions struct {
	Access   uint32 // e.g. KEY_READ, KEY_WRITE, KEY_WOW64_32KEY
	RootKey  registry.Key
	RootPath string // e.g. "Software\MyApp"
}

// ParseRegistry reads a live registry key and returns it as a KeyEntry tree.
func ParseRegistry(root registry.Key, path string, access uint32) (*regis3.KeyEntry, error) {
	k, err := registry.OpenKey(root, path, access|registry.READ)
	if err != nil {
		return nil, fmt.Errorf("failed to open key %s: %w", path, err)
	}
	defer k.Close()

	// Create the root KeyEntry
	// Note: The name of the returned key should probably match the leaf of the path
	parts := strings.Split(path, "\\")
	name := parts[len(parts)-1]
	if name == "" {
		name = rootKeyName(root)
	}
	
	keyEntry := regis3.NewKeyEntry(nil, name)
	
	if err := walkRegistry(k, keyEntry, access); err != nil {
		return nil, err
	}
	
	return keyEntry, nil
}

func walkRegistry(k registry.Key, entry *regis3.KeyEntry, access uint32) error {
	// 1. Read Values
	valNames, err := k.ReadValueNames(0)
	if err != nil {
		return err
	}
	
	for _, name := range valNames {
		// Read value content using queryValue helper which handles allocation and type retrieval
		valBytes, valType, err := queryValue(k, name)
		if err != nil {
			return fmt.Errorf("failed to read value %s: %w", name, err)
		}

		// Create ValueEntry
		valEntry := entry.FindOrCreateValue(name)
		
		// Map Windows type to regis3 type and set data
		// regis3 types match Windows API constants mostly.
		valEntry.SetBinaryType(valType, valBytes)
	}

	// 2. Read Subkeys
	subKeyNames, err := k.ReadSubKeyNames(0)
	if err != nil {
		return err
	}
	
	for _, subName := range subKeyNames {
		subK, err := registry.OpenKey(k, subName, access|registry.READ)
		if err != nil {
			// Permission denied or gone?
			// Should we warn or fail?
			// "Access is denied" is common. 
			// We probably want to skip keys we can't read?
			// C# implementation usually stops or logs error?
			// Let's wrap error but continue if possible? No, standard recursive walk fails.
			return fmt.Errorf("failed to open subkey %s: %w", subName, err)
		}
		
		subEntry := entry.FindOrCreateKey(subName)
		err = walkRegistry(subK, subEntry, access)
		subK.Close()
		if err != nil {
			return err
		}
	}
	
	return nil
}

func queryValue(k registry.Key, name string) ([]byte, uint32, error) {
	// Query size first
	var size uint32
	var valType uint32
	
	// We use windows.RegQueryValueEx directly to get size
	// handle is k
	
	ptrName, _ := windows.UTF16PtrFromString(name)
	err := windows.RegQueryValueEx(windows.Handle(k), ptrName, nil, &valType, nil, &size)
	if err != nil {
		return nil, 0, err
	}
	
	buf := make([]byte, size)
	err = windows.RegQueryValueEx(windows.Handle(k), ptrName, nil, &valType, &buf[0], &size)
	if err != nil {
		return nil, 0, err
	}
	
	return buf, valType, nil
}
func rootKeyName(k registry.Key) string {
	switch k {
	case registry.CLASSES_ROOT: return "HKEY_CLASSES_ROOT"
	case registry.CURRENT_USER: return "HKEY_CURRENT_USER"
	case registry.LOCAL_MACHINE: return "HKEY_LOCAL_MACHINE"
	case registry.USERS: return "HKEY_USERS"
	case registry.CURRENT_CONFIG: return "HKEY_CURRENT_CONFIG"
	}
	return "HKEY_UNKNOWN"
}