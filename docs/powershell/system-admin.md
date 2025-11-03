# Enterprise PowerShell System Administration

**Description**: Production-ready PowerShell automation for enterprise system administration, compliance monitoring, and infrastructure management.

**Language/Technology**: PowerShell 7+ / Cross-Platform Administration

**Code**:

```powershell
# ============================================
# User and Group Management
# ============================================

# Local user management
Get-LocalUser                                    # List all local users
New-LocalUser -Name "NewUser" -Description "Test User" -NoPassword
Set-LocalUser -Name "NewUser" -Password (ConvertTo-SecureString "P@ssw0rd" -AsPlainText -Force)
Remove-LocalUser -Name "NewUser"                # Delete local user

# Local group management
Get-LocalGroup                                   # List all local groups
New-LocalGroup -Name "CustomGroup" -Description "Custom group"
Add-LocalGroupMember -Group "CustomGroup" -Member "Username"
Remove-LocalGroupMember -Group "CustomGroup" -Member "Username"

# Active Directory (requires RSAT/AD module)
Import-Module ActiveDirectory
Get-ADUser -Filter * -Properties *              # Get all AD users
Get-ADGroup -Filter * | Select-Object Name      # Get all AD groups
New-ADUser -Name "John Doe" -SamAccountName "jdoe" -UserPrincipalName "jdoe@domain.com"

# ============================================
# Service Management
# ============================================

# Service operations
Get-Service                                      # List all services
Get-Service | Where-Object {$_.Status -eq "Stopped"} # Stopped services
Start-Service -Name "Spooler"                   # Start service
Stop-Service -Name "Spooler"                    # Stop service
Restart-Service -Name "Spooler"                 # Restart service
Set-Service -Name "Spooler" -StartupType Automatic # Set startup type

# Service configuration
New-Service -Name "CustomService" -BinaryPathName "C:\MyApp\service.exe"
Set-Service -Name "CustomService" -Description "My custom service"
Remove-Service -Name "CustomService"            # Remove service (PowerShell 6+)

# ============================================
# Registry Management
# ============================================

# Registry operations
Get-ChildItem -Path "HKLM:\SOFTWARE"            # Browse registry
Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion" -Name "ProductName"
New-Item -Path "HKCU:\SOFTWARE\MyApp" -Force    # Create registry key
Set-ItemProperty -Path "HKCU:\SOFTWARE\MyApp" -Name "Setting" -Value "Value"
Remove-Item -Path "HKCU:\SOFTWARE\MyApp" -Recurse # Delete registry key

# Registry backup and restore
reg export "HKEY_LOCAL_MACHINE\SOFTWARE\MyApp" "backup.reg"
reg import "backup.reg"

# ============================================
# Process and Performance Management
# ============================================

# Process management
Get-Process                                      # List all processes
Get-Process | Sort-Object CPU -Descending       # Sort by CPU usage
Stop-Process -Name "notepad" -Force             # Kill process by name
Stop-Process -Id 1234                          # Kill process by PID
Start-Process -FilePath "notepad.exe"          # Start new process

# Performance monitoring
Get-Counter "\Processor(Total)\% Processor Time" -SampleInterval 1 -MaxSamples 5
Get-Counter "\Memory\Available MBytes"          # Memory usage
Get-WmiObject -Class Win32_LogicalDisk | Select-Object DeviceID, @{Name="FreeSpace(GB)";Expression={[math]::Round($_.FreeSpace/1GB,2)}}

# ============================================
# Network Administration
# ============================================

# Network configuration
Get-NetAdapter                                   # Network adapters
Get-NetIPAddress                                # IP configuration
New-NetIPAddress -InterfaceAlias "Ethernet" -IPAddress "192.168.1.100" -PrefixLength 24
Remove-NetIPAddress -IPAddress "192.168.1.100" -Confirm:$false

# DNS management
Get-DnsClientServerAddress                      # DNS servers
Set-DnsClientServerAddress -InterfaceAlias "Ethernet" -ServerAddresses "8.8.8.8","8.8.4.4"
Resolve-DnsName "google.com"                   # DNS lookup
Clear-DnsClientCache                            # Clear DNS cache

# Firewall management
Get-NetFirewallRule                             # List firewall rules
New-NetFirewallRule -DisplayName "Allow Port 80" -Direction Inbound -Protocol TCP -LocalPort 80
Remove-NetFirewallRule -DisplayName "Allow Port 80"

# ============================================
# Scheduled Tasks
# ============================================

# Task management
Get-ScheduledTask                               # List scheduled tasks
$action = New-ScheduledTaskAction -Execute "PowerShell.exe" -Argument "-File C:\Scripts\backup.ps1"
$trigger = New-ScheduledTaskTrigger -Daily -At 2AM
Register-ScheduledTask -TaskName "DailyBackup" -Action $action -Trigger $trigger
Unregister-ScheduledTask -TaskName "DailyBackup" -Confirm:$false

# ============================================
# Event Log Management
# ============================================

# Event log operations
Get-EventLog -List                              # Available event logs
Get-EventLog -LogName System -Newest 50        # Recent system events
Get-EventLog -LogName Application -EntryType Error -Newest 10 # Application errors
Clear-EventLog -LogName Application             # Clear event log

# Windows Event Log (newer method)
Get-WinEvent -ListLog *                         # List all event logs
Get-WinEvent -LogName System -MaxEvents 10     # Get recent events
Get-WinEvent -FilterHashtable @{LogName='System'; Level=2} # Filter by level

# ============================================
# System Configuration
# ============================================

# Windows Features
Get-WindowsFeature                              # List Windows features (Server)
Enable-WindowsOptionalFeature -Online -FeatureName "IIS-WebServerRole"
Disable-WindowsOptionalFeature -Online -FeatureName "IIS-WebServerRole"

# System settings
Get-ComputerInfo                                # System information
Rename-Computer -NewName "NewComputerName"     # Rename computer
Add-Computer -DomainName "domain.com"          # Join domain
Remove-Computer -UnjoinDomainCredential (Get-Credential) # Leave domain

# Power management
powercfg /list                                  # List power schemes
powercfg /setactive SCHEME_GUID                # Set active power scheme
```

