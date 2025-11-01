# PowerShell Active Directory Operations

**Description**: Active Directory management, user administration, and domain operations using PowerShell and ADSI.

**Language/Technology**: PowerShell / Active Directory / ADSI

**Code**:

```powershell
# Import Active Directory module (requires RSAT)
Import-Module ActiveDirectory

# ============================================
# User Management
# ============================================

# Get user information
Get-ADUser -Identity "username"                              # Basic user info
Get-ADUser -Identity "username" -Properties *               # All properties
Get-ADUser -Filter "Name -like '*John*'" -Properties DisplayName, EmailAddress
Get-ADUser -SearchBase "OU=Users,DC=domain,DC=com" -Filter * # Users in specific OU

# Create new user
$userParams = @{
    Name = "John Doe"
    GivenName = "John"
    Surname = "Doe"
    SamAccountName = "jdoe"
    UserPrincipalName = "jdoe@company.com"
    EmailAddress = "john.doe@company.com"
    DisplayName = "John Doe"
    Description = "Sales Manager"
    Department = "Sales"
    Title = "Manager"
    Office = "New York"
    OfficePhone = "555-1234"
    Path = "OU=Users,DC=company,DC=com"
    AccountPassword = (ConvertTo-SecureString "P@ssw0rd123!" -AsPlainText -Force)
    Enabled = $true
    ChangePasswordAtLogon = $true
}
New-ADUser @userParams

# Modify user properties
Set-ADUser -Identity "jdoe" -Description "Senior Sales Manager"
Set-ADUser -Identity "jdoe" -Title "Senior Manager" -Department "Sales" -Office "Chicago"

# User account management
Enable-ADAccount -Identity "jdoe"                           # Enable account
Disable-ADAccount -Identity "jdoe"                          # Disable account
Unlock-ADAccount -Identity "jdoe"                           # Unlock account
Set-ADAccountPassword -Identity "jdoe" -Reset -NewPassword (ConvertTo-SecureString "NewP@ss123!" -AsPlainText -Force)

# Remove user
Remove-ADUser -Identity "jdoe" -Confirm:$false

# ============================================
# Group Management
# ============================================

# Group operations
Get-ADGroup -Identity "GroupName"                           # Get group info
Get-ADGroup -Filter "Name -like '*Sales*'" -Properties Description, ManagedBy
New-ADGroup -Name "IT Support" -GroupScope Global -GroupCategory Security -Path "OU=Groups,DC=company,DC=com"

# Group membership
Get-ADGroupMember -Identity "IT Support"                    # List group members
Add-ADGroupMember -Identity "IT Support" -Members "jdoe","msmith" # Add users to group
Remove-ADGroupMember -Identity "IT Support" -Members "jdoe" -Confirm:$false # Remove from group

# User group membership
Get-ADPrincipalGroupMembership -Identity "jdoe"            # Groups user belongs to
Add-ADPrincipalGroupMembership -Identity "jdoe" -MemberOf "IT Support"

# ============================================
# Organizational Unit (OU) Management
# ============================================

# OU operations
Get-ADOrganizationalUnit -Filter *                          # List all OUs
Get-ADOrganizationalUnit -Identity "OU=Sales,DC=company,DC=com" -Properties *

# Create OU
New-ADOrganizationalUnit -Name "Marketing" -Path "DC=company,DC=com" -Description "Marketing Department"

# Move objects between OUs
Move-ADObject -Identity "CN=John Doe,OU=Users,DC=company,DC=com" -TargetPath "OU=Marketing,DC=company,DC=com"

# ============================================
# Computer Management
# ============================================

# Computer accounts
Get-ADComputer -Identity "COMPUTER01"                       # Get computer info
Get-ADComputer -Filter "OperatingSystem -like '*Windows 10*'" -Properties OperatingSystem, LastLogonDate
Get-ADComputer -SearchBase "OU=Workstations,DC=company,DC=com" -Filter *

# Create computer account
New-ADComputer -Name "WORKSTATION01" -Path "OU=Workstations,DC=company,DC=com" -Enabled $true

# Computer account management
Enable-ADAccount -Identity "WORKSTATION01$"                # Enable computer account
Disable-ADAccount -Identity "WORKSTATION01$"               # Disable computer account
Reset-ComputerMachinePassword -Credential (Get-Credential) # Reset computer account password

# ============================================
# Domain Controller and Forest Information
# ============================================

# Domain and forest info
Get-ADDomain                                                # Current domain info
Get-ADForest                                               # Forest information
Get-ADDomainController                                     # Domain controllers
Get-ADReplicationSite                                      # Replication sites

# Domain functional levels
(Get-ADDomain).DomainMode                                  # Domain functional level
(Get-ADForest).ForestMode                                  # Forest functional level

# FSMO roles
Get-ADDomain | Select-Object InfrastructureMaster, RIDMaster, PDCEmulator
Get-ADForest | Select-Object DomainNamingMaster, SchemaMaster

# ============================================
# Group Policy Operations
# ============================================

# Group Policy (requires GroupPolicy module)
Import-Module GroupPolicy

Get-GPO -All                                               # List all GPOs
Get-GPO -Name "Default Domain Policy"                      # Specific GPO
New-GPO -Name "Security Policy" -Comment "Company security settings"

# GPO links
Get-GPInheritance -Target "OU=Sales,DC=company,DC=com"     # GPO inheritance
New-GPLink -Name "Security Policy" -Target "OU=Sales,DC=company,DC=com" -LinkEnabled Yes

# ============================================
# Active Directory Queries and Reporting
# ============================================

# User reports
$users = Get-ADUser -Filter * -Properties LastLogonDate, PasswordLastSet, PasswordNeverExpires
$users | Where-Object {$_.LastLogonDate -lt (Get-Date).AddDays(-30)} | # Inactive users
Select-Object Name, LastLogonDate | Export-Csv "InactiveUsers.csv" -NoTypeInformation

# Password expiry report
$users | Where-Object {$_.PasswordNeverExpires -eq $false} |
ForEach-Object {
    $passwordAge = (Get-Date) - $_.PasswordLastSet
    $daysUntilExpiry = 90 - $passwordAge.Days  # Assuming 90-day policy
    
    [PSCustomObject]@{
        Name = $_.Name
        SamAccountName = $_.SamAccountName
        PasswordLastSet = $_.PasswordLastSet
        DaysUntilExpiry = $daysUntilExpiry
    }
} | Where-Object {$_.DaysUntilExpiry -le 7} | # Expiring within 7 days
Export-Csv "PasswordExpiry.csv" -NoTypeInformation

# Group membership report
$groups = Get-ADGroup -Filter * -Properties Members
foreach ($group in $groups) {
    $members = Get-ADGroupMember -Identity $group.Name
    foreach ($member in $members) {
        [PSCustomObject]@{
            GroupName = $group.Name
            MemberName = $member.Name
            MemberType = $member.ObjectClass
        }
    }
} | Export-Csv "GroupMembership.csv" -NoTypeInformation

# ============================================
# LDAP Queries and Advanced Searches
# ============================================

# Advanced LDAP filter queries
$filter = "(&(objectCategory=person)(objectClass=user)(!(userAccountControl:1.2.840.113556.1.4.803:=2)))"
Get-ADUser -LDAPFilter $filter -Properties LastLogonDate

# Search for locked out accounts
Search-ADAccount -LockedOut | Select-Object Name, SamAccountName, LockedOut, LastLogonDate

# Search for disabled accounts
Search-ADAccount -AccountDisabled | Select-Object Name, SamAccountName, Enabled

# Search for accounts with passwords that don't expire
Get-ADUser -Filter {PasswordNeverExpires -eq $true} -Properties PasswordNeverExpires |
Select-Object Name, SamAccountName, PasswordNeverExpires

# ============================================
# Active Directory Replication
# ============================================

# Replication status
Get-ADReplicationFailure -Target (Get-ADDomainController).Name  # Replication failures
Get-ADReplicationConnection -Filter *                           # Replication connections

# Force replication
Sync-ADObject -Object "CN=John Doe,OU=Users,DC=company,DC=com" -Source "DC01.company.com" -Destination "DC02.company.com"

# Replication monitoring
$dcs = Get-ADDomainController -Filter *
foreach ($dc in $dcs) {
    Write-Host "Checking replication for $($dc.Name)..."
    Get-ADReplicationPartnerMetadata -Target $dc.Name -Partition (Get-ADDomain).DistinguishedName
}

# ============================================
# Active Directory Backup and Maintenance
# ============================================

# AD database maintenance (run on DC)
ntdsutil "activate instance ntds" "files" "integrity" "quit" "quit"  # Check database integrity

# Export AD objects
Get-ADUser -Filter * | Export-Csv "ADUsers_$(Get-Date -Format 'yyyyMMdd').csv" -NoTypeInformation
Get-ADGroup -Filter * | Export-Csv "ADGroups_$(Get-Date -Format 'yyyyMMdd').csv" -NoTypeInformation
Get-ADComputer -Filter * | Export-Csv "ADComputers_$(Get-Date -Format 'yyyyMMdd').csv" -NoTypeInformation

# ============================================
# ADSI (Active Directory Service Interfaces)
# ============================================

# Using ADSI when AD module is not available
$domain = [ADSI]"LDAP://DC=company,DC=com"                  # Connect to domain
$searcher = New-Object System.DirectoryServices.DirectorySearcher($domain)

# Search for users using ADSI
$searcher.Filter = "(&(objectCategory=person)(objectClass=user))"
$searcher.PropertiesToLoad.Add("samaccountname") | Out-Null
$searcher.PropertiesToLoad.Add("displayname") | Out-Null
$users = $searcher.FindAll()

foreach ($user in $users) {
    Write-Host "$($user.Properties.displayname) - $($user.Properties.samaccountname)"
}

# Create user with ADSI
$ou = [ADSI]"LDAP://OU=Users,DC=company,DC=com"
$user = $ou.Create("user", "CN=Test User")
$user.Put("sAMAccountName", "testuser")
$user.Put("displayName", "Test User")
$user.Put("userPrincipalName", "testuser@company.com")
$user.SetInfo()

# Set password with ADSI
$user.SetPassword("P@ssw0rd123!")
$user.Put("userAccountControl", 512)  # Enable account
$user.SetInfo()

# ============================================
# Exchange Online Integration (if applicable)
# ============================================

# Connect to Exchange Online (requires ExchangeOnlineManagement module)
# Connect-ExchangeOnline -UserPrincipalName admin@company.com

# Create mailbox for new user
# Enable-Mailbox -Identity "jdoe" -Alias "jdoe"

# Set mailbox properties
# Set-Mailbox -Identity "jdoe" -DisplayName "John Doe" -Office "New York"
```

