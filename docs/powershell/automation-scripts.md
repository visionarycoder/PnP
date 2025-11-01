# PowerShell Automation Scripts

**Description**: Advanced PowerShell automation scripts for system administration, monitoring, and task scheduling.

**Language/Technology**: PowerShell / Automation / Task Scheduling

**Code**:

```powershell
# ============================================
# Scheduled Task Management
# ============================================

# Create scheduled task
$action = New-ScheduledTaskAction -Execute "PowerShell.exe" -Argument "-File C:\Scripts\Backup.ps1"
$trigger = New-ScheduledTaskTrigger -Daily -At "2:00AM"
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -RunOnlyIfNetworkAvailable
$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest

Register-ScheduledTask -TaskName "Daily Backup" -Action $action -Trigger $trigger -Settings $settings -Principal $principal

# Modify scheduled task
$task = Get-ScheduledTask -TaskName "Daily Backup"
$task.Triggers[0].StartBoundary = "2022-01-01T03:00:00"  # Change to 3:00 AM
Set-ScheduledTask -InputObject $task

# Task management
Get-ScheduledTask                                        # List all tasks
Get-ScheduledTask -TaskName "Daily Backup" | Get-ScheduledTaskInfo  # Task details
Start-ScheduledTask -TaskName "Daily Backup"            # Run task immediately
Stop-ScheduledTask -TaskName "Daily Backup"             # Stop running task
Unregister-ScheduledTask -TaskName "Daily Backup" -Confirm:$false   # Delete task

# ============================================
# Event Log Monitoring and Alerting
# ============================================

# Monitor event logs for specific events
function Start-EventLogMonitoring {
    param(
        [string]$LogName = "System",
        [string[]]$EventIDs = @("1074", "1076", "6005", "6006"),  # Shutdown/startup events
        [string]$EmailTo = "admin@company.com",
        [string]$SMTPServer = "smtp.company.com"
    )
    
    Register-WmiEvent -Query "SELECT * FROM Win32_NTLogEvent WHERE LogFile='$LogName'" -Action {
        $event = $Event.SourceEventArgs.NewEvent
        
        if ($event.EventCode -in $using:EventIDs) {
            $subject = "Event Alert: $($event.EventCode) on $($env:COMPUTERNAME)"
            $body = @"
Event Details:
Computer: $($env:COMPUTERNAME)
Event ID: $($event.EventCode)
Source: $($event.SourceName)
Time: $($event.TimeGenerated)
Message: $($event.Message)
"@
            
            try {
                Send-MailMessage -To $using:EmailTo -Subject $subject -Body $body -SmtpServer $using:SMTPServer -From "alerts@company.com"
                Write-Host "Alert sent for Event ID: $($event.EventCode)"
            } catch {
                Write-Host "Failed to send alert: $($_.Exception.Message)"
            }
        }
    }
}

# Event log analysis
function Analyze-EventLogs {
    param(
        [string]$LogName = "System",
        [int]$Hours = 24
    )
    
    $startTime = (Get-Date).AddHours(-$Hours)
    $events = Get-WinEvent -FilterHashtable @{LogName=$LogName; StartTime=$startTime}
    
    $summary = $events | Group-Object Id | Sort-Object Count -Descending |
    Select-Object @{Name="EventID";Expression={$_.Name}}, Count, @{Name="Description";Expression={(Get-WinEvent -FilterHashtable @{LogName=$LogName; Id=$_.Name} -MaxEvents 1).LevelDisplayName}}
    
    Write-Host "Event Summary for last $Hours hours:" -ForegroundColor Green
    $summary | Format-Table -AutoSize
    
    # Critical events
    $criticalEvents = $events | Where-Object {$_.LevelDisplayName -eq "Error" -or $_.LevelDisplayName -eq "Critical"}
    if ($criticalEvents) {
        Write-Host "`nCritical Events:" -ForegroundColor Red
        $criticalEvents | Select-Object TimeCreated, Id, LevelDisplayName, Message | Format-Table -Wrap
    }
}

# ============================================
# System Health Monitoring
# ============================================

