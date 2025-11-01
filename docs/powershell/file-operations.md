# PowerShell File Operations

**Description**: Comprehensive file and directory manipulation using PowerShell, including advanced operations, bulk processing, and automation.

**Language/Technology**: PowerShell / File Management

**Code**:

```powershell
# ============================================
# Advanced File Discovery and Filtering
# ============================================

# Find files by various criteria
Get-ChildItem -Path "C:\Users" -Recurse -File | Where-Object {$_.Length -gt 100MB}  # Large files
Get-ChildItem -Recurse -File | Where-Object {$_.LastWriteTime -lt (Get-Date).AddDays(-30)} # Old files
Get-ChildItem -Recurse -File | Where-Object {$_.Extension -in @(".log", ".tmp", ".cache")} # Temp files
Get-ChildItem -Recurse | Where-Object {$_.PSIsContainer -and $_.GetFiles().Count -eq 0} # Empty folders

# Advanced search patterns
Get-ChildItem -Path "C:\Logs" -Filter "*.log" -Recurse | Where-Object {
    (Get-Content $_.FullName | Select-String "ERROR").Count -gt 0
} # Files containing errors

# Search file content with context
Select-String -Path "C:\Scripts\*.ps1" -Pattern "Get-Process" -Context 2,2 # Show 2 lines before/after match

# Find duplicate files by hash
Get-ChildItem -Recurse -File | Group-Object {(Get-FileHash $_.FullName).Hash} | 
Where-Object {$_.Count -gt 1} | ForEach-Object {$_.Group}

# ============================================
# Bulk File Operations
# ============================================

# Bulk rename files
Get-ChildItem -Path "C:\Photos" -Filter "*.jpg" | 
ForEach-Object {
    $newName = "Photo_" + (Get-Date $_.LastWriteTime -Format "yyyyMMdd_HHmmss") + ".jpg"
    Rename-Item $_.FullName -NewName $newName
}

# Batch file extension change
Get-ChildItem -Filter "*.jpeg" | Rename-Item -NewName {$_.Name -replace "\.jpeg$", ".jpg"}

# Mass file moving with folder creation
Get-ChildItem -Filter "*.pdf" | ForEach-Object {
    $year = $_.LastWriteTime.Year
    $monthFolder = Join-Path "C:\Documents\Organized" "$year\$(Get-Date $_.LastWriteTime -Format 'MM-MMMM')"
    
    if (-not (Test-Path $monthFolder)) {
        New-Item -ItemType Directory -Path $monthFolder -Force
    }
    
    Move-Item $_.FullName -Destination $monthFolder
}

# Bulk copy with progress
$files = Get-ChildItem -Path "C:\Source" -Recurse
$totalFiles = $files.Count
$counter = 0

foreach ($file in $files) {
    $counter++
    $percentage = [math]::Round(($counter / $totalFiles) * 100, 2)
    Write-Progress -Activity "Copying Files" -Status "$percentage% Complete" -PercentComplete $percentage
    
    $destination = $file.FullName.Replace("C:\Source", "C:\Backup")
    $destinationDir = Split-Path $destination -Parent
    
    if (-not (Test-Path $destinationDir)) {
        New-Item -ItemType Directory -Path $destinationDir -Force | Out-Null
    }
    
    Copy-Item $file.FullName -Destination $destination
}

# ============================================
# File Content Manipulation
# ============================================

# Advanced text processing
# Replace text in multiple files
Get-ChildItem -Path "C:\Scripts" -Filter "*.ps1" -Recurse | ForEach-Object {
    $content = Get-Content $_.FullName
    $newContent = $content -replace "oldserver", "newserver"
    Set-Content -Path $_.FullName -Value $newContent
}

# Insert header/footer in files
Get-ChildItem -Filter "*.txt" | ForEach-Object {
    $header = "# File: $($_.Name) - Generated: $(Get-Date)"
    $content = Get-Content $_.FullName
    $newContent = @($header) + $content + @("# End of file")
    Set-Content -Path $_.FullName -Value $newContent
}

# CSV file manipulation
$csv = Import-Csv "data.csv"
$csv | Where-Object {$_.Age -gt 18} | Export-Csv "adults.csv" -NoTypeInformation

# JSON file processing
$json = Get-Content "config.json" | ConvertFrom-Json
$json.settings.timeout = 30
$json | ConvertTo-Json -Depth 10 | Set-Content "config.json"

# ============================================
# File Compression and Archives
# ============================================

# Create ZIP archives
Compress-Archive -Path "C:\Logs\*" -DestinationPath "C:\Backup\logs_$(Get-Date -Format 'yyyyMMdd').zip"

# Extract archives
Expand-Archive -Path "archive.zip" -DestinationPath "C:\Extracted"

# Compress files by date
Get-ChildItem -Path "C:\Logs" -File | 
Group-Object {$_.LastWriteTime.Date} | 
ForEach-Object {
    $date = $_.Name
    $zipName = "logs_$($date.Replace('/', '-').Replace(' ', '_')).zip"
    Compress-Archive -Path $_.Group.FullName -DestinationPath $zipName
}

# ============================================
# File Monitoring and Watching
# ============================================

# File system watcher
$watcher = New-Object System.IO.FileSystemWatcher
$watcher.Path = "C:\MonitoredFolder"
$watcher.Filter = "*.*"
$watcher.IncludeSubdirectories = $true
$watcher.EnableRaisingEvents = $true

# Event handlers
$action = {
    $path = $Event.SourceEventArgs.FullPath
    $changeType = $Event.SourceEventArgs.ChangeType
    Write-Host "File $changeType: $path" -ForegroundColor Green
}

Register-ObjectEvent -InputObject $watcher -EventName "Created" -Action $action
Register-ObjectEvent -InputObject $watcher -EventName "Changed" -Action $action
Register-ObjectEvent -InputObject $watcher -EventName "Deleted" -Action $action

# ============================================
# File Security and Permissions
# ============================================

# Get file permissions
Get-Acl "C:\ImportantFile.txt" | Select-Object -ExpandProperty Access

# Set file permissions
$acl = Get-Acl "C:\ImportantFile.txt"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("DOMAIN\User", "FullControl", "Allow")
$acl.SetAccessRule($accessRule)
Set-Acl -Path "C:\ImportantFile.txt" -AclObject $acl

# Take ownership of files
takeown /F "C:\ProtectedFile.txt" /A
icacls "C:\ProtectedFile.txt" /grant "Administrators:F"

# ============================================
# File Comparison and Synchronization
# ============================================

# Compare directories
$source = Get-ChildItem -Path "C:\Source" -Recurse | Select-Object Name, Length, LastWriteTime
$destination = Get-ChildItem -Path "C:\Backup" -Recurse | Select-Object Name, Length, LastWriteTime

Compare-Object $source $destination -Property Name, Length, LastWriteTime

# Sync directories (one-way)
function Sync-Directories {
    param(
        [string]$Source,
        [string]$Destination
    )
    
    robocopy $Source $Destination /MIR /R:3 /W:5 /MT:8 /LOG:sync.log
}

# ============================================
# File Cleanup and Maintenance
# ============================================

# Cleanup temporary files
$tempPaths = @(
    "$env:TEMP\*",
    "$env:WINDIR\Temp\*",
    "$env:LOCALAPPDATA\Temp\*"
)

foreach ($path in $tempPaths) {
    Get-ChildItem -Path $path -Recurse -Force | 
    Where-Object {$_.LastAccessTime -lt (Get-Date).AddDays(-7)} |
    Remove-Item -Force -Recurse -ErrorAction SilentlyContinue
}

# Clean old log files
Get-ChildItem -Path "C:\Logs" -Filter "*.log" | 
Where-Object {$_.LastWriteTime -lt (Get-Date).AddDays(-30)} |
Remove-Item -Force

# Disk space reporting
function Get-DiskSpaceReport {
    param([string[]]$Paths = @("C:\"))
    
    foreach ($path in $Paths) {
        $folderSizes = Get-ChildItem -Path $path -Directory | ForEach-Object {
            $size = (Get-ChildItem -Path $_.FullName -Recurse -File | Measure-Object -Property Length -Sum).Sum
            [PSCustomObject]@{
                Folder = $_.Name
                'Size (GB)' = [math]::Round($size / 1GB, 2)
                Path = $_.FullName
            }
        } | Sort-Object 'Size (GB)' -Descending
        
        return $folderSizes
    }
}
```

