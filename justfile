# regdiff build automation

version := "5.0.0"
binary := "regdiff"
cmd_path := "./cmd/regdiff"
dist_dir := "dist"

# Default recipe: show available commands
default:
    @just --list

# Build for current platform
build:
    go build -ldflags "-s -w -X main.Version={{version}}" -o {{binary}}{{ext}} {{cmd_path}}

# Build for Windows x64
build-windows-amd64:
    GOOS=windows GOARCH=amd64 go build -ldflags "-s -w -X main.Version={{version}}" -o {{dist_dir}}/{{binary}}-windows-amd64.exe {{cmd_path}}

# Build for Windows arm64
build-windows-arm64:
    GOOS=windows GOARCH=arm64 go build -ldflags "-s -w -X main.Version={{version}}" -o {{dist_dir}}/{{binary}}-windows-arm64.exe {{cmd_path}}

# Build all Windows targets
build-all: clean-dist build-windows-amd64 build-windows-arm64
    @echo "Built all targets in {{dist_dir}}/"

# Create release packages (zip files)
release: build-all _package-amd64 _package-arm64
    @echo "Release packages created in {{dist_dir}}/"
    @ls -la {{dist_dir}}/*.zip

# Internal: package Windows amd64
_package-amd64:
    powershell -Command "Compress-Archive -Force -Path '{{dist_dir}}/regdiff-windows-amd64.exe', 'LICENSE', 'README.md', 'docs/manual.md' -DestinationPath '{{dist_dir}}/regdiff-{{version}}-windows-amd64.zip'"

# Internal: package Windows arm64
_package-arm64:
    powershell -Command "Compress-Archive -Force -Path '{{dist_dir}}/regdiff-windows-arm64.exe', 'LICENSE', 'README.md', 'docs/manual.md' -DestinationPath '{{dist_dir}}/regdiff-{{version}}-windows-arm64.zip'"

# Run tests
test:
    go test ./...

# Run tests with verbose output
test-verbose:
    go test -v ./...

# Clean build artifacts
clean:
    rm -f {{binary}} {{binary}}.exe

# Clean dist directory
clean-dist:
    rm -rf {{dist_dir}}
    mkdir -p {{dist_dir}}

# Clean everything
clean-all: clean clean-dist

# Check code formatting
fmt-check:
    @gofmt -l . | grep -q . && echo "Code not formatted. Run 'just fmt'" && exit 1 || echo "Code is formatted"

# Format code
fmt:
    gofmt -w .

# Run go vet
vet:
    go vet ./...

# Run all checks (fmt, vet, test)
check: fmt-check vet test

# Show version
version:
    @echo "{{version}}"

# Platform extension helper
ext := if os() == "windows" { ".exe" } else { "" }
