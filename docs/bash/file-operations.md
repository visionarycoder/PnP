# File Operations

**Description**: Common file and directory operations in Bash
**Language/Technology**: Bash / Shell Scripting

## Basic File Operations

**Code**:

```bash
#!/bin/bash
set -euo pipefail

# Safe file operations with error handling
safe_mkdir() {
    local target_dir="$1"
    
    if [[ -z "$target_dir" ]]; then
        echo "ERROR: Directory path required" >&2
        return 1
    fi
    
    if ! mkdir -p "$target_dir"; then
        echo "ERROR: Failed to create directory: $target_dir" >&2
        return 1
    fi
    
    echo "INFO: Created directory: $target_dir"
}

# Copy with verification and backup
safe_copy() {
    local source="$1"
    local destination="$2"
    local backup_suffix=".backup.$(date +%s)"
    
    # Validate inputs
    [[ -z "$source" ]] && { echo "ERROR: Source file required" >&2; return 1; }
    [[ -z "$destination" ]] && { echo "ERROR: Destination required" >&2; return 1; }
    [[ ! -e "$source" ]] && { echo "ERROR: Source not found: $source" >&2; return 1; }
    
    # Create backup if destination exists
    if [[ -e "$destination" ]]; then
        cp "$destination" "$destination$backup_suffix"
        echo "INFO: Backup created: $destination$backup_suffix"
    fi
    
    # Perform copy with verification
    if cp -p "$source" "$destination"; then
        echo "INFO: Successfully copied: $source -> $destination"
    else
        echo "ERROR: Copy failed: $source -> $destination" >&2
        return 1
    fi
}

# Safe removal with confirmation
safe_remove() {
    local target="$1"
    local force="${2:-false}"
    
    [[ -z "$target" ]] && { echo "ERROR: Target path required" >&2; return 1; }
    [[ ! -e "$target" ]] && { echo "WARNING: Target not found: $target"; return 0; }
    
    if [[ "$force" != "true" ]]; then
        read -p "Remove '$target'? [y/N]: " -r response
        [[ ! "$response" =~ ^[Yy]$ ]] && { echo "INFO: Removal cancelled"; return 0; }
    fi
    
    if rm -rf "$target"; then
        echo "INFO: Removed: $target"
    else
        echo "ERROR: Failed to remove: $target" >&2
        return 1
    fi
}

# Set permissions securely
set_permissions() {
    local file="$1"
    local permissions="$2"
    local owner="${3:-}"
    
    [[ -z "$file" ]] && { echo "ERROR: File path required" >&2; return 1; }
    [[ -z "$permissions" ]] && { echo "ERROR: Permissions required" >&2; return 1; }
    [[ ! -e "$file" ]] && { echo "ERROR: File not found: $file" >&2; return 1; }
    
    if chmod "$permissions" "$file"; then
        echo "INFO: Set permissions $permissions on $file"
    else
        echo "ERROR: Failed to set permissions on $file" >&2
        return 1
    fi
    
    # Change ownership if specified
    if [[ -n "$owner" ]]; then
        if chown "$owner" "$file"; then
            echo "INFO: Set owner $owner on $file"
        else
            echo "WARNING: Failed to set owner $owner on $file" >&2
        fi
    fi
}
```

## Find and Locate Operations

**Code**:

```bash
# Advanced find operations with error handling
find_files() {
    local search_path="$1"
    local pattern="${2:-*}"
    local max_depth="${3:-}"
    
    # Validate search path
    [[ -z "$search_path" ]] && { echo "ERROR: Search path required" >&2; return 1; }
    [[ ! -d "$search_path" ]] && { echo "ERROR: Directory not found: $search_path" >&2; return 1; }
    
    local find_args=("$search_path")
    
    # Add max depth if specified
    [[ -n "$max_depth" ]] && find_args+=(-maxdepth "$max_depth")
    
    # Add pattern matching
    find_args+=(-name "$pattern")
    
    # Execute find with error handling
    if ! find "${find_args[@]}" 2>/dev/null; then
        echo "ERROR: Find operation failed" >&2
        return 1
    fi
}

# Find by size with human-readable output
find_by_size() {
    local search_path="$1"
    local size_criteria="$2"  # e.g., "+100M", "-1k"
    
    [[ -z "$search_path" || -z "$size_criteria" ]] && {
        echo "Usage: find_by_size <path> <size_criteria>" >&2
        return 1
    }
    
    find "$search_path" -type f -size "$size_criteria" -exec ls -lh {} + 2>/dev/null | 
    sort -k5 -hr | 
    head -20
}

# Find recent files with detailed output
find_recent() {
    local search_path="$1"
    local days="${2:-7}"
    
    [[ -z "$search_path" ]] && { echo "ERROR: Search path required" >&2; return 1; }
    
    echo "Files modified within last $days days in $search_path:"
    find "$search_path" -type f -mtime -"$days" -printf "%T@ %Tc %p\n" 2>/dev/null | 
    sort -nr | 
    head -10 | 
    cut -d' ' -f2-
}

# Safe batch operations on found files
find_and_process() {
    local search_path="$1"
    local pattern="$2"
    local action="$3"
    
    [[ -z "$search_path" || -z "$pattern" || -z "$action" ]] && {
        echo "Usage: find_and_process <path> <pattern> <action>" >&2
        return 1
    }
    
    local temp_file
    temp_file=$(mktemp)
    trap 'rm -f "$temp_file"' EXIT
    
    # Find files and store in temp file
    if ! find "$search_path" -name "$pattern" -type f > "$temp_file" 2>/dev/null; then
        echo "ERROR: Find operation failed" >&2
        return 1
    fi
    
    local file_count
    file_count=$(wc -l < "$temp_file")
    
    if [[ "$file_count" -eq 0 ]]; then
        echo "INFO: No files found matching pattern: $pattern"
        return 0
    fi
    
    echo "Found $file_count files matching '$pattern'"
    read -p "Proceed with $action? [y/N]: " -r response
    
    if [[ "$response" =~ ^[Yy]$ ]]; then
        while IFS= read -r file; do
            echo "Processing: $file"
            case "$action" in
                "delete")
                    rm -f "$file"
                    ;;
                "compress")
                    gzip "$file"
                    ;;
                *)
                    echo "Unknown action: $action" >&2
                    ;;
            esac
        done < "$temp_file"
    fi
}
```

## File Content Operations

**Code**:

```bash
# View file content
cat file.txt                       # Display entire file
less file.txt                      # Paginated view
head -n 10 file.txt               # First 10 lines
tail -n 20 file.txt               # Last 20 lines
tail -f /var/log/syslog           # Follow file changes

# File information
ls -la                            # Detailed listing
stat file.txt                    # Detailed file info
file unknown_file                 # Determine file type
wc -l file.txt                   # Count lines
du -sh directory/                # Directory size
df -h                           # Disk space usage
```

## Archive and Compression

**Code**:

```bash
# tar operations
tar -czf archive.tar.gz directory/          # Create compressed archive
tar -xzf archive.tar.gz                     # Extract compressed archive
tar -tzf archive.tar.gz                     # List contents without extracting

# zip operations
zip -r archive.zip directory/               # Create zip archive
unzip archive.zip                           # Extract zip archive
unzip -l archive.zip                        # List zip contents

# Other compression
gzip file.txt                              # Compress file (creates file.txt.gz)
gunzip file.txt.gz                         # Decompress
```

**Usage**:

```bash
# Batch rename files
for file in *.txt; do
    mv "$file" "${file%.txt}.bak"
done

# Find large files
find /home -size +1G -type f -exec ls -lh {} \; | sort -k5 -hr

# Clean up old log files
find /var/log -name "*.log" -mtime +30 -exec gzip {} \;

# Sync directories
rsync -av --delete source/ destination/

# Create backup with timestamp
backup_name="backup_$(date +%Y%m%d_%H%M%S).tar.gz"
tar -czf "$backup_name" important_directory/
```

**Notes**:

- **Safety**: Always use `-i` flag for interactive operations when deleting
- **Permissions**: Be careful with recursive chmod/chown operations
- **Paths**: Use quotes around paths with spaces: `"/path/with spaces/"`
- **Wildcards**: `*` matches any characters, `?` matches single character
- **Performance**: Use `find` instead of `ls` with pipes for better performance
- **Security**: Validate inputs in scripts to prevent injection attacks
- **Alternatives**: Consider `fd` as a faster alternative to `find`

## Related Snippets

- [System Administration](system-admin.md) - System-level file operations
- [Text Processing](text-processing.md) - Processing file contents