**Usage**:

```powershell
# Organize media files by date
function Organize-MediaFiles {
    param(
        [string]$SourcePath,
        [string]$DestinationPath
    )
    
    $mediaExtensions = @(".jpg", ".jpeg", ".png", ".mp4", ".mov", ".avi")
    
    Get-ChildItem -Path $SourcePath -Recurse -File | 
    Where-Object {$_.Extension.ToLower() -in $mediaExtensions} |
    ForEach-Object {
        try {
            # Try to get date from file metadata, fall back to creation date
            $dateFolder = $_.CreationTime.ToString("yyyy\\MM - MMMM")
            $destFolder = Join-Path $DestinationPath $dateFolder
            
            if (-not (Test-Path $destFolder)) {
                New-Item -ItemType Directory -Path $destFolder -Force
            }
            
            $destFile = Join-Path $destFolder $_.Name
            Move-Item $_.FullName -Destination $destFile -Force
            Write-Host "Moved: $($_.Name) to $dateFolder" -ForegroundColor Green
        }
        catch {
            Write-Host "Error processing $($_.Name): $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

# Backup script with compression and rotation
function Start-IncrementalBackup {
    param(
        [string]$SourcePath,
        [string]$BackupPath,
        [int]$RetentionDays = 30
    )
    
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $backupFolder = Join-Path $BackupPath "Backup_$timestamp"
    
    # Create backup
    Write-Host "Starting backup of $SourcePath..."
    robocopy $SourcePath $backupFolder /MIR /R:3 /W:5 /MT:8 /LOG:"$backupFolder\backup.log"
    
    # Compress backup
    Write-Host "Compressing backup..."
    Compress-Archive -Path $backupFolder -DestinationPath "$backupFolder.zip" -Force
    Remove-Item $backupFolder -Recurse -Force
    
    # Cleanup old backups
    Get-ChildItem -Path $BackupPath -Filter "Backup_*.zip" |
    Where-Object {$_.LastWriteTime -lt (Get-Date).AddDays(-$RetentionDays)} |
    Remove-Item -Force
    
    Write-Host "Backup completed: $backupFolder.zip"
}

# Find and remove empty directories
function Remove-EmptyDirectories {
    param([string]$Path)
    
    do {
        $dirs = Get-ChildItem -Path $Path -Recurse -Directory | 
        Where-Object {(Get-ChildItem -Path $_.FullName -Force).Count -eq 0}
        
        $dirs | ForEach-Object {
            Write-Host "Removing empty directory: $($_.FullName)" -ForegroundColor Yellow
            Remove-Item $_.FullName -Force
        }
    } while ($dirs.Count -gt 0)
}

# Generate file inventory report
function New-FileInventoryReport {
    param(
        [string]$Path,
        [string]$ReportPath = "inventory_report.csv"
    )
    
    Write-Host "Generating file inventory for $Path..."
    
    Get-ChildItem -Path $Path -Recurse -File | 
    Select-Object @{
        Name = 'FileName'
        Expression = {$_.Name}
    }, @{
        Name = 'Directory'
        Expression = {$_.DirectoryName}
    }, @{
        Name = 'SizeKB'
        Expression = {[math]::Round($_.Length / 1KB, 2)}
    }, @{
        Name = 'Extension'
        Expression = {$_.Extension}
    }, @{
        Name = 'Created'
        Expression = {$_.CreationTime}
    }, @{
        Name = 'Modified'
        Expression = {$_.LastWriteTime}
    }, @{
        Name = 'Accessed'
        Expression = {$_.LastAccessTime}
    } | Export-Csv -Path $ReportPath -NoTypeInformation
    
    Write-Host "Report saved to: $ReportPath"
}

# Monitor folder size changes
function Watch-FolderSize {
    param(
        [string]$Path,
        [int]$IntervalSeconds = 60
    )
    
    $lastSize = 0
    
    while ($true) {
        $currentSize = (Get-ChildItem -Path $Path -Recurse -File | Measure-Object -Property Length -Sum).Sum
        $currentSizeGB = [math]::Round($currentSize / 1GB, 2)
        
        if ($lastSize -ne 0) {
            $change = $currentSize - $lastSize
            $changeGB = [math]::Round($change / 1GB, 2)
            
            if ($change -ne 0) {
                $status = if ($change -gt 0) { "INCREASED" } else { "DECREASED" }
                Write-Host "$(Get-Date): Folder size $status by $changeGB GB (Total: $currentSizeGB GB)" -ForegroundColor $(if ($change -gt 0) {"Red"} else {"Green"})
            }
        }
        
        $lastSize = $currentSize
        Start-Sleep -Seconds $IntervalSeconds
    }
}
```

**Notes**:

- **Performance**: For large operations, consider using `-Parallel` with ForEach-Object in PowerShell 7+
- **Error Handling**: Always include proper error handling for file operations
- **Permissions**: Some operations require elevated privileges
- **Network Drives**: File operations on network drives may be slower and less reliable
- **Robocopy**: For large-scale file operations, consider using robocopy for better performance
- **Backup Strategy**: Always test restore procedures and maintain multiple backup copies
- **File Locks**: Be aware of file locks when working with active files
- **Unicode**: Use UTF-8 encoding for international file names and content

**Related Snippets**:

- [PowerShell Basics](powershell-basics.md) - Essential PowerShell commands and fundamentals  
- [System Administration](system-admin.md) - Advanced system management tasks
- [Network Operations](network-operations.md) - Network file operations and remote management
- [Automation Scripts](automation-scripts.md) - Scheduled file operations and workflows
