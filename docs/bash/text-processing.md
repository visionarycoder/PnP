# Text Processing

**Description**: Text manipulation and processing utilities in Bash
**Language/Technology**: Bash / Text Processing

## Basic Text Processing

**Code**:

```bash
#!/bin/bash
set -euo pipefail

# Advanced grep operations with error handling
safe_grep() {
    local pattern="$1"
    local file="$2"
    local options="${3:-}"
    
    # Validate inputs
    [[ -z "$pattern" ]] && { echo "ERROR: Pattern required" >&2; return 1; }
    [[ -z "$file" ]] && { echo "ERROR: File path required" >&2; return 1; }
    [[ ! -f "$file" ]] && { echo "ERROR: File not found: $file" >&2; return 1; }
    
    # Build grep command with options
    local grep_cmd="grep"
    [[ -n "$options" ]] && grep_cmd="grep $options"
    
    # Execute with error handling
    if $grep_cmd "$pattern" "$file"; then
        return 0
    else
        local exit_code=$?
        case $exit_code in
            1)
                echo "INFO: No matches found for pattern: $pattern" >&2
                return 1
                ;;
            2)
                echo "ERROR: Grep syntax error or file access issue" >&2
                return 2
                ;;
            *)
                echo "ERROR: Unexpected grep error (code: $exit_code)" >&2
                return $exit_code
                ;;
        esac
    fi
}

# Multi-file pattern search with summary
search_pattern() {
    local pattern="$1"
    local search_path="${2:-.}"
    local file_pattern="${3:-*}"
    
    [[ -z "$pattern" ]] && { echo "ERROR: Search pattern required" >&2; return 1; }
    
    local temp_file
    temp_file=$(mktemp)
    trap 'rm -f "$temp_file"' EXIT
    
    echo "Searching for pattern '$pattern' in $search_path..."
    
    # Find files and search pattern
    find "$search_path" -name "$file_pattern" -type f -exec grep -l "$pattern" {} + 2>/dev/null > "$temp_file" || true
    
    local match_count
    match_count=$(wc -l < "$temp_file")
    
    if [[ "$match_count" -eq 0 ]]; then
        echo "INFO: No files contain the pattern '$pattern'"
        return 1
    fi
    
    echo "Found pattern in $match_count files:"
    while IFS= read -r file; do
        local line_count
        line_count=$(grep -c "$pattern" "$file" 2>/dev/null || echo "0")
        printf "  %s (%d matches)\n" "$file" "$line_count"
    done < "$temp_file"
}

# Context-aware grep with highlighting
context_grep() {
    local pattern="$1"
    local file="$2"
    local context_lines="${3:-3}"
    
    [[ -z "$pattern" || -z "$file" ]] && {
        echo "Usage: context_grep <pattern> <file> [context_lines]" >&2
        return 1
    }
    
    if command -v grep --color=always >/dev/null 2>&1; then
        grep --color=always -n -C "$context_lines" "$pattern" "$file" 2>/dev/null || {
            echo "INFO: No matches found" >&2
            return 1
        }
    else
        grep -n -C "$context_lines" "$pattern" "$file" 2>/dev/null || {
            echo "INFO: No matches found" >&2
            return 1
        }
    fi
}
```

## sed - Stream Editor

**Code**:

```bash
# Basic substitution
sed 's/old/new/' file.txt                 # Replace first occurrence per line
sed 's/old/new/g' file.txt                # Replace all occurrences
sed 's/old/new/gi' file.txt               # Case insensitive global replace

# In-place editing
sed -i 's/old/new/g' file.txt             # Modify file directly
sed -i.bak 's/old/new/g' file.txt         # Create backup before modifying

# Line operations
sed '5d' file.txt                          # Delete line 5
sed '2,5d' file.txt                        # Delete lines 2-5
sed '/pattern/d' file.txt                  # Delete lines containing pattern
sed -n '10,20p' file.txt                  # Print lines 10-20 only

# Advanced operations
sed 's/\([0-9]\+\)/[\1]/g' file.txt      # Wrap numbers in brackets
sed '/start/,/end/d' file.txt             # Delete from start to end pattern
```

## awk - Pattern Processing

**Code**:

```bash
# Basic awk operations
awk '{print $1}' file.txt                 # Print first field
awk '{print NF, $0}' file.txt            # Print field count and line
awk '{sum+=$1} END {print sum}' numbers.txt # Sum first column

# Field separator
awk -F: '{print $1}' /etc/passwd          # Use colon as separator
awk -F',' '{print $2}' data.csv           # Process CSV files

# Conditional processing
awk '$3 > 100 {print $0}' file.txt        # Print lines where field 3 > 100
awk '/pattern/ {print $1}' file.txt       # Print first field of matching lines

# Built-in variables
awk '{print NR, NF, $0}' file.txt         # Line number, field count, line
awk 'END {print "Total lines:", NR}' file.txt

# Complex processing
awk '{
    if ($1 > max) max = $1
} END {
    print "Maximum value:", max
}' numbers.txt
```

## cut and sort Operations

**Code**:

```bash
# cut - Extract columns
cut -d: -f1 /etc/passwd                   # Extract first field (colon delimiter)
cut -c1-10 file.txt                       # Extract characters 1-10
cut -d, -f2,4 data.csv                    # Extract fields 2 and 4 (CSV)

# sort operations
sort file.txt                             # Alphabetical sort
sort -n numbers.txt                       # Numerical sort
sort -r file.txt                          # Reverse sort
sort -k2 -n data.txt                      # Sort by 2nd field numerically
sort -u file.txt                          # Sort and remove duplicates

# uniq operations (requires sorted input)
sort file.txt | uniq                      # Remove duplicates
sort file.txt | uniq -c                   # Count occurrences
sort file.txt | uniq -d                   # Show only duplicates
```

## Text Transformation

**Code**:

```bash
# tr - Character translation
tr 'a-z' 'A-Z' < file.txt                # Convert to uppercase
tr -d '[:digit:]' < file.txt              # Delete all digits
tr -s ' ' < file.txt                      # Squeeze multiple spaces
echo "hello world" | tr ' ' '_'           # Replace spaces with underscores

# Column operations
column -t data.txt                        # Format into columns
paste file1.txt file2.txt                # Merge files side by side
join file1.txt file2.txt                 # Join files on common field

# Word operations
wc -l file.txt                           # Count lines
wc -w file.txt                           # Count words
wc -c file.txt                           # Count characters

# Text formatting
fmt -w 80 file.txt                       # Wrap text to 80 characters
expand file.txt                          # Convert tabs to spaces
unexpand file.txt                        # Convert spaces to tabs
```

## Advanced Text Processing

**Code**:

```bash
# Process CSV data
process_csv() {
    local file="$1"
    awk -F',' '
    NR==1 {
        for(i=1; i<=NF; i++) headers[i] = $i
        next
    }
    {
        for(i=1; i<=NF; i++) {
            printf "%s: %s", headers[i], $i
            if(i<NF) printf ", "
        }
        print ""
    }' "$file"
}

# Extract emails from text
extract_emails() {
    grep -oE '[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}' "$1"
}

# Count word frequency
word_frequency() {
    tr -cs 'A-Za-z' '\n' < "$1" | 
    tr 'A-Z' 'a-z' | 
    sort | 
    uniq -c | 
    sort -rn | 
    head -20
}

# Remove comments and blank lines
clean_config() {
    sed '/^[[:space:]]*#/d; /^[[:space:]]*$/d' "$1"
}
```

**Usage**:

```bash
# Find and replace in multiple files
find . -name "*.txt" -exec sed -i 's/old_text/new_text/g' {} \;

# Extract specific data from logs
awk '/ERROR/ {print $1, $2, $NF}' /var/log/application.log

# Process structured data
cut -d: -f1,3 /etc/passwd | sort -t: -k2 -n

# Generate reports
awk -F',' '{sum[$2] += $3} END {for (i in sum) print i, sum[i]}' sales.csv

# Clean up whitespace
sed 's/[[:space:]]*$//' file.txt | sed '/^$/d' > cleaned.txt

# Extract URLs from HTML
grep -oP 'href="\K[^"]*' index.html
```

**Notes**:

- **Performance**: For large files, consider using specialized tools like `ripgrep` or `ag`
- **Regex**: Be aware of different regex flavors (BRE, ERE, PCRE)
- **Locale**: Text processing behavior can depend on locale settings
- **Memory**: `sort` and `uniq` load entire files into memory
- **Pipes**: Chain commands efficiently to avoid temporary files
- **Security**: Validate input when processing untrusted text data
- **Alternatives**: Consider `jq` for JSON, `xmlstarlet` for XML

## Related Snippets

- [File Operations](file-operations.md) - Basic file handling operations
- [System Administration](system-admin.md) - System log processing
