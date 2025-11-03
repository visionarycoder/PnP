# Bash Snippets

Collection of production-ready Bash scripts following modern shell scripting best practices.

## Index

- [File Operations](file-operations.md) - Safe file and directory manipulation with error handling
- [Text Processing](text-processing.md) - Advanced grep, sed, awk patterns with performance optimization
- [System Administration](system-admin.md) - Robust system monitoring and maintenance scripts

## Quick Reference

### Script Structure Template

```bash
#!/bin/bash
# Script: example-script.sh
# Description: Template for bash scripts with best practices
# Usage: ./example-script.sh [options]

set -euo pipefail  # Exit on error, undefined vars, pipe failures

# Constants
readonly SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
readonly SCRIPT_NAME="$(basename "$0")"
readonly LOG_FILE="/tmp/${SCRIPT_NAME%.sh}.log"

# Functions
usage() {
    cat << EOF
Usage: $SCRIPT_NAME [OPTIONS]

Options:
    -h, --help      Show this help message
    -v, --verbose   Enable verbose output
    -d, --debug     Enable debug mode

Examples:
    $SCRIPT_NAME --verbose
    $SCRIPT_NAME --debug
EOF
}

log() {
    local level="$1"
    shift
    echo "[$(date +'%Y-%m-%d %H:%M:%S')] [$level] $*" | tee -a "$LOG_FILE"
}

cleanup() {
    log "INFO" "Cleaning up temporary files..."
    [[ -n "${temp_file:-}" ]] && rm -f "$temp_file"
}

main() {
    local verbose=false
    local debug=false
    
    # Parse command line arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            -h|--help)
                usage
                exit 0
                ;;
            -v|--verbose)
                verbose=true
                shift
                ;;
            -d|--debug)
                debug=true
                set -x
                shift
                ;;
            *)
                log "ERROR" "Unknown option: $1"
                usage >&2
                exit 1
                ;;
        esac
    done
    
    # Set up signal handlers
    trap cleanup EXIT
    trap 'log "ERROR" "Script interrupted"; exit 130' INT TERM
    
    log "INFO" "Script started with PID $$"
    
    # Main script logic here
    
    log "INFO" "Script completed successfully"
}

# Only run main if script is executed directly
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi
```

### Error Handling Patterns

```bash
# Check command availability
check_dependencies() {
    local deps=("curl" "jq" "git")
    local missing=()
    
    for cmd in "${deps[@]}"; do
        if ! command -v "$cmd" >/dev/null 2>&1; then
            missing+=("$cmd")
        fi
    done
    
    if [[ ${#missing[@]} -gt 0 ]]; then
        log "ERROR" "Missing dependencies: ${missing[*]}"
        exit 1
    fi
}

# Safe file operations
safe_file_operation() {
    local source_file="$1"
    local backup_dir="/tmp/backups"
    
    # Validate input
    [[ -z "$source_file" ]] && { log "ERROR" "No file specified"; return 1; }
    [[ ! -f "$source_file" ]] && { log "ERROR" "File not found: $source_file"; return 1; }
    
    # Create backup
    mkdir -p "$backup_dir"
    cp "$source_file" "$backup_dir/$(basename "$source_file").backup.$(date +%s)"
    
    log "INFO" "Backup created for $source_file"
}
```

### Variable Handling Best Practices

```bash
# Environment variables with defaults
readonly CONFIG_FILE="${CONFIG_FILE:-/etc/myapp/config.conf}"
readonly MAX_RETRIES="${MAX_RETRIES:-3}"
readonly TIMEOUT="${TIMEOUT:-30}"

# Array operations
declare -a processed_files=()
declare -A file_checksums=()

process_files() {
    local directory="$1"
    local -a files=()
    
    # Read files into array safely
    while IFS= read -r -d '' file; do
        files+=("$file")
    done < <(find "$directory" -type f -name "*.txt" -print0)
    
    # Process each file
    for file in "${files[@]}"; do
        local checksum
        checksum=$(sha256sum "$file" | cut -d' ' -f1)
        file_checksums["$file"]="$checksum"
        processed_files+=("$file")
        
        log "INFO" "Processed: $file (checksum: $checksum)"
    done
}
```

### Performance Optimization

```bash
# Efficient text processing
process_large_file() {
    local input_file="$1"
    local output_file="$2"
    
    # Use built-in string operations instead of external commands
    while IFS= read -r line; do
        # Remove leading/trailing whitespace using parameter expansion
        line="${line#"${line%%[![:space:]]*}"}"
        line="${line%"${line##*[![:space:]]}"}"
        
        # Skip empty lines
        [[ -n "$line" ]] && echo "$line"
    done < "$input_file" > "$output_file"
}

# Parallel processing with job control
parallel_process() {
    local -a tasks=("$@")
    local max_jobs=4
    local job_count=0
    
    for task in "${tasks[@]}"; do
        # Wait if we've reached max jobs
        while (( job_count >= max_jobs )); do
            wait -n  # Wait for any job to complete
            ((job_count--))
        done
        
        # Start new job in background
        process_task "$task" &
        ((job_count++))
    done
    
    # Wait for all remaining jobs
    wait
}
```

## Categories

### File Operations

- Safe file manipulation with backup strategies
- Directory traversal with proper error handling  
- Permission management and ownership verification
- Atomic file operations using temporary files
- Large file processing with memory efficiency

### Text Processing

- Advanced grep patterns with performance optimization
- sed scripting for complex text transformations
- awk programming for data extraction and reporting
- Stream processing for large datasets
- Regular expression best practices

### System Administration

- Process monitoring and management
- Service health checks with alerting
- Log rotation and cleanup automation
- Resource usage monitoring and reporting
- Backup and recovery procedures

### Network Operations

- HTTP/HTTPS operations with curl best practices
- Network connectivity testing and diagnostics
- API interaction patterns with error handling
- File transfer protocols (FTP, SCP, RSYNC)
- Network security scanning basics

### Process Management

- Job control and background processing
- Signal handling for graceful shutdowns
- Process supervision and restart logic
- Inter-process communication (pipes, FIFOs)
- Resource limiting and monitoring

### Security Patterns

- Input validation and sanitization
- Secure temporary file creation
- Privilege escalation handling
- Credential management best practices
- Command injection prevention
