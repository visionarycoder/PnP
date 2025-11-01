# PowerShell Network Operations

**Description**: Network configuration, monitoring, and troubleshooting using PowerShell commands and scripts.

**Language/Technology**: PowerShell / Network Administration

**Code**:

```powershell
# ============================================
# Network Adapter Configuration
# ============================================

# Network adapter information
Get-NetAdapter                                   # List all network adapters
Get-NetAdapter | Where-Object {$_.Status -eq "Up"} # Active adapters only
Get-NetAdapter -Name "Ethernet" | Format-List   # Detailed info for specific adapter

# Enable/Disable network adapters
Enable-NetAdapter -Name "Ethernet"              # Enable adapter
Disable-NetAdapter -Name "Ethernet" -Confirm:$false # Disable adapter

# Rename network adapter
Rename-NetAdapter -Name "Ethernet" -NewName "LAN Connection"

# ============================================
# IP Address Configuration
# ============================================

# IP address management
Get-NetIPAddress                                 # All IP addresses
Get-NetIPAddress -InterfaceAlias "Ethernet"     # Specific interface
Get-NetIPAddress -AddressFamily IPv4            # IPv4 addresses only

# Set static IP address
New-NetIPAddress -InterfaceAlias "Ethernet" -IPAddress "192.168.1.100" -PrefixLength 24 -DefaultGateway "192.168.1.1"

# Remove IP address
Remove-NetIPAddress -IPAddress "192.168.1.100" -Confirm:$false

# Set IP address to DHCP
Remove-NetIPAddress -InterfaceAlias "Ethernet" -Confirm:$false
Set-NetIPInterface -InterfaceAlias "Ethernet" -Dhcp Enabled

# ============================================
# DNS Configuration
# ============================================

# DNS settings
Get-DnsClientServerAddress                      # Current DNS servers
Get-DnsClientServerAddress -InterfaceAlias "Ethernet" # Specific interface

# Set DNS servers
Set-DnsClientServerAddress -InterfaceAlias "Ethernet" -ServerAddresses "8.8.8.8","8.8.4.4"
Set-DnsClientServerAddress -InterfaceAlias "Ethernet" -ResetServerAddresses # Reset to DHCP

# DNS resolution and cache
Resolve-DnsName "google.com"                   # DNS lookup
Resolve-DnsName "google.com" -Type MX          # MX records
Get-DnsClientCache                             # View DNS cache
Clear-DnsClientCache                           # Clear DNS cache

# ============================================
# Network Connectivity Testing
# ============================================

# Basic connectivity tests
Test-Connection -ComputerName "google.com" -Count 4 # Ping test
Test-NetConnection -ComputerName "google.com" -Port 80 # TCP port test
Test-NetConnection -ComputerName "192.168.1.1" -InformationLevel Detailed # Detailed test

# Advanced connectivity testing
$result = Test-NetConnection -ComputerName "server01" -Port 3389
if ($result.TcpTestSucceeded) {
    Write-Host "RDP connection successful" -ForegroundColor Green
} else {
    Write-Host "RDP connection failed" -ForegroundColor Red
}

# Trace route
Test-NetConnection -ComputerName "google.com" -TraceRoute

# ============================================
# Network Statistics and Monitoring
# ============================================

# Network statistics
Get-NetAdapterStatistics                       # Adapter statistics
Get-NetTCPConnection                           # Active TCP connections
Get-NetTCPConnection -State Established        # Established connections only
Get-NetUDPEndpoint                            # UDP endpoints

# Network utilization monitoring
Get-Counter "\Network Interface(*)\Bytes Total/sec" -SampleInterval 1 -MaxSamples 5

# Monitor specific connections
Get-NetTCPConnection | Where-Object {$_.RemoteAddress -eq "192.168.1.50"} | 
Format-Table LocalAddress, LocalPort, RemoteAddress, RemotePort, State

# ============================================
# Firewall Management
# ============================================

# Firewall rules
Get-NetFirewallRule                            # All firewall rules
Get-NetFirewallRule -Enabled True             # Enabled rules only
Get-NetFirewallRule -Direction Inbound        # Inbound rules

# Create firewall rules
New-NetFirewallRule -DisplayName "Allow HTTP" -Direction Inbound -Protocol TCP -LocalPort 80 -Action Allow
New-NetFirewallRule -DisplayName "Block Telnet" -Direction Outbound -Protocol TCP -RemotePort 23 -Action Block

# Modify firewall rules
Set-NetFirewallRule -DisplayName "Allow HTTP" -Enabled False # Disable rule
Remove-NetFirewallRule -DisplayName "Allow HTTP"          # Remove rule

# Firewall profiles
Get-NetFirewallProfile                         # Firewall profile status
Set-NetFirewallProfile -Profile Domain -Enabled True      # Enable domain profile

# ============================================
# Network Shares and Resources
# ============================================

# SMB shares
Get-SmbShare                                   # Local SMB shares
New-SmbShare -Name "SharedFolder" -Path "C:\Shared" -FullAccess "Everyone"
Remove-SmbShare -Name "SharedFolder" -Force   # Remove share

# Map network drives
New-PSDrive -Name "Z" -PSProvider FileSystem -Root "\\server\share" -Credential (Get-Credential)
Remove-PSDrive -Name "Z" -Force               # Disconnect drive

# SMB client connections
Get-SmbConnection                              # Active SMB connections
Get-SmbMapping                                 # Mapped drives

# ============================================
# Wireless Network Management
# ============================================

# WiFi profiles and connections (Windows 10/11)
netsh wlan show profiles                       # List WiFi profiles
netsh wlan show profile name="WiFiName" key=clear # Show WiFi password

# Connect to WiFi network
$profileXml = @"
<?xml version="1.0"?>
<WLANProfile xmlns="http://www.microsoft.com/networking/WLAN/profile/v1">
    <name>MyNetwork</name>
    <SSIDConfig><SSID><name>MyNetwork</name></SSID></SSIDConfig>
    <connectionType>ESS</connectionType>
    <connectionMode>auto</connectionMode>
    <MSM>
        <security>
            <authEncryption>
                <authentication>WPA2PSK</authentication>
                <encryption>AES</encryption>
            </authEncryption>
            <sharedKey><keyType>passPhrase</keyType><protected>false</protected><keyMaterial>MyPassword</keyMaterial></sharedKey>
        </security>
    </MSM>
</WLANProfile>
"@

$profileXml | Out-File -FilePath "wifi-profile.xml" -Encoding ASCII
netsh wlan add profile filename="wifi-profile.xml"
netsh wlan connect name="MyNetwork"

# ============================================
# Network Troubleshooting
# ============================================

# Network diagnostics
Get-NetAdapterBinding                          # Protocol bindings
Get-NetRoute                                   # Routing table
Get-NetNeighbor                               # ARP table

# Reset network stack
netsh winsock reset                           # Reset Winsock
netsh int ip reset                            # Reset TCP/IP stack
ipconfig /release                             # Release IP
ipconfig /renew                               # Renew IP
ipconfig /flushdns                            # Flush DNS cache

# Network adapter troubleshooting
Restart-NetAdapter -Name "Ethernet"           # Restart adapter
Reset-NetAdapterAdvancedProperty -Name "Ethernet" -DisplayName "*" # Reset advanced properties

# ============================================
# SNMP and WMI Network Information
# ============================================

# Network information via WMI
Get-WmiObject -Class Win32_NetworkAdapterConfiguration | 
Where-Object {$_.IPEnabled -eq $true} |
Select-Object Description, IPAddress, SubnetMask, DefaultIPGateway, DNSServerSearchOrder

# Network adapter details
Get-WmiObject -Class Win32_NetworkAdapter | 
Where-Object {$_.NetConnectionStatus -eq 2} |
Select-Object Name, MACAddress, Speed

# TCP connection information
Get-WmiObject -Class Win32_PerfRawData_Tcpip_TCPv4 |
Select-Object ConnectionsEstablished, ConnectionsActive, ConnectionsPassive
```