**Usage**:

```powershell
# Bulk user creation from CSV
function New-UsersFromCSV {
    param([string]$CSVPath)
    
    $users = Import-Csv $CSVPath
    
    foreach ($user in $users) {
        $userParams = @{
            Name = "$($user.FirstName) $($user.LastName)"
            GivenName = $user.FirstName
            Surname = $user.LastName
            SamAccountName = $user.Username
            UserPrincipalName = "$($user.Username)@company.com"
            EmailAddress = $user.Email
            Department = $user.Department
            Title = $user.Title
            Path = $user.OU
            AccountPassword = (ConvertTo-SecureString $user.Password -AsPlainText -Force)
            Enabled = $true
            ChangePasswordAtLogon = $true
        }
        
        try {
            New-ADUser @userParams
            Write-Host "Created user: $($user.Username)" -ForegroundColor Green
        } catch {
            Write-Host "Failed to create user: $($user.Username) - $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

# AD health check function
function Test-ADHealth {
    Write-Host "=== Active Directory Health Check ===" -ForegroundColor Green
    
    # Check domain controllers
    $dcs = Get-ADDomainController -Filter *
    Write-Host "`nDomain Controllers:" -ForegroundColor Yellow
    foreach ($dc in $dcs) {
        $ping = Test-Connection -ComputerName $dc.HostName -Count 1 -Quiet
        $status = if ($ping) { "Online" } else { "Offline" }
        $color = if ($ping) { "Green" } else { "Red" }
        Write-Host "  $($dc.Name): $status" -ForegroundColor $color
    }
    
    # Check replication
    Write-Host "`nReplication Status:" -ForegroundColor Yellow
    try {
        $replFailures = Get-ADReplicationFailure -Target * -ErrorAction SilentlyContinue
        if ($replFailures) {
            Write-Host "  Replication failures detected!" -ForegroundColor Red
            $replFailures | ForEach-Object { Write-Host "    $($_.Server): $($_.FirstFailureTime)" }
        } else {
            Write-Host "  No replication failures" -ForegroundColor Green
        }
    } catch {
        Write-Host "  Could not check replication status" -ForegroundColor Yellow
    }
    
    # Check FSMO roles
    Write-Host "`nFSMO Roles:" -ForegroundColor Yellow
    $domain = Get-ADDomain
    $forest = Get-ADForest
    Write-Host "  PDC Emulator: $($domain.PDCEmulator)"
    Write-Host "  RID Master: $($domain.RIDMaster)"
    Write-Host "  Infrastructure Master: $($domain.InfrastructureMaster)"
    Write-Host "  Schema Master: $($forest.SchemaMaster)"
    Write-Host "  Domain Naming Master: $($forest.DomainNamingMaster)"
}