**Usage**:

```powershell
# System health check script
function Get-SystemHealth {
    Write-Host "=== System Health Check ===" -ForegroundColor Green
    
    # CPU usage
    $cpu = Get-Counter "\Processor(Total)\% Processor Time" -SampleInterval 1 -MaxSamples 3
    $avgCpu = ($cpu.CounterSamples | Measure-Object CookedValue -Average).Average
    Write-Host "Average CPU Usage: $([math]::Round($avgCpu, 2))%"
    
    # Memory usage
    $memory = Get-WmiObject -Class Win32_OperatingSystem
    $freeMemoryPercent = ($memory.FreePhysicalMemory / $memory.TotalVisibleMemorySize) * 100
    Write-Host "Free Memory: $([math]::Round($freeMemoryPercent, 2))%"
    
    # Disk space
    Get-WmiObject -Class Win32_LogicalDisk | ForEach-Object {
        $freePercent = ($_.FreeSpace / $_.Size) * 100
        Write-Host "Drive $($_.DeviceID) Free Space: $([math]::Round($freePercent, 2))%"
    }
    
    # Services status
    $stoppedServices = Get-Service | Where-Object {$_.Status -eq "Stopped" -and $_.StartType -eq "Automatic"}
    if ($stoppedServices) {
        Write-Host "Stopped Automatic Services:" -ForegroundColor Red
        $stoppedServices | Select-Object Name, Status | Format-Table
    }
}

# Bulk user creation from CSV
function New-UsersFromCSV {
    param([string]$CsvPath)
    
    $users = Import-Csv $CsvPath
    foreach ($user in $users) {
        try {
            $securePassword = ConvertTo-SecureString $user.Password -AsPlainText -Force
            New-LocalUser -Name $user.Username -Password $securePassword -FullName $user.FullName -Description $user.Description
            Write-Host "Created user: $($user.Username)" -ForegroundColor Green
        } catch {
            Write-Host "Failed to create user: $($user.Username) - $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

# Service monitoring with email alerts
function Watch-CriticalServices {
    param(
        [string[]]$ServiceNames = @("Spooler", "DHCP", "DNS"),
        [string]$EmailTo,
        [string]$SmtpServer
    )
    
    foreach ($serviceName in $ServiceNames) {
        $service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
        if (-not $service -or $service.Status -ne "Running") {
            $message = "Critical service '$serviceName' is not running on $env:COMPUTERNAME"
            Write-Host $message -ForegroundColor Red
            
            if ($EmailTo -and $SmtpServer) {
                Send-MailMessage -To $EmailTo -From "admin@company.com" -Subject "Service Alert" -Body $message -SmtpServer $SmtpServer
            }
        }
    }
}

# Registry cleanup for software uninstall
function Remove-SoftwareRegistryEntries {
    param([string]$SoftwareName)
    
    $uninstallPaths = @(
        "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*",
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*"
    )
    
    foreach ($path in $uninstallPaths) {
        Get-ItemProperty $path | Where-Object {$_.DisplayName -like "*$SoftwareName*"} | 
        ForEach-Object {
            Write-Host "Removing registry entry for: $($_.DisplayName)"
            Remove-Item $_.PSPath -Recurse -Force
        }
    }
}

# Network connectivity test with reporting
function Test-NetworkConnectivity {
    param([string[]]$Hosts = @("google.com", "microsoft.com", "github.com"))
    
    $results = @()
    foreach ($host in $Hosts) {
        $result = Test-NetConnection -ComputerName $host -Port 80
        $results += [PSCustomObject]@{
            Host = $host
            Reachable = $result.TcpTestSucceeded
            PingTime = $result.PingReplyDetails.RoundtripTime
        }
    }
    
    return $results | Format-Table -AutoSize
}
```

**Notes**:

- **Administrative Privileges**: Most system administration tasks require elevated PowerShell (Run as Administrator)
- **Remote Management**: Enable PowerShell remoting with `Enable-PSRemoting` for remote administration
- **Modules**: Some cmdlets require specific modules (ActiveDirectory, DnsClient, etc.)
- **Security**: Always validate input and use proper error handling in production scripts
- **Backup**: Always backup registry and system settings before making changes
- **Testing**: Test scripts in development environment before running in production
- **Logging**: Consider adding comprehensive logging for audit trails
- **Credentials**: Use secure methods for credential handling (`Get-Credential`, secure strings)

**Related Snippets**:

- [PowerShell Basics](powershell-basics.md) - Essential PowerShell commands and fundamentals
- [File Operations](file-operations.md) - Advanced file and directory management
- [Network Operations](network-operations.md) - Network configuration and monitoring
- [Active Directory](active-directory.md) - AD management and automation
