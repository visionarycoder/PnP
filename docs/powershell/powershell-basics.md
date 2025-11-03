# Modern PowerShell Fundamentals

**Description**: Enterprise PowerShell scripting patterns with modern syntax, error handling, and cross-platform compatibility
**Language/Technology**: PowerShell 7+ / Cross-Platform Administration

## Enterprise File Operations with Error Handling

**Code**:

```powershell
# ============================================================
# Advanced File Discovery and Management
# ============================================================

function Get-EnterpriseFileInfo {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateScript({ Test-Path $_ })]
        [string]$Path,
        
        [Parameter()]
        [string[]]$Include = @('*'),
        
        [Parameter()]
        [string[]]$Exclude = @(),
        
        [Parameter()]
        [switch]$Recurse,
        
        [Parameter()]
        [int]$Depth = -1
    )
    
    try {
        $getChildItemParams = @{
            Path = $Path
            Include = $Include
            Exclude = $Exclude
            Recurse = $Recurse
            ErrorAction = 'Stop'
        }
        
        if ($Depth -gt 0) {
            $getChildItemParams.Depth = $Depth
        }
        
        Get-ChildItem @getChildItemParams | 
        Select-Object @{
            Name = 'FullName'
            Expression = { $_.FullName }
        }, @{
            Name = 'SizeGB'
            Expression = { [Math]::Round($_.Length / 1GB, 3) }
        }, @{
            Name = 'LastModified'
            Expression = { $_.LastWriteTime }
        }, @{
            Name = 'Owner'
            Expression = { (Get-Acl $_.FullName).Owner }
        }, @{
            Name = 'IsReadOnly'
            Expression = { $_.IsReadOnly }
        }
    }
    catch {
        Write-Error "Failed to process path '$Path': $($_.Exception.Message)" -ErrorAction Stop
    }
}

# ============================================================
# Secure File Operations with Validation
# ============================================================

function Copy-ItemSecurely {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory, ValueFromPipeline)]
        [ValidateScript({ Test-Path $_ })]
        [string[]]$Source,
        
        [Parameter(Mandatory)]
        [string]$Destination,
        
        [Parameter()]
        [switch]$CreateDestination,
        
        [Parameter()]
        [switch]$Overwrite
    )
    
    begin {
        if ($CreateDestination -and -not (Test-Path $Destination)) {
            New-Item -ItemType Directory -Path $Destination -Force | Out-Null
            Write-Verbose "Created destination directory: $Destination"
        }
    }
    
    process {
        foreach ($sourceItem in $Source) {
            try {
                $sourceInfo = Get-Item $sourceItem
                $destinationPath = Join-Path $Destination $sourceInfo.Name
                
                if (Test-Path $destinationPath -and -not $Overwrite) {
                    Write-Warning "Destination exists and -Overwrite not specified: $destinationPath"
                    continue
                }
                
                if ($PSCmdlet.ShouldProcess($sourceItem, "Copy to $destinationPath")) {
                    Copy-Item -Path $sourceItem -Destination $destinationPath -Force:$Overwrite -ErrorAction Stop
                    Write-Verbose "Successfully copied: $sourceItem -> $destinationPath"
                }
            }
            catch {
                Write-Error "Failed to copy '$sourceItem': $($_.Exception.Message)" -ErrorAction Continue
            }
        }
    }
}

# ============================================================
# Content Management with Encoding Support
# ============================================================

function Set-FileContentSecurely {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [string]$Path,
        
        [Parameter(Mandatory, ValueFromPipeline)]
        [AllowEmptyString()]
        [string[]]$Content,
        
        [Parameter()]
        [System.Text.Encoding]$Encoding = [System.Text.Encoding]::UTF8,
        
        [Parameter()]
        [switch]$Append,
        
        [Parameter()]
        [switch]$CreateDirectory
    )
    
    begin {
        $allContent = @()
        
        if ($CreateDirectory) {
            $parentDir = Split-Path $Path -Parent
            if (-not (Test-Path $parentDir)) {
                New-Item -ItemType Directory -Path $parentDir -Force | Out-Null
            }
        }
    }
    
    process {
        $allContent += $Content
    }
    
    end {
        try {
            if ($PSCmdlet.ShouldProcess($Path, "Write content")) {
                if ($Append -and (Test-Path $Path)) {
                    Add-Content -Path $Path -Value $allContent -Encoding $Encoding.BodyName -ErrorAction Stop
                }
                else {
                    Set-Content -Path $Path -Value $allContent -Encoding $Encoding.BodyName -ErrorAction Stop
                }
                Write-Verbose "Successfully wrote content to: $Path"
            }
        }
        catch {
            Write-Error "Failed to write content to '$Path': $($_.Exception.Message)" -ErrorAction Stop
        }
    }
}

## Enterprise System Monitoring & Diagnostics

**Code**:

```powershell
# ============================================================
# Cross-Platform System Information Gathering
# ============================================================