# User account audit function
function Get-UserAccountAudit {
    param(
        [int]$InactiveDays = 90,
        [int]$PasswordExpiryDays = 7
    )
    
    $users = Get-ADUser -Filter * -Properties LastLogonDate, PasswordLastSet, PasswordNeverExpires, Enabled
    
    $report = @{
        TotalUsers = $users.Count
        EnabledUsers = ($users | Where-Object {$_.Enabled -eq $true}).Count
        DisabledUsers = ($users | Where-Object {$_.Enabled -eq $false}).Count
        InactiveUsers = ($users | Where-Object {$_.LastLogonDate -lt (Get-Date).AddDays(-$InactiveDays)}).Count
        PasswordNeverExpires = ($users | Where-Object {$_.PasswordNeverExpires -eq $true}).Count
        PasswordExpiringSoon = 0
    }
    
    # Calculate password expiry (assuming 90-day policy)
    $expiringUsers = $users | Where-Object {
        $_.PasswordNeverExpires -eq $false -and 
        $_.PasswordLastSet -and
        ((Get-Date) - $_.PasswordLastSet).Days -gt (90 - $PasswordExpiryDays)
    }
    $report.PasswordExpiringSoon = $expiringUsers.Count
    
    Write-Host "=== User Account Audit Report ===" -ForegroundColor Green
    $report.GetEnumerator() | ForEach-Object {
        Write-Host "$($_.Key): $($_.Value)" -ForegroundColor Cyan
    }
    
    return $report
}

