//go:build windows

package registry

import (
	"fmt"
	"syscall"
	"unsafe"

	"github.com/gersonkurz/go-regis3"
	"golang.org/x/sys/windows"
	"golang.org/x/sys/windows/registry"
)

var (
	modadvapi32 = windows.NewLazySystemDLL("advapi32.dll")
	procRegSetValueExW = modadvapi32.NewProc("RegSetValueExW")
)

func regSetValueEx(key windows.Handle, valueName *uint16, reserved uint32, vtype uint32, buf *byte, bufLen uint32) error {
	r1, _, e1 := syscall.Syscall6(procRegSetValueExW.Addr(), 6, uintptr(key), uintptr(unsafe.Pointer(valueName)), uintptr(reserved), uintptr(vtype), uintptr(unsafe.Pointer(buf)), uintptr(bufLen))
	if r1 != 0 {
		if e1 != 0 {
			return error(e1)
		}
		return syscall.EINVAL
	}
	return nil
}

// WriteRegistry applies a KeyEntry tree to the live registry.
func WriteRegistry(root registry.Key, path string, key *regis3.KeyEntry, access uint32) error {
	if key.RemoveFlag() {
		// Key marked for removal. Delete it recursively.
		// We have `root` (handle) and `path` (relative to root).
		if err := deleteKeyRecursive(root, path); err != nil && err != registry.ErrNotExist {
			return fmt.Errorf("failed to delete key %s: %w", path, err)
		}
		return nil
	}

	// Create/Open the target key
	k, _, err := registry.CreateKey(root, path, access|registry.WRITE)
	if err != nil {
		return fmt.Errorf("failed to create/open key %s: %w", path, err)
	}
	defer k.Close()
	
	// 1. Process Values
	for _, v := range key.Values() {
		if v.RemoveFlag() {
			if err := k.DeleteValue(v.Name()); err != nil && err != registry.ErrNotExist {
				return fmt.Errorf("failed to delete value %s: %w", v.Name(), err)
			}
			continue
		}
		
		// Write value
		// regis3 stores raw bytes and type. We can use SetValueEx?
		// registry package doesn't expose SetValueEx directly for raw bytes easily
		// except via `SetBinaryValue` (REG_BINARY) etc.
		// But we want to preserve the exact type (e.g. REG_DWORD_BIG_ENDIAN).
		// We should use a syscall wrapper for raw writing.
		if err := setRawValue(k, v.Name(), v.Kind(), v.Data()); err != nil {
			return fmt.Errorf("failed to write value %s: %w", v.Name(), err)
		}
	}
	
	// Default Value
	if def := key.DefaultValue(); def != nil {
		if def.RemoveFlag() {
			k.DeleteValue("")
		} else {
			if err := setRawValue(k, "", def.Kind(), def.Data()); err != nil {
				return fmt.Errorf("failed to write default value: %w", err)
			}
		}
	}

	// 2. Process Subkeys
	for _, sub := range key.SubKeys() {
		subPath := sub.Name() // Relative name
		
		if sub.RemoveFlag() {
			// Delete this subkey recursively
			if err := deleteKeyRecursive(k, subPath); err != nil && err != registry.ErrNotExist {
				return fmt.Errorf("failed to delete key %s: %w", subPath, err)
			}
			continue
		}

		// Recurse
		if err := WriteRegistry(k, subPath, sub, access); err != nil {
			return err
		}
	}
	
	return nil
}

func deleteKeyRecursive(parent registry.Key, path string) error {
	k, err := registry.OpenKey(parent, path, registry.ENUMERATE_SUB_KEYS | registry.QUERY_VALUE | windows.KEY_SET_VALUE)
	if err != nil {
		return err
	}
	defer k.Close()
	
	// Delete all subkeys first
	subKeys, err := k.ReadSubKeyNames(0)
	if err != nil {
		return err
	}
	
	for _, sub := range subKeys {
		if err := deleteKeyRecursive(k, sub); err != nil {
			return err
		}
	}
	
	// Close key before deleting it (parent deletes child)
	k.Close()
	
	return registry.DeleteKey(parent, path)
}

// setRawValue writes raw bytes with specific type

func setRawValue(k registry.Key, name string, valType uint32, data []byte) error {

	// registry.SetValue not available, use syscall wrapper

	ptrName, err := windows.UTF16PtrFromString(name)

	if err != nil {

		return err

	}

	

	// Data pointer

	var ptrData *byte

	if len(data) > 0 {

		ptrData = &data[0]

	}

	

	return regSetValueEx(

		windows.Handle(k),

		ptrName,

		0,

		valType,

		ptrData,

		uint32(len(data)),

	)

}