function Get-SystemHealthReport {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string[]]$ComputerName = @($env:COMPUTERNAME),
        
        [Parameter()]
        [switch]$IncludeProcesses,
        
        [Parameter()]
        [switch]$IncludeServices,
        
        [Parameter()]
        [int]$TopProcessCount = 10
    )
    
    $results = foreach ($computer in $ComputerName) {
        try {
            Write-Verbose "Gathering system information for: $computer"
            
            $systemInfo = if ($IsWindows -or $PSVersionTable.PSVersion.Major -le 5) {
                # Windows-specific information
                Get-CimInstance -ClassName Win32_ComputerSystem -ComputerName $computer -ErrorAction Stop |
                Select-Object @{
                    Name = 'ComputerName'
                    Expression = { $_.Name }
                }, @{
                    Name = 'OperatingSystem'
                    Expression = { (Get-CimInstance -ClassName Win32_OperatingSystem -ComputerName $computer).Caption }
                }, @{
                    Name = 'TotalMemoryGB'
                    Expression = { [Math]::Round($_.TotalPhysicalMemory / 1GB, 2) }
                }, @{
                    Name = 'ProcessorCount'
                    Expression = { $_.NumberOfProcessors }
                }, @{
                    Name = 'LastBootTime'
                    Expression = { (Get-CimInstance -ClassName Win32_OperatingSystem -ComputerName $computer).LastBootUpTime }
                }
            }
            else {
                # Cross-platform information using uname and /proc
                [PSCustomObject]@{
                    ComputerName = $computer
                    OperatingSystem = if (Test-Path '/etc/os-release') { 
                        (Get-Content '/etc/os-release' | Where-Object { $_ -like 'PRETTY_NAME=*' } | 
                         ForEach-Object { $_ -replace 'PRETTY_NAME=', '' -replace '"', '' })
                    } else { 'Unknown Linux Distribution' }
                    TotalMemoryGB = if (Test-Path '/proc/meminfo') {
                        $memInfo = Get-Content '/proc/meminfo' | Where-Object { $_ -like 'MemTotal:*' }
                        [Math]::Round(($memInfo -split '\s+')[1] / 1024 / 1024, 2)
                    } else { 0 }
                    ProcessorCount = if (Test-Path '/proc/cpuinfo') {
                        (Get-Content '/proc/cpuinfo' | Where-Object { $_ -like 'processor*' }).Count
                    } else { 1 }
                    LastBootTime = Get-Date # Simplified for cross-platform
                }
            }
            
            # Performance metrics
            $performanceData = [PSCustomObject]@{
                CPUUsagePercent = if ($IsWindows -or $PSVersionTable.PSVersion.Major -le 5) {
                    (Get-CimInstance -ClassName Win32_Processor -ComputerName $computer | 
                     Measure-Object -Property LoadPercentage -Average).Average
                } else {
                    # Linux CPU usage approximation
                    if (Get-Command 'top' -ErrorAction SilentlyContinue) {
                        # Simplified CPU usage for demo - in production use more sophisticated methods
                        50 # Placeholder value
                    } else { 0 }
                }
                
                AvailableMemoryGB = if ($IsWindows -or $PSVersionTable.PSVersion.Major -le 5) {
                    [Math]::Round((Get-CimInstance -ClassName Win32_OperatingSystem -ComputerName $computer).FreePhysicalMemory / 1MB, 2)
                } else {
                    if (Test-Path '/proc/meminfo') {
                        $memInfo = Get-Content '/proc/meminfo' | Where-Object { $_ -like 'MemAvailable:*' }
                        [Math]::Round(($memInfo -split '\s+')[1] / 1024 / 1024, 2)
                    } else { 0 }
                }
                
                DiskSpaceInfo = if ($IsWindows -or $PSVersionTable.PSVersion.Major -le 5) {
                    Get-CimInstance -ClassName Win32_LogicalDisk -ComputerName $computer -Filter "DriveType=3" |
                    Select-Object @{
                        Name = 'Drive'
                        Expression = { $_.DeviceID }
                    }, @{
                        Name = 'SizeGB'
                        Expression = { [Math]::Round($_.Size / 1GB, 2) }
                    }, @{
                        Name = 'FreeGB'
                        Expression = { [Math]::Round($_.FreeSpace / 1GB, 2) }
                    }, @{
                        Name = 'PercentFree'
                        Expression = { [Math]::Round(($_.FreeSpace / $_.Size) * 100, 1) }
                    }
                } else {
                    # Linux disk space using df command
                    if (Get-Command 'df' -ErrorAction SilentlyContinue) {
                        $dfOutput = df -h --output=source,size,avail,pcent,target 2>/dev/null | 
                                   Select-Object -Skip 1
                        $dfOutput | ForEach-Object {
                            $fields = $_ -split '\s+'
                            if ($fields.Count -ge 5) {
                                [PSCustomObject]@{
                                    Drive = $fields[4]  # Mount point
                                    SizeGB = $fields[1] # Size
                                    FreeGB = $fields[2] # Available
                                    PercentFree = $fields[3] -replace '%', '' # Percent used
                                }
                            }
                        }
                    }
                }
            }
            
            # Combine system info with performance data
            $systemInfo | Add-Member -NotePropertyMembers @{
                Performance = $performanceData
                TopProcesses = if ($IncludeProcesses) {
                    Get-Process -ComputerName $computer -ErrorAction SilentlyContinue |
                    Sort-Object CPU -Descending |
                    Select-Object -First $TopProcessCount Name, CPU, WorkingSet, Id
                } else { $null }
                
                Services = if ($IncludeServices -and ($IsWindows -or $PSVersionTable.PSVersion.Major -le 5)) {
                    Get-Service -ComputerName $computer -ErrorAction SilentlyContinue |
                    Where-Object { $_.Status -eq 'Stopped' -and $_.StartType -eq 'Automatic' } |
                    Select-Object Name, Status, StartType
                } else { $null }
                
                Timestamp = Get-Date
            }
            
            Write-Output $systemInfo
        }
        catch {
            Write-Error "Failed to gather information for '$computer': $($_.Exception.Message)" -ErrorAction Continue
        }
    }
    
    return $results
}