**Usage**:

```powershell
# Network health check script
function Test-NetworkHealth {
    Write-Host "=== Network Health Check ===" -ForegroundColor Green
    
    # Test network adapters
    $adapters = Get-NetAdapter | Where-Object {$_.Status -eq "Up"}
    Write-Host "Active Network Adapters: $($adapters.Count)"
    
    foreach ($adapter in $adapters) {
        $ip = Get-NetIPAddress -InterfaceAlias $adapter.Name -AddressFamily IPv4 -ErrorAction SilentlyContinue
        if ($ip) {
            Write-Host "  $($adapter.Name): $($ip.IPAddress)" -ForegroundColor Green
        }
    }
    
    # Test internet connectivity
    $sites = @("google.com", "microsoft.com", "github.com")
    foreach ($site in $sites) {
        $result = Test-Connection -ComputerName $site -Count 1 -Quiet
        $status = if ($result) { "OK" } else { "FAILED" }
        $color = if ($result) { "Green" } else { "Red" }
        Write-Host "  $site`: $status" -ForegroundColor $color
    }
    
    # Check DNS resolution
    try {
        $dnsTest = Resolve-DnsName "google.com" -ErrorAction Stop
        Write-Host "DNS Resolution: OK" -ForegroundColor Green
    } catch {
        Write-Host "DNS Resolution: FAILED" -ForegroundColor Red
    }
}

# Port scanner function
function Test-PortRange {
    param(
        [string]$ComputerName,
        [int]$StartPort = 1,
        [int]$EndPort = 1000,
        [int]$Timeout = 100
    )
    
    $openPorts = @()
    
    for ($port = $StartPort; $port -le $EndPort; $port++) {
        $tcpClient = New-Object System.Net.Sockets.TcpClient
        $connection = $tcpClient.BeginConnect($ComputerName, $port, $null, $null)
        $wait = $connection.AsyncWaitHandle.WaitOne($Timeout, $false)
        
        if ($wait) {
            try {
                $tcpClient.EndConnect($connection)
                $openPorts += $port
                Write-Host "Port $port is open" -ForegroundColor Green
            } catch {}
        }
        
        $tcpClient.Close()
    }
    
    return $openPorts
}

