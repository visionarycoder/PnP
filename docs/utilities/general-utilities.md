# General Utilities

**Description**: Cross-platform utility functions and helper tools
**Language/Technology**: Multiple / General Purpose

## Data Conversion Utilities

**Code**:

```bash
# Base64 encoding/decoding (Bash)
echo "Hello World" | base64                    # Encode to base64
echo "SGVsbG8gV29ybGQK" | base64 -d            # Decode from base64

# URL encoding/decoding
url_encode() {
    python3 -c "import urllib.parse; print(urllib.parse.quote('$1'))"
}

url_decode() {
    python3 -c "import urllib.parse; print(urllib.parse.unquote('$1'))"
}

# JSON formatting
format_json() {
    cat "$1" | python3 -m json.tool
}

# CSV to JSON conversion
csv_to_json() {
    python3 -c "
import csv, json, sys
reader = csv.DictReader(sys.stdin)
print(json.dumps([row for row in reader], indent=2))
" < "$1"
}
```

```powershell
# PowerShell data conversion
# Base64 encoding/decoding
$text = "Hello World"
$encoded = [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($text))
$decoded = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($encoded))

# JSON operations
$object = @{name="John"; age=30}
$json = $object | ConvertTo-Json
$backToObject = $json | ConvertFrom-Json

# CSV operations
Import-Csv "data.csv" | ConvertTo-Json | Out-File "data.json"
```

```python
# Python data conversion utilities
import json
import csv
import base64
from urllib.parse import quote, unquote

def csv_to_json_file(csv_file, json_file):
    """Convert CSV file to JSON format"""
    with open(csv_file, 'r') as f:
        reader = csv.DictReader(f)
        data = [row for row in reader]
    
    with open(json_file, 'w') as f:
        json.dump(data, f, indent=2)

def base64_encode_decode(text, encode=True):
    """Encode or decode base64"""
    if encode:
        return base64.b64encode(text.encode()).decode()
    else:
        return base64.b64decode(text).decode()
```

## Text Processing Utilities

**Code**:

```bash
# Text manipulation functions
remove_duplicates() {
    sort "$1" | uniq
}

count_lines() {
    wc -l < "$1"
}

extract_emails() {
    grep -oE '[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}' "$1"
}

# String utilities
to_uppercase() {
    echo "$1" | tr '[:lower:]' '[:upper:]'
}

to_lowercase() {
    echo "$1" | tr '[:upper:]' '[:lower:]'
}

trim_whitespace() {
    echo "$1" | sed 's/^[[:space:]]*//;s/[[:space:]]*$//'
}
```

```python
# Python text utilities
import re
import string

def extract_numbers(text):
    """Extract all numbers from text"""
    return re.findall(r'\d+\.?\d*', text)

def clean_text(text):
    """Remove special characters and extra whitespace"""
    # Remove special characters
    text = re.sub(r'[^a-zA-Z0-9\s]', '', text)
    # Remove extra whitespace
    text = re.sub(r'\s+', ' ', text)
    return text.strip()

def word_count(text):
    """Count words in text"""
    words = text.split()
    return len(words)

def generate_slug(text):
    """Generate URL-friendly slug"""
    # Convert to lowercase
    text = text.lower()
    # Replace spaces and special chars with hyphens
    text = re.sub(r'[^a-z0-9]+', '-', text)
    # Remove leading/trailing hyphens
    return text.strip('-')
```

## Date and Time Utilities

**Code**:

```bash
# Date utilities (Bash)
current_timestamp() {
    date +%s
}

format_date() {
    date -d "@$1" "+%Y-%m-%d %H:%M:%S"
}

days_between() {
    local date1="$1"
    local date2="$2"
    local timestamp1=$(date -d "$date1" +%s)
    local timestamp2=$(date -d "$date2" +%s)
    echo $(( (timestamp2 - timestamp1) / 86400 ))
}

# Backup with timestamp
backup_with_timestamp() {
    local file="$1"
    local timestamp=$(date +%Y%m%d_%H%M%S)
    cp "$file" "${file}.backup.$timestamp"
}
```

```powershell
# PowerShell date utilities
# Current date/time operations
$now = Get-Date
$formatted = $now.ToString("yyyy-MM-dd HH:mm:ss")
$timestamp = [DateTimeOffset]::Now.ToUnixTimeSeconds()

# Date calculations
$futureDate = (Get-Date).AddDays(30)
$pastDate = (Get-Date).AddMonths(-6)
$daysDiff = (Get-Date) - (Get-Date "2023-01-01")

# File operations with dates
$today = Get-Date -Format "yyyy-MM-dd"
Get-ChildItem | Where-Object {$_.LastWriteTime.Date -eq (Get-Date).Date}
```

