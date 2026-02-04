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
	case "HKEY_CLASSES_ROOT", "HKCR":
		return registry.CLASSES_ROOT, nil
	case "HKEY_CURRENT_USER", "HKCU":
		return registry.CURRENT_USER, nil
	case "HKEY_LOCAL_MACHINE", "HKLM":
		return registry.LOCAL_MACHINE, nil
	case "HKEY_USERS", "HKU":
		return registry.USERS, nil
	case "HKEY_CURRENT_CONFIG", "HKCC":
		return registry.CURRENT_CONFIG, nil
	}
	return 0, fmt.Errorf("unknown hive: %s", name)
}

// ReadRegistryPath reads a registry path like "HKEY_LOCAL_MACHINE\Software\Microsoft"
// and returns a KeyEntry tree with all keys and values under that path.
func ReadRegistryPath(path string, access uint32) (*regis3.KeyEntry, error) {
	// Parse the path to extract hive and relative path
	hiveName, relPath := splitRegistryPath(path)
	if hiveName == "" {
		return nil, fmt.Errorf("invalid registry path: %s", path)
	}

	rootKey, err := MapHive(hiveName)
	if err != nil {
		return nil, err
	}

	// Create result tree with hive as root
	result := regis3.NewKeyEntry(nil, hiveName)

	if relPath == "" {
		// Read entire hive (just the root level - this could be huge)
		if err := readRegistryRecursive(rootKey, "", result, access); err != nil {
			return nil, err
		}
	} else {
		// Create path structure and read from the specified subkey
		parent := result
		parts := strings.Split(relPath, "\\")
		for i, part := range parts {
			child := regis3.NewKeyEntry(parent, part)
			parent.SubKeys()[strings.ToLower(part)] = child
			child.SetParent(parent)

			if i == len(parts)-1 {
				// Last part - read registry content here
				if err := readRegistryRecursive(rootKey, relPath, child, access); err != nil {
					return nil, err
				}
			}
			parent = child
		}
	}

	// Wrap in anonymous root to match expected structure
	root := regis3.NewKeyEntry(nil, "")
	root.SubKeys()[strings.ToLower(hiveName)] = result
	result.SetParent(root)

	return root, nil
}

func splitRegistryPath(path string) (hive, relPath string) {
	knownHives := []string{
		"HKEY_CLASSES_ROOT", "HKEY_CURRENT_USER", "HKEY_LOCAL_MACHINE",
		"HKEY_USERS", "HKEY_CURRENT_CONFIG", "HKEY_PERFORMANCE_DATA",
		"HKCR", "HKCU", "HKLM", "HKU", "HKCC", "HKPD",
	}

	upper := strings.ToUpper(path)
	for _, h := range knownHives {
		if upper == h {
			return h, ""
		}
		if strings.HasPrefix(upper, h+"\\") {
			return path[:len(h)], path[len(h)+1:]
		}
	}
	return "", ""
}

func readRegistryRecursive(rootKey registry.Key, relPath string, entry *regis3.KeyEntry, access uint32) error {
	var k registry.Key
	var err error

	if relPath == "" {
		k = rootKey
	} else {
		k, err = registry.OpenKey(rootKey, relPath, access|registry.READ)
		if err != nil {
			return fmt.Errorf("failed to open key %s: %w", relPath, err)
		}
		defer k.Close()
	}

	// Read values
	valNames, err := k.ReadValueNames(0)
	if err != nil {
		return err
	}

	for _, name := range valNames {
		valBytes, valType, err := queryValue(k, name)
		if err != nil {
			return fmt.Errorf("failed to read value %s: %w", name, err)
		}
		valEntry := entry.FindOrCreateValue(name)
		valEntry.SetBinaryType(valType, valBytes)
	}

	// Read subkeys
	subKeyNames, err := k.ReadSubKeyNames(0)
	if err != nil {
		return err
	}

	for _, subName := range subKeyNames {
		subEntry := entry.FindOrCreateKey(subName)
		subPath := subName
		if relPath != "" {
			subPath = relPath + "\\" + subName
		}

		if err := readRegistryRecursive(rootKey, subPath, subEntry, access); err != nil {
			// Log but continue on access denied
			if strings.Contains(err.Error(), "Access is denied") {
				continue
			}
			return err
		}
	}

	return nil
}