# Bandwidth monitoring
function Monitor-NetworkBandwidth {
    param(
        [string]$InterfaceAlias = "Ethernet",
        [int]$IntervalSeconds = 5,
        [int]$SampleCount = 12
    )
    
    $counter = "\Network Interface($InterfaceAlias)\Bytes Total/sec"
    
    for ($i = 1; $i -le $SampleCount; $i++) {
        $sample = Get-Counter -Counter $counter -SampleInterval $IntervalSeconds
        $bytesPerSec = $sample.CounterSamples[0].CookedValue
        $mbps = [math]::Round($bytesPerSec * 8 / 1MB, 2)
        
        Write-Host "$(Get-Date -Format 'HH:mm:ss'): $mbps Mbps" -ForegroundColor Cyan
    }
}

# Network configuration backup
function Backup-NetworkConfiguration {
    param([string]$BackupPath = "C:\NetworkBackup")
    
    if (-not (Test-Path $BackupPath)) {
        New-Item -ItemType Directory -Path $BackupPath -Force
    }
    
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    
    # Export network configuration
    Get-NetAdapter | Export-Csv "$BackupPath\NetworkAdapters_$timestamp.csv" -NoTypeInformation
    Get-NetIPAddress | Export-Csv "$BackupPath\IPAddresses_$timestamp.csv" -NoTypeInformation
    Get-NetRoute | Export-Csv "$BackupPath\Routes_$timestamp.csv" -NoTypeInformation
    Get-DnsClientServerAddress | Export-Csv "$BackupPath\DNSServers_$timestamp.csv" -NoTypeInformation
    Get-NetFirewallRule | Export-Csv "$BackupPath\FirewallRules_$timestamp.csv" -NoTypeInformation
    
    Write-Host "Network configuration backed up to: $BackupPath"
}

# WiFi network scanner
function Get-WiFiNetworks {
    $networks = netsh wlan show profiles | Select-String "All User Profile" | 
    ForEach-Object {
        $profileName = $_.ToString().Split(":")[1].Trim()
        
        try {
            $details = netsh wlan show profile name="$profileName" key=clear
            $ssid = ($details | Select-String "SSID name").ToString().Split(":")[1].Trim().Trim('"')
            $auth = ($details | Select-String "Authentication").ToString().Split(":")[1].Trim()
            $key = ($details | Select-String "Key Content").ToString().Split(":")[1].Trim()
            
            [PSCustomObject]@{
                ProfileName = $profileName
                SSID = $ssid
                Authentication = $auth
                Password = if ($key) { $key } else { "Not stored" }
            }
        } catch {
            [PSCustomObject]@{
                ProfileName = $profileName
                SSID = "Error retrieving details"
                Authentication = ""
                Password = ""
            }
        }
    }
    
    return $networks
}

# Network latency monitoring
function Monitor-NetworkLatency {
    param(
        [string[]]$Targets = @("8.8.8.8", "1.1.1.1", "208.67.222.222"),
        [int]$Count = 10
    )
    
    foreach ($target in $Targets) {
        Write-Host "`nTesting latency to $target..." -ForegroundColor Yellow
        
        $results = @()
        for ($i = 1; $i -le $Count; $i++) {
            $ping = Test-Connection -ComputerName $target -Count 1 -ErrorAction SilentlyContinue
            if ($ping) {
                $results += $ping.ResponseTime
                Write-Host "  Ping $i`: $($ping.ResponseTime)ms"
            } else {
                Write-Host "  Ping $i`: Timeout" -ForegroundColor Red
            }
        }
        
        if ($results.Count -gt 0) {
            $avg = [math]::Round(($results | Measure-Object -Average).Average, 2)
            $min = ($results | Measure-Object -Minimum).Minimum
            $max = ($results | Measure-Object -Maximum).Maximum
            
            Write-Host "Average: ${avg}ms, Min: ${min}ms, Max: ${max}ms" -ForegroundColor Green
        }
    }
}
```

**Notes**:

- **Administrative Privileges**: Many network configuration commands require elevated PowerShell
- **Network Modules**: Some cmdlets require specific modules (NetAdapter, NetSecurity, etc.)
- **Remote Management**: Use `Invoke-Command` for remote network management
- **Firewall**: Network changes may be blocked by Windows Firewall
- **Compatibility**: Some commands are Windows 10/11 specific
- **Security**: Be cautious when modifying firewall rules and network settings
- **Testing**: Always test network changes in a controlled environment first
- **Documentation**: Keep records of network configuration changes

**Related Snippets**:

- [PowerShell Basics](powershell-basics.md) - Essential PowerShell commands and fundamentals
- [System Administration](system-admin.md) - Advanced system management tasks
- [File Operations](file-operations.md) - Network file operations and remote management
- [Active Directory](active-directory.md) - Domain networking and AD integration
