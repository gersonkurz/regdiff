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

# Build for Linux x64
build-linux-amd64:
    GOOS=linux GOARCH=amd64 go build -ldflags "-s -w -X main.Version={{version}}" -o {{dist_dir}}/{{binary}}-linux-amd64 {{cmd_path}}

# Build for Linux arm64
build-linux-arm64:
    GOOS=linux GOARCH=arm64 go build -ldflags "-s -w -X main.Version={{version}}" -o {{dist_dir}}/{{binary}}-linux-arm64 {{cmd_path}}

# Build for macOS x64 (Intel)
build-darwin-amd64:
    GOOS=darwin GOARCH=amd64 go build -ldflags "-s -w -X main.Version={{version}}" -o {{dist_dir}}/{{binary}}-darwin-amd64 {{cmd_path}}

# Build for macOS arm64 (Apple Silicon)
build-darwin-arm64:
    GOOS=darwin GOARCH=arm64 go build -ldflags "-s -w -X main.Version={{version}}" -o {{dist_dir}}/{{binary}}-darwin-arm64 {{cmd_path}}

# Build all Windows targets
build-windows: build-windows-amd64 build-windows-arm64

# Build all Linux targets
build-linux: build-linux-amd64 build-linux-arm64

# Build all macOS targets
build-darwin: build-darwin-amd64 build-darwin-arm64

# Build all targets
build-all: clean-dist build-windows build-linux build-darwin
    @echo "Built all targets in {{dist_dir}}/"

# Create release packages (zip/tar.gz files)
release: build-all _package-windows-amd64 _package-windows-arm64 _package-linux-amd64 _package-linux-arm64 _package-darwin-amd64 _package-darwin-arm64
    @echo "Release packages created in {{dist_dir}}/"

# Internal: package Windows amd64
_package-windows-amd64:
    powershell -Command "Compress-Archive -Force -Path '{{dist_dir}}/regdiff-windows-amd64.exe', 'LICENSE', 'README.md', 'docs/manual.md' -DestinationPath '{{dist_dir}}/regdiff-{{version}}-windows-amd64.zip'"

# Internal: package Windows arm64
_package-windows-arm64:
    powershell -Command "Compress-Archive -Force -Path '{{dist_dir}}/regdiff-windows-arm64.exe', 'LICENSE', 'README.md', 'docs/manual.md' -DestinationPath '{{dist_dir}}/regdiff-{{version}}-windows-arm64.zip'"

# Internal: package Linux amd64
_package-linux-amd64:
    tar -czvf {{dist_dir}}/regdiff-{{version}}-linux-amd64.tar.gz -C {{dist_dir}} regdiff-linux-amd64 -C .. LICENSE README.md docs/manual.md

# Internal: package Linux arm64
_package-linux-arm64:
    tar -czvf {{dist_dir}}/regdiff-{{version}}-linux-arm64.tar.gz -C {{dist_dir}} regdiff-linux-arm64 -C .. LICENSE README.md docs/manual.md

# Internal: package macOS amd64
_package-darwin-amd64:
    tar -czvf {{dist_dir}}/regdiff-{{version}}-darwin-amd64.tar.gz -C {{dist_dir}} regdiff-darwin-amd64 -C .. LICENSE README.md docs/manual.md

# Internal: package macOS arm64
_package-darwin-arm64:
    tar -czvf {{dist_dir}}/regdiff-{{version}}-darwin-arm64.tar.gz -C {{dist_dir}} regdiff-darwin-arm64 -C .. LICENSE README.md docs/manual.md

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