# ============================================================
# Network Connectivity and Performance Testing
# ============================================================

function Test-EnterpriseConnectivity {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string[]]$ComputerName,
        
        [Parameter()]
        [int[]]$Port = @(80, 443, 3389, 22),
        
        [Parameter()]
        [int]$TimeoutMs = 5000,
        
        [Parameter()]
        [switch]$IncludeDnsResolution,
        
        [Parameter()]
        [switch]$IncludeTraceRoute
    )
    
    $results = foreach ($target in $ComputerName) {
        $testResult = [PSCustomObject]@{
            Target = $target
            IpAddress = $null
            DnsResolutionTime = $null
            PortTests = @()
            TraceRoute = $null
            Timestamp = Get-Date
        }
        
        try {
            # DNS Resolution Test
            if ($IncludeDnsResolution) {
                $dnsStart = Get-Date
                $resolvedIp = [System.Net.Dns]::GetHostAddresses($target) | 
                             Where-Object { $_.AddressFamily -eq 'InterNetwork' } |
                             Select-Object -First 1
                $dnsEnd = Get-Date
                
                $testResult.IpAddress = $resolvedIp.IPAddressToString
                $testResult.DnsResolutionTime = ($dnsEnd - $dnsStart).TotalMilliseconds
            }
            
            # Port Connectivity Tests
            foreach ($portNumber in $Port) {
                $portStart = Get-Date
                
                try {
                    $tcpClient = New-Object System.Net.Sockets.TcpClient
                    $asyncResult = $tcpClient.BeginConnect($target, $portNumber, $null, $null)
                    $waitHandle = $asyncResult.AsyncWaitHandle
                    
                    $connected = $waitHandle.WaitOne($TimeoutMs, $false)
                    
                    if ($connected) {
                        $tcpClient.EndConnect($asyncResult)
                        $responseTime = (Get-Date) - $portStart
                        $portTest = [PSCustomObject]@{
                            Port = $portNumber
                            Status = 'Open'
                            ResponseTimeMs = [Math]::Round($responseTime.TotalMilliseconds, 2)
                        }
                    }
                    else {
                        $portTest = [PSCustomObject]@{
                            Port = $portNumber
                            Status = 'Timeout'
                            ResponseTimeMs = $TimeoutMs
                        }
                    }
                    
                    $tcpClient.Close()
                }
                catch {
                    $portTest = [PSCustomObject]@{
                        Port = $portNumber
                        Status = 'Closed'
                        ResponseTimeMs = $null
                    }
                }
                
                $testResult.PortTests += $portTest
            }
            
            # Trace Route (Windows/Cross-Platform)
            if ($IncludeTraceRoute) {
                $testResult.TraceRoute = if ($IsWindows -or $PSVersionTable.PSVersion.Major -le 5) {
                    Test-NetConnection -ComputerName $target -TraceRoute -ErrorAction SilentlyContinue |
                    Select-Object -ExpandProperty TraceRoute
                }
                else {
                    # Linux traceroute equivalent
                    if (Get-Command 'traceroute' -ErrorAction SilentlyContinue) {
                        (traceroute $target 2>/dev/null | Select-Object -Skip 1) -split "`n" |
                        Where-Object { $_ -match '\d+\.\d+\.\d+\.\d+' } |
                        ForEach-Object { ($_ -split '\s+')[2] }
                    }
                }
            }
        }
        catch {
            Write-Warning "Connectivity test failed for '$target': $($_.Exception.Message)"
        }
        
        Write-Output $testResult
    }
    
    return $results
}

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

