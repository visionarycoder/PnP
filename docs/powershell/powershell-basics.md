# PowerShell Basics

**Description**: Essential PowerShell commands and scripting patterns
**Language/Technology**: PowerShell / Windows Administration

## Basic File Operations

**Code**:

```powershell
# Get file and directory information
Get-ChildItem                          # List current directory (ls equivalent)
Get-ChildItem -Path C:\Windows         # List specific directory
Get-ChildItem -Recurse                 # Recursive listing
Get-ChildItem -Filter "*.txt"          # Filter by extension
Get-ChildItem -Hidden                  # Show hidden files

# File operations
Copy-Item source.txt destination.txt   # Copy file
Move-Item old.txt new.txt              # Move/rename file
Remove-Item file.txt                   # Delete file
New-Item -ItemType File -Name "test.txt" # Create new file
New-Item -ItemType Directory -Name "folder" # Create directory

# File content operations
Get-Content file.txt                   # Read file content
Set-Content -Path file.txt -Value "Hello World" # Write to file
Add-Content -Path file.txt -Value "New line"    # Append to file
Out-File -FilePath output.txt -InputObject $data # Write object to file
```

## System Information

**Code**:

```powershell
# System information
Get-ComputerInfo                       # Comprehensive system info
Get-Process                           # Running processes
Get-Service                           # Windows services
Get-EventLog -LogName System -Newest 10 # Recent system events

# Hardware information
Get-WmiObject -Class Win32_ComputerSystem # Computer details
Get-WmiObject -Class Win32_Processor     # CPU information
Get-WmiObject -Class Win32_PhysicalMemory # Memory information
Get-Disk                                 # Disk information

# Network information
Get-NetAdapter                         # Network adapters
Get-NetIPAddress                       # IP addresses
Test-NetConnection -ComputerName google.com # Test connectivity
Get-NetTCPConnection                   # Active TCP connections
```

## Object Manipulation

**Code**:

```powershell
# PowerShell pipeline and objects
Get-Process | Where-Object {$_.CPU -gt 100} # Filter processes by CPU
Get-Service | Sort-Object Status            # Sort services by status
Get-ChildItem | Select-Object Name, Length  # Select specific properties

# Grouping and measuring
Get-Process | Group-Object ProcessName      # Group by process name
Get-ChildItem | Measure-Object Length -Sum  # Calculate total size
Get-EventLog -LogName System | Group-Object EntryType # Group events

# Formatting output
Get-Process | Format-Table Name, CPU        # Table format
Get-Service | Format-List Name, Status      # List format
Get-Process | Out-GridView                  # GUI grid view
```

## Variables and Scripting

**Code**:

```powershell
# Variables
$name = "PowerShell"                   # String variable
$number = 42                           # Numeric variable
$array = @("item1", "item2", "item3")  # Array
$hash = @{key1="value1"; key2="value2"} # Hashtable

# Conditional statements
if ($number -gt 30) {
    Write-Host "Number is greater than 30"
} elseif ($number -eq 30) {
    Write-Host "Number is exactly 30"
} else {
    Write-Host "Number is less than 30"
}

# Loops
for ($i = 0; $i -lt 5; $i++) {
    Write-Host "Iteration $i"
}

foreach ($item in $array) {
    Write-Host "Processing: $item"
}

# Functions
function Get-Square {
    param([int]$Number)
    return $Number * $Number
}
```

## Error Handling

**Code**:

```powershell
# Try-Catch-Finally
try {
    Get-Content "nonexistent.txt"
} catch [System.IO.FileNotFoundException] {
    Write-Host "File not found!"
} catch {
    Write-Host "An error occurred: $($_.Exception.Message)"
} finally {
    Write-Host "Cleanup operations"
}

# Error preferences
$ErrorActionPreference = "Stop"        # Stop on any error
$ErrorActionPreference = "Continue"    # Continue on errors (default)
$ErrorActionPreference = "SilentlyContinue" # Suppress error messages

# Testing and validation
Test-Path "C:\Windows"                 # Check if path exists
$null -eq (Get-Process -Name "notepad" -ErrorAction SilentlyContinue)
```

**Usage**:

```powershell
# Find large files
Get-ChildItem -Recurse | Where-Object {$_.Length -gt 100MB} | 
Sort-Object Length -Descending

# Get running services
Get-Service | Where-Object {$_.Status -eq "Running"} | 
Select-Object Name, DisplayName

# Monitor process CPU usage
while ($true) {
    Get-Process | Sort-Object CPU -Descending | 
    Select-Object -First 5 | Format-Table Name, CPU
    Start-Sleep -Seconds 5
}

# Backup files modified today
$today = Get-Date
Get-ChildItem -Recurse | Where-Object {$_.LastWriteTime.Date -eq $today.Date} |
Copy-Item -Destination "C:\Backup\"

# Remote operations (if enabled)
Invoke-Command -ComputerName "RemotePC" -ScriptBlock {
    Get-Process | Select-Object Name, CPU
}
```

**Notes**:

- **Execution Policy**: May need to set execution policy: `Set-ExecutionPolicy RemoteSigned`
- **Objects**: Everything in PowerShell is an object with properties and methods
- **Pipeline**: Use `|` to pass objects between cmdlets efficiently
- **Help**: Use `Get-Help cmdlet-name` for detailed help information
- **Aliases**: Many Unix commands work as aliases (ls, cat, man, etc.)
- **Security**: Be cautious with remote execution and credential handling
- **Performance**: For large datasets, consider using .NET methods directly

## Related Snippets

- [System Administration](system-admin.md) - Advanced system management tasks
- [File Operations](file-operations.md) - Detailed file manipulation examples