# Comprehensive system health check
function Get-SystemHealthReport {
    param([string]$OutputPath = "C:\Reports\SystemHealth_$(Get-Date -Format 'yyyyMMdd_HHmmss').html")
    
    $report = @{
        ComputerName = $env:COMPUTERNAME
        ReportTime = Get-Date
        OS = Get-CimInstance Win32_OperatingSystem
        CPU = Get-CimInstance Win32_Processor
        Memory = Get-CimInstance Win32_PhysicalMemory
        Disk = Get-CimInstance Win32_LogicalDisk | Where-Object {$_.DriveType -eq 3}
        Network = Get-CimInstance Win32_NetworkAdapterConfiguration | Where-Object {$_.IPEnabled -eq $true}
        Services = Get-Service | Where-Object {$_.StartType -eq "Automatic" -and $_.Status -ne "Running"}
        Processes = Get-Process | Sort-Object CPU -Descending | Select-Object -First 10
        EventErrors = Get-WinEvent -FilterHashtable @{LogName="System"; Level=2} -MaxEvents 10 -ErrorAction SilentlyContinue
    }
    
    # Generate HTML report
    $html = @"
<!DOCTYPE html>
<html>
<head>
    <title>System Health Report - $($report.ComputerName)</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        h1, h2 { color: #2E8B57; }
        table { border-collapse: collapse; width: 100%; margin: 10px 0; }
        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
        th { background-color: #f2f2f2; }
        .warning { color: #FF6347; }
        .ok { color: #32CD32; }
    </style>
</head>
<body>
    <h1>System Health Report</h1>
    <p><strong>Computer:</strong> $($report.ComputerName)</p>
    <p><strong>Report Time:</strong> $($report.ReportTime)</p>
    
    <h2>System Information</h2>
    <table>
        <tr><th>Operating System</th><td>$($report.OS.Caption) $($report.OS.Version)</td></tr>
        <tr><th>Total Memory</th><td>$([math]::Round($report.OS.TotalVisibleMemorySize/1MB, 2)) GB</td></tr>
        <tr><th>Available Memory</th><td>$([math]::Round($report.OS.FreePhysicalMemory/1MB, 2)) GB</td></tr>
        <tr><th>CPU</th><td>$($report.CPU[0].Name)</td></tr>
        <tr><th>CPU Usage</th><td>$(Get-CimInstance Win32_PerfRawData_PerfOS_Processor | Where-Object {$_.Name -eq "_Total"} | ForEach-Object {100 - [math]::Round($_.PercentIdleTime / $_.TimeStamp_Sys100NS * 100, 2)})%</td></tr>
    </table>
    
    <h2>Disk Usage</h2>
    <table>
        <tr><th>Drive</th><th>Size (GB)</th><th>Free (GB)</th><th>% Free</th><th>Status</th></tr>
"@
    
    foreach ($disk in $report.Disk) {
        $freePercent = [math]::Round(($disk.FreeSpace / $disk.Size) * 100, 2)
        $status = if ($freePercent -lt 10) { "class='warning'>Critical" } elseif ($freePercent -lt 20) { "class='warning'>Warning" } else { "class='ok'>OK" }
        
        $html += "<tr><td>$($disk.DeviceID)</td><td>$([math]::Round($disk.Size/1GB, 2))</td><td>$([math]::Round($disk.FreeSpace/1GB, 2))</td><td>$freePercent%</td><td $status</td></tr>"
    }
    
    $html += @"
    </table>
    
    <h2>Failed Services</h2>
    <table>
        <tr><th>Service Name</th><th>Display Name</th><th>Status</th></tr>
"@
    
    foreach ($service in $report.Services) {
        $html += "<tr><td>$($service.Name)</td><td>$($service.DisplayName)</td><td class='warning'>$($service.Status)</td></tr>"
    }
    
    $html += "</table></body></html>"
    
    $html | Out-File -FilePath $OutputPath -Encoding UTF8
    Write-Host "System health report saved to: $OutputPath"
}

# Performance monitoring
function Start-PerformanceMonitoring {
    param(
        [string[]]$Counters = @(
            "\Processor(_Total)\% Processor Time",
            "\Memory\Available MBytes",
            "\PhysicalDisk(_Total)\% Disk Time",
            "\Network Interface(*)\Bytes Total/sec"
        ),
        [int]$IntervalSeconds = 5,
        [int]$SampleCount = 60,
        [string]$OutputFile = "C:\Logs\Performance_$(Get-Date -Format 'yyyyMMdd_HHmmss').csv"
    )
    
    $samples = @()
    
    for ($i = 1; $i -le $SampleCount; $i++) {
        $timestamp = Get-Date
        $counterData = Get-Counter -Counter $Counters -SampleInterval $IntervalSeconds
        
        foreach ($sample in $counterData.CounterSamples) {
            $samples += [PSCustomObject]@{
                Timestamp = $timestamp
                Counter = $sample.Path
                Value = $sample.CookedValue
                Instance = $sample.InstanceName
            }
        }
        
        Write-Progress -Activity "Performance Monitoring" -Status "Sample $i of $SampleCount" -PercentComplete (($i / $SampleCount) * 100)
    }
    
    $samples | Export-Csv -Path $OutputFile -NoTypeInformation
    Write-Host "Performance data saved to: $OutputFile"
}

# ============================================
# Automated Backup Solutions
# ============================================

# Automated file backup with rotation
function Start-AutomatedBackup {
    param(
        [string[]]$SourcePaths = @("C:\Important", "C:\Users\Documents"),
        [string]$BackupDestination = "\\BackupServer\Backups\$env:COMPUTERNAME",
        [int]$RetentionDays = 30,
        [string]$LogFile = "C:\Logs\Backup.log"
    )
    
    function Write-BackupLog {
        param([string]$Message, [string]$Level = "INFO")
        $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        $logEntry = "$timestamp [$Level] $Message"
        Add-Content -Path $LogFile -Value $logEntry
        Write-Host $logEntry -ForegroundColor $(if ($Level -eq "ERROR") { "Red" } elseif ($Level -eq "WARN") { "Yellow" } else { "Green" })
    }
    
    Write-BackupLog "Starting automated backup process"
    
    # Create backup directory with timestamp
    $backupFolder = Join-Path $BackupDestination (Get-Date -Format "yyyyMMdd_HHmmss")
    
    try {
        New-Item -ItemType Directory -Path $backupFolder -Force | Out-Null
        Write-BackupLog "Created backup directory: $backupFolder"
    } catch {
        Write-BackupLog "Failed to create backup directory: $($_.Exception.Message)" "ERROR"
        return
    }
    
    # Backup each source path
    foreach ($sourcePath in $SourcePaths) {
        if (Test-Path $sourcePath) {
            $destinationPath = Join-Path $backupFolder (Split-Path $sourcePath -Leaf)
            
            try {
                Write-BackupLog "Backing up: $sourcePath to $destinationPath"
                robocopy $sourcePath $destinationPath /MIR /R:3 /W:5 /LOG+:$LogFile /TEE /NP
                
                if ($LASTEXITCODE -le 7) {  # Robocopy exit codes 0-7 are considered successful
                    Write-BackupLog "Successfully backed up: $sourcePath"
                } else {
                    Write-BackupLog "Backup completed with warnings: $sourcePath (Exit code: $LASTEXITCODE)" "WARN"
                }
            } catch {
                Write-BackupLog "Failed to backup: $sourcePath - $($_.Exception.Message)" "ERROR"
            }
        } else {
            Write-BackupLog "Source path not found: $sourcePath" "WARN"
        }
    }
    
    # Cleanup old backups
    try {
        $cutoffDate = (Get-Date).AddDays(-$RetentionDays)
        $oldBackups = Get-ChildItem $BackupDestination -Directory | 
                     Where-Object {$_.CreationTime -lt $cutoffDate}
        
        foreach ($oldBackup in $oldBackups) {
            Remove-Item $oldBackup.FullName -Recurse -Force
            Write-BackupLog "Removed old backup: $($oldBackup.Name)"
        }
    } catch {
        Write-BackupLog "Failed to cleanup old backups: $($_.Exception.Message)" "WARN"
    }
    
    Write-BackupLog "Backup process completed"
}

# ============================================
# Windows Update Automation
# ============================================

# Automated Windows Update management
function Invoke-WindowsUpdateAutomation {
    param(
        [switch]$DownloadOnly,
        [switch]$InstallOnly,
        [string[]]$ExcludeKBs = @(),
        [string]$LogFile = "C:\Logs\WindowsUpdate.log"
    )
    
    # Install PSWindowsUpdate module if not available
    if (-not (Get-Module -ListAvailable -Name PSWindowsUpdate)) {
        Install-Module PSWindowsUpdate -Force -Scope CurrentUser
    }
    
    Import-Module PSWindowsUpdate
    
    function Write-UpdateLog {
        param([string]$Message)
        $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        $logEntry = "$timestamp $Message"
        Add-Content -Path $LogFile -Value $logEntry
        Write-Host $logEntry
    }
    
    Write-UpdateLog "Starting Windows Update automation"
    
    # Get available updates
    $updates = Get-WUList -MicrosoftUpdate
    
    if ($ExcludeKBs) {
        $updates = $updates | Where-Object {
            $kb = $_.KBArticleIDs
            -not ($ExcludeKBs | Where-Object {$kb -contains $_})
        }
    }
    
    Write-UpdateLog "Found $($updates.Count) available updates"
    
    if ($updates.Count -eq 0) {
        Write-UpdateLog "No updates available"
        return
    }
    
    # List updates
    foreach ($update in $updates) {
        Write-UpdateLog "Available: $($update.Title) ($($update.Size) bytes)"
    }
    
    if (-not $InstallOnly) {
        Write-UpdateLog "Downloading updates..."
        Get-WUInstall -MicrosoftUpdate -Download -AcceptAll -IgnoreReboot
    }
    
    if (-not $DownloadOnly) {
        Write-UpdateLog "Installing updates..."
        $installResult = Get-WUInstall -MicrosoftUpdate -Install -AcceptAll -AutoReboot
        
        foreach ($result in $installResult) {
            Write-UpdateLog "Installed: $($result.Title) - Status: $($result.Result)"
        }
    }
    
    Write-UpdateLog "Windows Update automation completed"
}

# ============================================
# Network Drive Mapping Automation
# ============================================

# Automated network drive mapping with credential management
function Set-NetworkDriveMappings {
    param(
        [hashtable]$DriveMappings = @{
            "H:" = "\\FileServer\Home\$env:USERNAME"
            "S:" = "\\FileServer\Shared"
            "P:" = "\\FileServer\Projects"
        },
        [PSCredential]$Credential
    )
    
    if (-not $Credential) {
        $Credential = Get-Credential -Message "Enter network credentials"
    }
    
    foreach ($drive in $DriveMappings.GetEnumerator()) {
        try {
            # Remove existing mapping if present
            if (Get-PSDrive -Name $drive.Key.TrimEnd(':') -ErrorAction SilentlyContinue) {
                Remove-PSDrive -Name $drive.Key.TrimEnd(':') -Force
            }
            
            # Map network drive
            New-PSDrive -Name $drive.Key.TrimEnd(':') -PSProvider FileSystem -Root $drive.Value -Credential $Credential -Persist
            Write-Host "Mapped $($drive.Key) to $($drive.Value)" -ForegroundColor Green
        } catch {
            Write-Host "Failed to map $($drive.Key): $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

# ============================================
# Certificate Management Automation
# ============================================

# Automated certificate monitoring and renewal alerts
function Monitor-Certificates {
    param(
        [int]$ExpirationThresholdDays = 30,
        [string]$EmailTo = "admin@company.com",
        [string]$SMTPServer = "smtp.company.com"
    )
    
    $expiringCerts = @()
    $stores = @("LocalMachine\My", "LocalMachine\Root", "CurrentUser\My")
    
    foreach ($store in $stores) {
        $certs = Get-ChildItem "Cert:\$store" | Where-Object {
            $_.NotAfter -lt (Get-Date).AddDays($ExpirationThresholdDays) -and
            $_.NotAfter -gt (Get-Date)
        }
        
        foreach ($cert in $certs) {
            $expiringCerts += [PSCustomObject]@{
                Subject = $cert.Subject
                Thumbprint = $cert.Thumbprint
                ExpirationDate = $cert.NotAfter
                DaysUntilExpiration = ($cert.NotAfter - (Get-Date)).Days
                Store = $store
            }
        }
    }
    
    if ($expiringCerts) {
        $emailBody = "The following certificates are expiring soon:`n`n"
        $emailBody += $expiringCerts | ForEach-Object {
            "Subject: $($_.Subject)`nExpires: $($_.ExpirationDate)`nDays remaining: $($_.DaysUntilExpiration)`nStore: $($_.Store)`n`n"
        }
        
        try {
            Send-MailMessage -To $EmailTo -Subject "Certificate Expiration Alert - $env:COMPUTERNAME" -Body $emailBody -SmtpServer $SMTPServer -From "certificates@company.com"
            Write-Host "Certificate expiration alert sent" -ForegroundColor Yellow
        } catch {
            Write-Host "Failed to send certificate alert: $($_.Exception.Message)" -ForegroundColor Red
        }
    } else {
        Write-Host "No certificates expiring within $ExpirationThresholdDays days" -ForegroundColor Green
    }
    
    return $expiringCerts
}
```

**Usage**:

```powershell
# Create a comprehensive automation script
function Start-DailyMaintenanceAutomation {
    param(
        [string]$LogPath = "C:\Logs",
        [string]$ReportPath = "C:\Reports",
        [string]$BackupPath = "\\BackupServer\Backups\$env:COMPUTERNAME"
    )
    
    # Ensure directories exist
    @($LogPath, $ReportPath) | ForEach-Object {
        if (-not (Test-Path $_)) {
            New-Item -ItemType Directory -Path $_ -Force
        }
    }
    
    $logFile = "$LogPath\DailyMaintenance_$(Get-Date -Format 'yyyyMMdd').log"
    
    function Write-MaintenanceLog {
        param([string]$Message, [string]$Level = "INFO")
        $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        $logEntry = "$timestamp [$Level] $Message"
        Add-Content -Path $logFile -Value $logEntry
        Write-Host $logEntry
    }
    
    Write-MaintenanceLog "Starting daily maintenance automation"
    
    try {
        # 1. System Health Check
        Write-MaintenanceLog "Running system health check..."
        Get-SystemHealthReport -OutputPath "$ReportPath\HealthReport_$(Get-Date -Format 'yyyyMMdd').html"
        
        # 2. Event Log Analysis
        Write-MaintenanceLog "Analyzing event logs..."
        Analyze-EventLogs -Hours 24
        
        # 3. Certificate Check
        Write-MaintenanceLog "Checking certificate expiration..."
        $expiringCerts = Monitor-Certificates -ExpirationThresholdDays 30
        
        # 4. Disk Space Check
        Write-MaintenanceLog "Checking disk space..."
        $disks = Get-CimInstance Win32_LogicalDisk | Where-Object {$_.DriveType -eq 3}
        foreach ($disk in $disks) {
            $freePercent = [math]::Round(($disk.FreeSpace / $disk.Size) * 100, 2)
            if ($freePercent -lt 10) {
                Write-MaintenanceLog "CRITICAL: Drive $($disk.DeviceID) only has $freePercent% free space" "ERROR"
            } elseif ($freePercent -lt 20) {
                Write-MaintenanceLog "WARNING: Drive $($disk.DeviceID) has $freePercent% free space" "WARN"
            }
        }
        
        # 5. Service Status Check
        Write-MaintenanceLog "Checking critical services..."
        $criticalServices = @("Spooler", "BITS", "Themes", "AudioSrv", "Dhcp")
        foreach ($serviceName in $criticalServices) {
            $service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
            if ($service -and $service.Status -ne "Running") {
                Write-MaintenanceLog "WARNING: Service $serviceName is $($service.Status)" "WARN"
                try {
                    Start-Service -Name $serviceName
                    Write-MaintenanceLog "Started service: $serviceName"
                } catch {
                    Write-MaintenanceLog "Failed to start service $serviceName`: $($_.Exception.Message)" "ERROR"
                }
            }
        }
        
        # 6. Temporary File Cleanup
        Write-MaintenanceLog "Cleaning temporary files..."
        $tempPaths = @(
            "$env:TEMP\*",
            "$env:WINDIR\Temp\*",
            "$env:LOCALAPPDATA\Temp\*"
        )
        
        $totalCleaned = 0
        foreach ($path in $tempPaths) {
            try {
                $files = Get-ChildItem $path -Recurse -Force -ErrorAction SilentlyContinue
                $size = ($files | Measure-Object -Property Length -Sum).Sum
                Remove-Item $path -Recurse -Force -ErrorAction SilentlyContinue
                $totalCleaned += $size
            } catch {
                # Silently continue if files are in use
            }
        }
        
        Write-MaintenanceLog "Cleaned $([math]::Round($totalCleaned / 1MB, 2)) MB of temporary files"
        
        # 7. Windows Update Check
        Write-MaintenanceLog "Checking for Windows updates..."
        if (Get-Module -ListAvailable -Name PSWindowsUpdate) {
            $updates = Get-WUList -MicrosoftUpdate
            Write-MaintenanceLog "Found $($updates.Count) available updates"
        }
        
        Write-MaintenanceLog "Daily maintenance completed successfully"
        
    } catch {
        Write-MaintenanceLog "Daily maintenance failed: $($_.Exception.Message)" "ERROR"
    }
}

# Schedule the daily maintenance
function Install-MaintenanceSchedule {
    $scriptPath = "C:\Scripts\DailyMaintenance.ps1"
    
    # Create the maintenance script
    $scriptContent = @'
# Daily maintenance automation script
Import-Module -Name "C:\Scripts\MaintenanceModule.psm1" -Force
Start-DailyMaintenanceAutomation
'@
    
    $scriptContent | Out-File -FilePath $scriptPath -Encoding UTF8
    
    # Create scheduled task
    $action = New-ScheduledTaskAction -Execute "PowerShell.exe" -Argument "-ExecutionPolicy Bypass -File `"$scriptPath`""
    $trigger = New-ScheduledTaskTrigger -Daily -At "2:00AM"
    $settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -RunOnlyIfNetworkAvailable
    $principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest
    
    Register-ScheduledTask -TaskName "Daily System Maintenance" -Action $action -Trigger $trigger -Settings $settings -Principal $principal
    
    Write-Host "Daily maintenance schedule installed successfully"
}

# System monitoring dashboard
function Show-SystemDashboard {
    while ($true) {
        Clear-Host
        Write-Host "=== System Dashboard - $env:COMPUTERNAME ===" -ForegroundColor Green
        Write-Host "Last Updated: $(Get-Date)" -ForegroundColor Yellow
        
        # CPU Usage
        $cpu = Get-CimInstance Win32_PerfRawData_PerfOS_Processor | Where-Object {$_.Name -eq "_Total"}
        $cpuUsage = 100 - [math]::Round($cpu.PercentIdleTime / $cpu.TimeStamp_Sys100NS * 100, 1)
        Write-Host "`nCPU Usage: $cpuUsage%" -ForegroundColor $(if ($cpuUsage -gt 80) { "Red" } elseif ($cpuUsage -gt 60) { "Yellow" } else { "Green" })
        
        # Memory Usage
        $memory = Get-CimInstance Win32_OperatingSystem
        $memoryUsage = [math]::Round((($memory.TotalVisibleMemorySize - $memory.FreePhysicalMemory) / $memory.TotalVisibleMemorySize) * 100, 1)
        Write-Host "Memory Usage: $memoryUsage%" -ForegroundColor $(if ($memoryUsage -gt 90) { "Red" } elseif ($memoryUsage -gt 75) { "Yellow" } else { "Green" })
        
        # Disk Usage
        Write-Host "`nDisk Usage:"
        $disks = Get-CimInstance Win32_LogicalDisk | Where-Object {$_.DriveType -eq 3}
        foreach ($disk in $disks) {
            $freePercent = [math]::Round(($disk.FreeSpace / $disk.Size) * 100, 1)
            $color = if ($freePercent -lt 10) { "Red" } elseif ($freePercent -lt 20) { "Yellow" } else { "Green" }
            Write-Host "  $($disk.DeviceID) $($freePercent)% free" -ForegroundColor $color
        }
        
        # Network Connections
        $connections = Get-NetTCPConnection -State Established
        Write-Host "`nActive Network Connections: $($connections.Count)"
        
        # Recent Events
        $recentErrors = Get-WinEvent -FilterHashtable @{LogName="System"; Level=2; StartTime=(Get-Date).AddHours(-1)} -MaxEvents 5 -ErrorAction SilentlyContinue
        if ($recentErrors) {
            Write-Host "`nRecent System Errors: $($recentErrors.Count)" -ForegroundColor Red
        } else {
            Write-Host "`nNo recent system errors" -ForegroundColor Green
        }
        
        Write-Host "`nPress Ctrl+C to exit, refreshing in 30 seconds..."
        Start-Sleep -Seconds 30
    }
}
```

**Notes**:

- **Execution Policy**: May require `Set-ExecutionPolicy RemoteSigned` or similar
- **Administrative Rights**: Most automation tasks require elevated privileges
- **Error Handling**: All production scripts should include comprehensive error handling
- **Logging**: Implement detailed logging for audit and troubleshooting purposes
- **Testing**: Always test automation scripts in non-production environments first
- **Scheduling**: Use Task Scheduler for reliable script execution
- **Security**: Store credentials securely using Windows Credential Manager or encrypted files
- **Monitoring**: Implement alerting for failed automation tasks
- **Documentation**: Maintain detailed documentation of all automated processes

**Related Snippets**:

- [PowerShell Basics](powershell-basics.md) - Fundamental PowerShell commands and concepts
- [System Administration](system-admin.md) - Core system management tasks
- [File Operations](file-operations.md) - File system automation and management
- [Network Operations](network-operations.md) - Network-related automation tasks
- [Active Directory](active-directory.md) - AD automation and user management
