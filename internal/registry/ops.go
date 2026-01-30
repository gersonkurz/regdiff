//go:build windows

package registry

import (
	"fmt"
	"strings"

	"github.com/gersonkurz/go-regis3"
	"golang.org/x/sys/windows"
	"golang.org/x/sys/windows/registry"
)

// Constants for Access and View
const (
	AccessRead      = registry.READ
	AccessWrite     = registry.WRITE
	AccessAll       = registry.ALL_ACCESS
	View32          = windows.KEY_WOW64_32KEY
	View64          = windows.KEY_WOW64_64KEY
)

// LoadLiveRegistry populates liveRoot with keys found in fileKey structure.
func LoadLiveRegistry(liveRoot *regis3.KeyEntry, fileKey *regis3.KeyEntry, access uint32) error {
	for hiveName, hiveKey := range fileKey.SubKeys() {
		rootH, err := MapHive(hiveName)
		if err != nil {
			return err
		}
		
		liveHive := liveRoot.FindOrCreateKey(hiveName)
		
		for subName, subKey := range hiveKey.SubKeys() {
			if err := recursiveLoad(liveHive, subKey, rootH, subName, access); err != nil {
				return err
			}
		}
	}
	return nil
}

func recursiveLoad(liveParent *regis3.KeyEntry, fileNode *regis3.KeyEntry, rootH registry.Key, relPath string, access uint32) error {
	if len(fileNode.Values()) > 0 || len(fileNode.SubKeys()) == 0 {
		loadedKey, err := ParseRegistry(rootH, relPath, access)
		if err != nil {
			// Handle Access Denied (5) or File Not Found (2)
			if err == registry.ErrNotExist || strings.Contains(err.Error(), "The system cannot find the file specified") {
				return nil
			}
			// Access Denied: Log and skip
			if strings.Contains(err.Error(), "Access is denied") {
				fmt.Printf("Warning: Access denied reading registry key %s\n", relPath)
				return nil
			}
			
			fmt.Printf("Warning: failed to read registry key %s: %v\n", relPath, err)
			return nil
		}
		
		parts := strings.Split(relPath, "\\")
		parentPath := ""
		if len(parts) > 1 {
			parentPath = strings.Join(parts[:len(parts)-1], "\\")
		}
		
		destContainer := liveParent.FindOrCreateKey(parentPath)
		destContainer.SubKeys()[strings.ToLower(loadedKey.Name())] = loadedKey
		loadedKey.SetParent(destContainer)
		return nil
	}

	for subName, subKey := range fileNode.SubKeys() {
		newPath := relPath + "\\" + subName
		if err := recursiveLoad(liveParent, subKey, rootH, newPath, access); err != nil {
			return err
		}
	}
	return nil
}

// WriteToRegistry writes the key tree to the registry.
func WriteToRegistry(key *regis3.KeyEntry, access uint32) error {
	for hiveName, hiveKey := range key.SubKeys() {
		rootH, err := MapHive(hiveName)
		if err != nil {
			return err
		}
		for subName, subKey := range hiveKey.SubKeys() {
			if err := WriteRegistry(rootH, subName, subKey, access); err != nil {
				return err
			}
		}
	}
	return nil
}

func MapHive(name string) (registry.Key, error) {
	switch strings.ToUpper(name) {
	case "HKEY_CLASSES_ROOT", "HKCR": return registry.CLASSES_ROOT, nil
	case "HKEY_CURRENT_USER", "HKCU": return registry.CURRENT_USER, nil
	case "HKEY_LOCAL_MACHINE", "HKLM": return registry.LOCAL_MACHINE, nil
	case "HKEY_USERS", "HKU": return registry.USERS, nil
	case "HKEY_CURRENT_CONFIG", "HKCC": return registry.CURRENT_CONFIG, nil
	}
	return 0, fmt.Errorf("unknown hive: %s", name)
}