## Enterprise Error Handling & Logging

**Code**:

```powershell
# ============================================================
# Advanced Error Handling with Structured Logging
# ============================================================

function Invoke-WithEnterpriseErrorHandling {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [scriptblock]$ScriptBlock,
        
        [Parameter()]
        [string]$OperationName = 'Unknown Operation',
        
        [Parameter()]
        [string]$LogPath = $null,
        
        [Parameter()]
        [switch]$ThrowOnError
    )
    
    $operationId = [Guid]::NewGuid()
    $startTime = Get-Date
    
    try {
        Write-Verbose "[$operationId] Starting operation: $OperationName"
        
        # Execute the script block with error handling
        $result = & $ScriptBlock
        
        $endTime = Get-Date
        $duration = $endTime - $startTime
        
        $logEntry = [PSCustomObject]@{
            OperationId = $operationId
            OperationName = $OperationName
            Status = 'Success'
            StartTime = $startTime
            EndTime = $endTime
            DurationMs = $duration.TotalMilliseconds
            Result = $result
            ErrorDetails = $null
        }
        
        if ($LogPath) {
            $logEntry | Export-Csv -Path $LogPath -Append -NoTypeInformation
        }
        
        Write-Verbose "[$operationId] Operation completed successfully in $($duration.TotalMilliseconds)ms"
        return $result
    }
    catch [System.UnauthorizedAccessException] {
        $errorDetails = @{
            ErrorType = 'AccessDenied'
            Message = $_.Exception.Message
            Recommendation = 'Verify user permissions and run as administrator if required'
        }
        Write-Error "[$operationId] Access denied: $($_.Exception.Message)"
    }
    catch [System.IO.FileNotFoundException] {
        $errorDetails = @{
            ErrorType = 'FileNotFound'
            Message = $_.Exception.Message
            Recommendation = 'Verify file path exists and is accessible'
        }
        Write-Error "[$operationId] File not found: $($_.Exception.Message)"
    }
    catch [System.Net.NetworkInformation.PingException] {
        $errorDetails = @{
            ErrorType = 'NetworkError'
            Message = $_.Exception.Message
            Recommendation = 'Check network connectivity and DNS resolution'
        }
        Write-Error "[$operationId] Network error: $($_.Exception.Message)"
    }
    catch {
        $errorDetails = @{
            ErrorType = 'UnhandledException'
            Message = $_.Exception.Message
            StackTrace = $_.ScriptStackTrace
            Recommendation = 'Review logs and contact support if issue persists'
        }
        Write-Error "[$operationId] Unexpected error: $($_.Exception.Message)"
    }
    finally {
        $endTime = Get-Date
        $duration = $endTime - $startTime
        
        if ($errorDetails) {
            $logEntry = [PSCustomObject]@{
                OperationId = $operationId
                OperationName = $OperationName
                Status = 'Failed'
                StartTime = $startTime
                EndTime = $endTime
                DurationMs = $duration.TotalMilliseconds
                Result = $null
                ErrorDetails = $errorDetails
            }
            
            if ($LogPath) {
                $logEntry | Export-Csv -Path $LogPath -Append -NoTypeInformation
            }
            
            if ($ThrowOnError) {
                throw "Operation '$OperationName' failed: $($errorDetails.Message)"
            }
        }
        
        Write-Verbose "[$operationId] Operation cleanup completed"
    }
}

# ============================================================
# Input Validation and Parameter Testing
# ============================================================

function Test-EnterpriseParameters {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Parameters,
        
        [Parameter(Mandatory)]
        [hashtable]$ValidationRules
    )
    
    $validationResults = @()
    
    foreach ($paramName in $ValidationRules.Keys) {
        $rule = $ValidationRules[$paramName]
        $value = $Parameters[$paramName]
        
        $result = [PSCustomObject]@{
            ParameterName = $paramName
            Value = $value
            IsValid = $true
            ValidationErrors = @()
        }
        
        # Required parameter check
        if ($rule.Required -and ($null -eq $value -or $value -eq '')) {
            $result.IsValid = $false
            $result.ValidationErrors += "Parameter '$paramName' is required"
        }
        
        # Type validation
        if ($value -and $rule.Type) {
            if ($value -isnot $rule.Type) {
                $result.IsValid = $false
                $result.ValidationErrors += "Parameter '$paramName' must be of type $($rule.Type.Name)"
            }
        }
        
        # Range validation for numeric types
        if ($value -and $rule.MinValue -and $value -lt $rule.MinValue) {
            $result.IsValid = $false
            $result.ValidationErrors += "Parameter '$paramName' must be at least $($rule.MinValue)"
        }
        
        if ($value -and $rule.MaxValue -and $value -gt $rule.MaxValue) {
            $result.IsValid = $false
            $result.ValidationErrors += "Parameter '$paramName' must be no more than $($rule.MaxValue)"
        }
        
        # Path validation
        if ($value -and $rule.ValidatePath) {
            if (-not (Test-Path $value)) {
                $result.IsValid = $false
                $result.ValidationErrors += "Path '$value' for parameter '$paramName' does not exist"
            }
        }
        
        # Custom validation script
        if ($value -and $rule.CustomValidation) {
            try {
                $customResult = & $rule.CustomValidation $value
                if (-not $customResult) {
                    $result.IsValid = $false
                    $result.ValidationErrors += "Custom validation failed for parameter '$paramName'"
                }
            }
            catch {
                $result.IsValid = $false
                $result.ValidationErrors += "Custom validation error for parameter '$paramName': $($_.Exception.Message)"
            }
        }
        
        $validationResults += $result
    }
    
    return $validationResults
}

# Example usage of validation
$validationRules = @{
    'FilePath' = @{
        Required = $true
        Type = [string]
        ValidatePath = $true
    }
    'MaxSize' = @{
        Required = $false
        Type = [int]
        MinValue = 1
        MaxValue = 1000
    }
    'EmailAddress' = @{
        Required = $true
        Type = [string]
        CustomValidation = { param($email) $email -match '^[^@\s]+@[^@\s]+\.[^@\s]+$' }
    }
}

# Validate parameters
$testParams = @{
    'FilePath' = 'C:\NonExistent\file.txt'
    'MaxSize' = 500
    'EmailAddress' = 'invalid-email'
}

$validationResults = Test-EnterpriseParameters -Parameters $testParams -ValidationRules $validationRules
$validationResults | Where-Object { -not $_.IsValid } | ForEach-Object {
    Write-Warning "Validation failed for $($_.ParameterName): $($_.ValidationErrors -join ', ')"
}
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

**Enterprise Implementation Guidelines**:

- **PowerShell 7+ Recommended**: Use PowerShell 7+ for cross-platform compatibility and modern features
- **Execution Policy Management**: Set appropriate execution policies using `Set-ExecutionPolicy` or Group Policy
- **Security Best Practices**:
  - Always validate input parameters with proper validation attributes
  - Use `SecureString` for sensitive data and Windows Credential Manager for secrets
  - Implement comprehensive logging for audit trails and compliance
  - Follow principle of least privilege for all automation scripts
- **Error Handling Standards**:
  - Use structured error handling with specific exception types
  - Implement proper logging with operation IDs for traceability
  - Provide actionable error messages with remediation steps
- **Performance Optimization**:
  - Use pipeline-aware functions for efficient object processing
  - Implement early filtering to reduce memory usage
  - Consider parallel processing with `ForEach-Object -Parallel` for CPU-intensive tasks
  - Use .NET methods directly for performance-critical operations
- **Cross-Platform Considerations**:
  - Test scripts on Windows, Linux, and macOS environments
  - Use `$IsWindows`, `$IsLinux`, `$IsMacOS` variables for platform-specific logic
  - Handle path separators correctly using `Join-Path` and `[System.IO.Path]`
  - Avoid Windows-specific cmdlets when cross-platform compatibility is required

## Related Snippets

- [System Administration](system-admin.md) - Advanced system management tasks
- [File Operations](file-operations.md) - Detailed file manipulation examples