```python
# Python date utilities
from datetime import datetime, timedelta
import time

def format_timestamp(timestamp, format_str="%Y-%m-%d %H:%M:%S"):
    """Format Unix timestamp"""
    return datetime.fromtimestamp(timestamp).strftime(format_str)

def days_between_dates(date1_str, date2_str):
    """Calculate days between two date strings"""
    date1 = datetime.strptime(date1_str, "%Y-%m-%d")
    date2 = datetime.strptime(date2_str, "%Y-%m-%d")
    return abs((date2 - date1).days)

def get_next_weekday(weekday):
    """Get next occurrence of specified weekday (0=Monday)"""
    today = datetime.today()
    days_ahead = weekday - today.weekday()
    if days_ahead <= 0:
        days_ahead += 7
    return today + timedelta(days_ahead)
```

## File and System Utilities

**Code**:

```bash
# File utilities
find_large_files() {
    local size="${1:-100M}"
    local path="${2:-.}"
    find "$path" -type f -size "+$size" -exec ls -lh {} \; | sort -k5 -hr
}

disk_usage_report() {
    echo "Disk Usage Report - $(date)"
    echo "=================================="
    df -h | grep -v "tmpfs\|udev"
    echo
    echo "Largest directories:"
    du -h /home /var /usr 2>/dev/null | sort -hr | head -10
}

create_project_structure() {
    local project_name="$1"
    mkdir -p "$project_name"/{src,tests,docs,config}
    touch "$project_name"/README.md
    echo "Created project structure for $project_name"
}
```

```python
# Python system utilities
import os
import shutil
import subprocess
from pathlib import Path

def get_directory_size(path):
    """Get total size of directory in bytes"""
    total_size = 0
    for dirpath, dirnames, filenames in os.walk(path):
        for filename in filenames:
            filepath = os.path.join(dirpath, filename)
            if os.path.isfile(filepath):
                total_size += os.path.getsize(filepath)
    return total_size

def format_bytes(bytes_value):
    """Format bytes in human readable format"""
    for unit in ['B', 'KB', 'MB', 'GB', 'TB']:
        if bytes_value < 1024.0:
            return f"{bytes_value:.1f} {unit}"
        bytes_value /= 1024.0

def run_command(command):
    """Run system command and return result"""
    try:
        result = subprocess.run(
            command, shell=True, capture_output=True, text=True
        )
        return result.stdout, result.stderr, result.returncode
    except Exception as e:
        return "", str(e), 1
```

**Usage**:

```bash
# Text processing examples
echo "Hello@example.com and test@domain.org" | extract_emails
echo "  Hello World  " | trim_whitespace
remove_duplicates file_with_duplicates.txt > unique_lines.txt

# Date operations
current_timestamp
days_between "2023-01-01" "2023-12-31"
backup_with_timestamp important_file.txt

# File operations
find_large_files 50M /home/user
create_project_structure "my-new-project"
disk_usage_report
```

```python
# Python usage examples
# Data conversion
csv_to_json_file("data.csv", "data.json")
encoded = base64_encode_decode("Hello World", encode=True)

# Text processing
numbers = extract_numbers("Price: $123.45 and $67.89")
clean = clean_text("Hello!!! @#$ World???")
slug = generate_slug("My Blog Post Title")

# Date operations
formatted = format_timestamp(1640995200)  # 2022-01-01
days = days_between_dates("2023-01-01", "2023-12-31")

# System utilities
size = get_directory_size("/path/to/directory")
readable_size = format_bytes(size)
```

**Notes**:

- **Cross-Platform**: Test utilities on target platforms (Windows/Linux/macOS)
- **Error Handling**: Add proper error checking for production use
- **Performance**: Consider performance implications for large datasets
- **Dependencies**: Document any external tool dependencies
- **Security**: Validate inputs when processing untrusted data
- **Testing**: Create test cases for utility functions
- **Documentation**: Include examples and parameter descriptions

## Related Snippets

- [Text Processing](../bash/text-processing.md) - Advanced text manipulation
- [PowerShell Basics](../powershell/powershell-basics.md) - PowerShell utilities
- [File Operations](../bash/file-operations.md) - File system utilities