# Group membership cleanup
function Remove-EmptyGroups {
    param([switch]$WhatIf)
    
    $groups = Get-ADGroup -Filter * -Properties Members
    $emptyGroups = $groups | Where-Object {$_.Members.Count -eq 0}
    
    Write-Host "Found $($emptyGroups.Count) empty groups:" -ForegroundColor Yellow
    
    foreach ($group in $emptyGroups) {
        Write-Host "  $($group.Name)" -ForegroundColor Red
        
        if (-not $WhatIf) {
            $confirm = Read-Host "Delete group '$($group.Name)'? (y/N)"
            if ($confirm -eq 'y' -or $confirm -eq 'Y') {
                try {
                    Remove-ADGroup -Identity $group.Name -Confirm:$false
                    Write-Host "    Deleted successfully" -ForegroundColor Green
                } catch {
                    Write-Host "    Failed to delete: $($_.Exception.Message)" -ForegroundColor Red
                }
            }
        }
    }
}

# Disable inactive computer accounts
function Disable-InactiveComputers {
    param(
        [int]$InactiveDays = 90,
        [switch]$WhatIf
    )
    
    $cutoffDate = (Get-Date).AddDays(-$InactiveDays)
    $computers = Get-ADComputer -Filter * -Properties LastLogonDate |
                Where-Object {$_.LastLogonDate -lt $cutoffDate -and $_.Enabled -eq $true}
    
    Write-Host "Found $($computers.Count) inactive computers (not logged on since $cutoffDate):" -ForegroundColor Yellow
    
    foreach ($computer in $computers) {
        Write-Host "  $($computer.Name) - Last logon: $($computer.LastLogonDate)" -ForegroundColor Red
        
        if (-not $WhatIf) {
            try {
                Disable-ADAccount -Identity $computer.Name
                Write-Host "    Disabled successfully" -ForegroundColor Green
            } catch {
                Write-Host "    Failed to disable: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    }
}

# AD backup export function
function Export-ADConfiguration {
    param([string]$BackupPath = "C:\ADBackup")
    
    if (-not (Test-Path $BackupPath)) {
        New-Item -ItemType Directory -Path $BackupPath -Force
    }
    
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    
    # Export users
    Get-ADUser -Filter * -Properties * | 
    Export-Csv "$BackupPath\Users_$timestamp.csv" -NoTypeInformation
    
    # Export groups
    Get-ADGroup -Filter * -Properties * | 
    Export-Csv "$BackupPath\Groups_$timestamp.csv" -NoTypeInformation
    
    # Export computers
    Get-ADComputer -Filter * -Properties * | 
    Export-Csv "$BackupPath\Computers_$timestamp.csv" -NoTypeInformation
    
    # Export OUs
    Get-ADOrganizationalUnit -Filter * -Properties * | 
    Export-Csv "$BackupPath\OUs_$timestamp.csv" -NoTypeInformation
    
    # Export GPOs
    if (Get-Module -ListAvailable -Name GroupPolicy) {
        Import-Module GroupPolicy
        Get-GPO -All | Export-Csv "$BackupPath\GPOs_$timestamp.csv" -NoTypeInformation
    }
    
    Write-Host "AD configuration exported to: $BackupPath"
}
```

**Notes**:

- **Prerequisites**: Active Directory PowerShell module (part of RSAT)
- **Permissions**: Most operations require Domain Admin or delegated permissions
- **PowerShell Version**: Works with PowerShell 3.0+ and PowerShell Core
- **Error Handling**: Always use try-catch blocks for production scripts
- **Testing**: Use `-WhatIf` parameter where available for testing changes
- **Security**: Secure storage of passwords and credentials is critical
- **Logging**: Consider implementing detailed logging for audit purposes
- **Performance**: Large domains may require pagination and filtering
- **Compliance**: Follow your organization's AD change management policies

**Related Snippets**:

- [PowerShell Basics](powershell-basics.md) - Essential PowerShell commands and syntax
- [System Administration](system-admin.md) - General system management tasks
- [Network Operations](network-operations.md) - Network and domain connectivity
- [File Operations](file-operations.md) - File system and share management
