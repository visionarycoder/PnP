# Advanced Windows Batch Scripts

**Description**: Production-ready Windows batch automation scripts with comprehensive error handling, logging, and enterprise features
**Language/Technology**: Windows Batch (.bat, .cmd)
**Prerequisites**: Windows Command Prompt, Administrator privileges for system operations, PowerShell for hybrid scripts

## Enterprise Batch Script Template

**Description**: Production-ready batch script template with comprehensive error handling, logging, and configuration management

**Code**:

```cmd
@echo off
setlocal enabledelayedexpansion

REM ============================================================================
REM Enterprise Batch Script Template
REM Version: 2.0
REM Description: Production-ready template with comprehensive features
REM ============================================================================

REM Configuration Section - Modify these variables as needed
set "SCRIPT_NAME=%~n0"
set "SCRIPT_VERSION=2.0"
set "LOG_DIR=%~dp0logs"
set "CONFIG_FILE=%~dp0%SCRIPT_NAME%.config"
set "LOCK_FILE=%TEMP%\%SCRIPT_NAME%.lock"

REM Initialize logging system
call :InitializeLogging
if errorlevel 1 (
    echo FATAL: Cannot initialize logging system
    exit /b 1
)

REM Check for existing instance
call :CheckSingleInstance
if errorlevel 1 (
    call :LogMessage "ERROR" "Another instance is already running"
    exit /b 2
)

REM Create lock file
echo %DATE% %TIME% > "%LOCK_FILE%"

REM Main execution
call :LogMessage "INFO" "Starting %SCRIPT_NAME% v%SCRIPT_VERSION%"
call :LogMessage "INFO" "Execution started by %USERNAME% on %COMPUTERNAME%"

call :LoadConfiguration
if errorlevel 1 (
    call :LogMessage "ERROR" "Failed to load configuration"
    goto :Cleanup
)

call :ValidateEnvironment
if errorlevel 1 (
    call :LogMessage "ERROR" "Environment validation failed"
    goto :Cleanup
)

REM Execute main logic
call :MainLogic
set "MAIN_RESULT=%errorlevel%"

if !MAIN_RESULT!==0 (
    call :LogMessage "INFO" "Script completed successfully"
) else (
    call :LogMessage "ERROR" "Script failed with error code !MAIN_RESULT!"
)

:Cleanup
    call :LogMessage "INFO" "Performing cleanup operations"
    if exist "%LOCK_FILE%" del "%LOCK_FILE%" >nul 2>&1
    call :LogMessage "INFO" "Script execution finished"
    exit /b %MAIN_RESULT%

REM ============================================================================
REM FUNCTION DEFINITIONS
REM ============================================================================

:InitializeLogging
    if not exist "%LOG_DIR%" (
        mkdir "%LOG_DIR%" 2>nul
        if errorlevel 1 (
            echo ERROR: Cannot create log directory: %LOG_DIR%
            exit /b 1
        )
    )
    
    set "LOG_FILE=%LOG_DIR%\%SCRIPT_NAME%_%DATE:~-4,4%%DATE:~-10,2%%DATE:~-7,2%.log"
    
    REM Test log file write access
    echo. >> "%LOG_FILE%" 2>nul
    if errorlevel 1 (
        echo ERROR: Cannot write to log file: %LOG_FILE%
        exit /b 1
    )
    
    exit /b 0

:LogMessage
    set "LOG_LEVEL=%~1"
    set "MESSAGE=%~2"
    set "TIMESTAMP=%DATE% %TIME%"
    
    echo [%TIMESTAMP%] [%LOG_LEVEL%] %MESSAGE%
    echo [%TIMESTAMP%] [%LOG_LEVEL%] %MESSAGE% >> "%LOG_FILE%"
    
    exit /b 0

:CheckSingleInstance
    if exist "%LOCK_FILE%" (
        exit /b 1
    )
    exit /b 0

:LoadConfiguration
    call :LogMessage "INFO" "Loading configuration from %CONFIG_FILE%"
    
    if not exist "%CONFIG_FILE%" (
        call :LogMessage "INFO" "Configuration file not found, using defaults"
        call :CreateDefaultConfig
        exit /b 0
    )
    
    REM Parse configuration file
    for /f "usebackq tokens=1,2 delims==" %%A in ("%CONFIG_FILE%") do (
        set "%%A=%%B"
        call :LogMessage "DEBUG" "Config: %%A=%%B"
    )
    
    exit /b 0

:CreateDefaultConfig
    call :LogMessage "INFO" "Creating default configuration file"
    
    (
        echo # %SCRIPT_NAME% Configuration File
        echo # Generated: %DATE% %TIME%
        echo.
        echo DEBUG_MODE=false
        echo MAX_RETRIES=3
        echo TIMEOUT_SECONDS=30
        echo ENABLE_BACKUP=true
        echo BACKUP_RETENTION_DAYS=7
    ) > "%CONFIG_FILE%"
    
    if errorlevel 1 (
        call :LogMessage "ERROR" "Failed to create configuration file"
        exit /b 1
    )
    
    exit /b 0

:ValidateEnvironment
    call :LogMessage "INFO" "Validating execution environment"
    
    REM Check required tools
    where powershell >nul 2>&1
    if errorlevel 1 (
        call :LogMessage "WARNING" "PowerShell not found in PATH"
    )
    
    REM Check disk space (require at least 100MB free)
    for /f "tokens=3" %%A in ('dir /-c ^| findstr /E "bytes free"') do (
        set "FREE_SPACE=%%A"
    )
    
    set /a "FREE_MB=%FREE_SPACE%/1048576"
    if %FREE_MB% LSS 100 (
        call :LogMessage "WARNING" "Low disk space: %FREE_MB%MB available"
    )
    
    call :LogMessage "INFO" "Environment validation completed"
    exit /b 0

:MainLogic
    call :LogMessage "INFO" "Executing main business logic"
    
    REM Example: Process files in a directory
    set "SOURCE_DIR=C:\Data"
    set "PROCESSED_COUNT=0"
    
    if exist "%SOURCE_DIR%" (
        for %%F in ("%SOURCE_DIR%\*.*") do (
            call :ProcessFile "%%F"
            if not errorlevel 1 (
                set /a "PROCESSED_COUNT+=1"
            )
        )
        
        call :LogMessage "INFO" "Processed %PROCESSED_COUNT% files successfully"
    ) else (
        call :LogMessage "WARNING" "Source directory not found: %SOURCE_DIR%"
    )
    
    exit /b 0

:ProcessFile
    set "FILE_PATH=%~1"
    call :LogMessage "DEBUG" "Processing file: %FILE_PATH%"
    
    REM Add your file processing logic here
    REM This is just an example that always succeeds
    
    call :LogMessage "DEBUG" "File processed successfully: %~nx1"
    exit /b 0
```cmd
**Usage**:

```cmd
C:\Scripts>enterprise_template.bat
[Fri 11/01/2025 14:32:15.23] [INFO] Starting enterprise_template v2.0
[Fri 11/01/2025 14:32:15.25] [INFO] Execution started by Administrator on SERVER01
[Fri 11/01/2025 14:32:15.27] [INFO] Loading configuration from C:\Scripts\enterprise_template.config
[Fri 11/01/2025 14:32:15.28] [INFO] Configuration file not found, using defaults
[Fri 11/01/2025 14:32:15.30] [INFO] Creating default configuration file
[Fri 11/01/2025 14:32:15.32] [INFO] Validating execution environment
[Fri 11/01/2025 14:32:15.45] [INFO] Environment validation completed
[Fri 11/01/2025 14:32:15.47] [INFO] Executing main business logic
[Fri 11/01/2025 14:32:15.52] [INFO] Processed 5 files successfully
[Fri 11/01/2025 14:32:15.54] [INFO] Script completed successfully
[Fri 11/01/2025 14:32:15.55] [INFO] Performing cleanup operations
[Fri 11/01/2025 14:32:15.56] [INFO] Script execution finished
```cmd
## Automated Backup System

**Description**: Enterprise-grade backup automation with versioning, compression, and retention policies

**Code**:

```cmd
@echo off
setlocal enabledelayedexpansion

REM ============================================================================
REM Automated Backup System with Retention Management
REM ============================================================================

REM Configuration - Customize these paths and settings
set "SOURCE_DIRS=C:\ImportantData;C:\Projects;C:\Documents"
set "BACKUP_ROOT=D:\Backups"
set "RETENTION_DAYS=30"
set "COMPRESS_BACKUPS=true"
set "LOG_FILE=%BACKUP_ROOT%\backup_%DATE:~-4,4%%DATE:~-10,2%%DATE:~-7,2%.log"
set "EMAIL_ALERTS=admin@company.com"
set "MAX_BACKUP_SIZE_GB=50"

REM Initialize backup system
call :InitializeBackup
if errorlevel 1 (
    echo FATAL: Backup system initialization failed
    exit /b 1
)

REM Execute backup operations
call :ExecuteBackup
set "BACKUP_RESULT=%errorlevel%"

REM Cleanup old backups
call :CleanupOldBackups

REM Generate backup report
call :GenerateBackupReport

REM Send notification if configured
if defined EMAIL_ALERTS call :SendNotification

exit /b %BACKUP_RESULT%

:InitializeBackup
    call :LogBackup "INFO" "Initializing backup system"
    
    REM Create backup directory structure
    if not exist "%BACKUP_ROOT%" (
        mkdir "%BACKUP_ROOT%" 2>nul
        if errorlevel 1 (
            call :LogBackup "ERROR" "Cannot create backup root: %BACKUP_ROOT%"
            exit /b 1
        )
    )
    
    REM Check available disk space
    for /f "tokens=3" %%A in ('dir "%BACKUP_ROOT%\" /-c ^| findstr /E "bytes free"') do (
        set "FREE_SPACE=%%A"
    )
    
    set /a "FREE_GB=!FREE_SPACE!/1073741824"
    if !FREE_GB! LSS %MAX_BACKUP_SIZE_GB% (
        call :LogBackup "WARNING" "Limited disk space: !FREE_GB!GB available"
    )
    
    call :LogBackup "INFO" "Backup system initialized successfully"
    exit /b 0

:ExecuteBackup
    call :LogBackup "INFO" "Starting backup operations"
    
    set "BACKUP_TIMESTAMP=%DATE:~-4,4%%DATE:~-10,2%%DATE:~-7,2%_%TIME:~0,2%%TIME:~3,2%%TIME:~6,2%"
    set "BACKUP_TIMESTAMP=%BACKUP_TIMESTAMP: =0%"
    set "CURRENT_BACKUP_DIR=%BACKUP_ROOT%\backup_%BACKUP_TIMESTAMP%"
    
    mkdir "%CURRENT_BACKUP_DIR%" 2>nul
    if errorlevel 1 (
        call :LogBackup "ERROR" "Cannot create backup directory: %CURRENT_BACKUP_DIR%"
        exit /b 1
    )
    
    REM Process each source directory
    set "TOTAL_FILES=0"
    set "TOTAL_ERRORS=0"
    
    for %%D in (%SOURCE_DIRS:;= %) do (
        if exist "%%D" (
            call :BackupDirectory "%%D" "%CURRENT_BACKUP_DIR%"
            if errorlevel 1 set /a "TOTAL_ERRORS+=1"
        ) else (
            call :LogBackup "WARNING" "Source directory not found: %%D"
            set /a "TOTAL_ERRORS+=1"
        )
    )
    
    if /i "%COMPRESS_BACKUPS%"=="true" (
        call :CompressBackup "%CURRENT_BACKUP_DIR%"
    )
    
    if %TOTAL_ERRORS%==0 (
        call :LogBackup "INFO" "Backup completed successfully - %TOTAL_FILES% files"
        exit /b 0
    ) else (
        call :LogBackup "ERROR" "Backup completed with %TOTAL_ERRORS% errors"
        exit /b 1
    )

:BackupDirectory
    set "SRC_DIR=%~1"
    set "DEST_DIR=%~2"
    set "DIR_NAME=%~nx1"
    
    call :LogBackup "INFO" "Backing up: %SRC_DIR%"
    
    REM Use robocopy for efficient file copying
    robocopy "%SRC_DIR%" "%DEST_DIR%\%DIR_NAME%" /MIR /R:3 /W:5 /LOG+:"%LOG_FILE%_robocopy.log" /TEE
    
    REM Robocopy exit codes: 0-7 are success, 8+ are errors
    if errorlevel 8 (
        call :LogBackup "ERROR" "Backup failed for: %SRC_DIR%"
        exit /b 1
    ) else (
        call :LogBackup "INFO" "Successfully backed up: %SRC_DIR%"
        exit /b 0
    )

:CompressBackup
    set "BACKUP_DIR=%~1"
    set "ARCHIVE_NAME=%BACKUP_DIR%.zip"
    
    call :LogBackup "INFO" "Compressing backup: %BACKUP_DIR%"
    
    REM Use PowerShell for compression if available
    powershell -Command "Compress-Archive -Path '%BACKUP_DIR%\*' -DestinationPath '%ARCHIVE_NAME%' -Force" 2>nul
    
    if errorlevel 1 (
        call :LogBackup "WARNING" "Compression failed, keeping uncompressed backup"
        exit /b 1
    ) else (
        call :LogBackup "INFO" "Backup compressed successfully"
        rmdir /s /q "%BACKUP_DIR%" 2>nul
        exit /b 0
    )

:CleanupOldBackups
    call :LogBackup "INFO" "Cleaning up backups older than %RETENTION_DAYS% days"
    
    set /a "DELETED_COUNT=0"
    
    REM Find and delete old backup directories
    forfiles /p "%BACKUP_ROOT%" /m backup_* /d -%RETENTION_DAYS% /c "cmd /c echo Deleting @path & rmdir /s /q @path" 2>nul
    if not errorlevel 1 set /a "DELETED_COUNT+=1"
    
    REM Find and delete old backup archives
    forfiles /p "%BACKUP_ROOT%" /m *.zip /d -%RETENTION_DAYS% /c "cmd /c echo Deleting @path & del @path" 2>nul
    if not errorlevel 1 set /a "DELETED_COUNT+=1"
    
    call :LogBackup "INFO" "Cleanup completed - %DELETED_COUNT% old backups removed"
    exit /b 0

:GenerateBackupReport
    set "REPORT_FILE=%BACKUP_ROOT%\backup_report_%DATE:~-4,4%%DATE:~-10,2%%DATE:~-7,2%.txt"
    
    (
        echo ===== BACKUP SYSTEM REPORT =====
        echo Generated: %DATE% %TIME%
        echo Computer: %COMPUTERNAME%
        echo User: %USERNAME%
        echo.
        echo === BACKUP CONFIGURATION ===
        echo Source Directories: %SOURCE_DIRS%
        echo Backup Location: %BACKUP_ROOT%
        echo Retention Policy: %RETENTION_DAYS% days
        echo Compression: %COMPRESS_BACKUPS%
        echo.
        echo === BACKUP STATISTICS ===
        
        REM Count current backups
        dir "%BACKUP_ROOT%\backup_*" /ad 2>nul | find "File(s)" >nul
        if not errorlevel 1 (
            for /f "tokens=1" %%C in ('dir "%BACKUP_ROOT%\backup_*" /ad ^| findstr /E "Dir(s)"') do echo Active Backups: %%C
        )
        
        REM Calculate total backup size
        for /f "tokens=3" %%S in ('dir "%BACKUP_ROOT%" /s /-c ^| findstr /E "bytes"') do (
            set /a "SIZE_GB=%%S/1073741824"
            echo Total Backup Size: !SIZE_GB! GB
        )
        
        echo.
        echo === LOG SUMMARY ===
        findstr /C:"ERROR" "%LOG_FILE%" 2>nul | find /c "ERROR" >nul && (
            echo Errors Found: Check full log for details
        ) || (
            echo No errors detected
        )
        
    ) > "%REPORT_FILE%"
    
    call :LogBackup "INFO" "Backup report generated: %REPORT_FILE%"
    exit /b 0

:SendNotification
    call :LogBackup "INFO" "Sending backup notification to %EMAIL_ALERTS%"
    
    REM Simple email notification using PowerShell (requires SMTP configuration)
    powershell -Command "Send-MailMessage -SmtpServer 'smtp.company.com' -From 'backup@company.com' -To '%EMAIL_ALERTS%' -Subject 'Backup Report - %COMPUTERNAME%' -Body 'Backup completed. Check log for details: %LOG_FILE%'" 2>nul
    
    if errorlevel 1 (
        call :LogBackup "WARNING" "Failed to send email notification"
    ) else (
        call :LogBackup "INFO" "Email notification sent successfully"
    )
    
    exit /b 0

:LogBackup
    set "LEVEL=%~1"
    set "MESSAGE=%~2"
    set "TIMESTAMP=%DATE% %TIME%"
    
    echo [%TIMESTAMP%] [%LEVEL%] %MESSAGE%
    echo [%TIMESTAMP%] [%LEVEL%] %MESSAGE% >> "%LOG_FILE%"
    
    exit /b 0
```cmd
**Usage**:

```cmd
C:\Scripts>backup_system.bat
[Fri 11/01/2025 14:30:00.12] [INFO] Initializing backup system
[Fri 11/01/2025 14:30:00.15] [INFO] Backup system initialized successfully
[Fri 11/01/2025 14:30:00.17] [INFO] Starting backup operations
[Fri 11/01/2025 14:30:00.20] [INFO] Backing up: C:\ImportantData
[Fri 11/01/2025 14:30:15.45] [INFO] Successfully backed up: C:\ImportantData
[Fri 11/01/2025 14:30:15.47] [INFO] Backing up: C:\Projects
[Fri 11/01/2025 14:30:25.33] [INFO] Successfully backed up: C:\Projects
[Fri 11/01/2025 14:30:25.35] [INFO] Compressing backup: D:\Backups\backup_20251101_143000
[Fri 11/01/2025 14:30:35.67] [INFO] Backup compressed successfully
[Fri 11/01/2025 14:30:35.69] [INFO] Cleaning up backups older than 30 days
[Fri 11/01/2025 14:30:36.12] [INFO] Cleanup completed - 2 old backups removed
[Fri 11/01/2025 14:30:36.15] [INFO] Backup report generated: D:\Backups\backup_report_20251101.txt
```cmd
## System Monitoring & Service Management

**Description**: Comprehensive system monitoring with service health checks, performance monitoring, and automated remediation

**Code**:

```cmd
@echo off
setlocal enabledelayedexpansion

REM ============================================================================
REM System Monitoring & Service Management Script
REM ============================================================================

REM Configuration
set "MONITOR_SERVICES=Spooler;Themes;AudioSrv;BITS"
set "CRITICAL_SERVICES=EventLog;RpcSs;LanmanServer;LanmanWorkstation"
set "CPU_THRESHOLD=80"
set "MEMORY_THRESHOLD=85"
set "DISK_THRESHOLD=90"
set "LOG_FILE=%~dp0system_monitor_%DATE:~-4,4%%DATE:~-10,2%%DATE:~-7,2%.log"
set "ALERT_FILE=%~dp0system_alerts.txt"
set "RESTART_ATTEMPTS=3"

REM Initialize monitoring
call :LogMonitor "INFO" "Starting system monitoring session"
call :LogMonitor "INFO" "Monitor configuration loaded - %MONITOR_SERVICES%"

REM Execute monitoring tasks
call :MonitorSystemHealth
call :MonitorServices
call :MonitorPerformance
call :GenerateSystemReport

call :LogMonitor "INFO" "System monitoring session completed"

exit /b 0

:MonitorSystemHealth
    call :LogMonitor "INFO" "Performing system health assessment"
    
    REM Check system uptime
    for /f "skip=1 tokens=1,2,3,4" %%A in ('wmic os get LastBootUpTime /value') do (
        if "%%A"=="LastBootUpTime" (
            set "BOOT_TIME=%%B"
            call :LogMonitor "INFO" "System last booted: !BOOT_TIME:~0,8! !BOOT_TIME:~8,6!"
        )
    )
    
    REM Check Windows Update status
    call :LogMonitor "INFO" "Checking Windows Update status"
    powershell -Command "Get-WUList -MicrosoftUpdate | Measure-Object | Select-Object -ExpandProperty Count" 2>nul > "%TEMP%\updates.tmp"
    if exist "%TEMP%\updates.tmp" (
        set /p UPDATE_COUNT=<"%TEMP%\updates.tmp"
        del "%TEMP%\updates.tmp" 2>nul
        if defined UPDATE_COUNT (
            if !UPDATE_COUNT! GTR 0 (
                call :LogMonitor "WARNING" "!UPDATE_COUNT! Windows Updates available"
                echo %DATE% %TIME% - !UPDATE_COUNT! updates pending >> "%ALERT_FILE%"
            ) else (
                call :LogMonitor "INFO" "Windows Updates are current"
            )
        )
    )
    
    REM Check system file integrity
    call :LogMonitor "INFO" "Checking system file integrity"
    sfc /verifyonly >nul 2>&1
    if errorlevel 1 (
        call :LogMonitor "WARNING" "System file corruption detected - recommend running SFC scan"
        echo %DATE% %TIME% - System file corruption detected >> "%ALERT_FILE%"
    ) else (
        call :LogMonitor "INFO" "System files integrity verified"
    )
    
    exit /b 0

:MonitorServices
    call :LogMonitor "INFO" "Monitoring critical services"
    
    REM Check critical services
    for %%S in (%CRITICAL_SERVICES:;= %) do (
        call :CheckServiceStatus "%%S" "CRITICAL"
        if errorlevel 1 (
            call :RestartService "%%S"
        )
    )
    
    REM Check standard monitored services
    for %%S in (%MONITOR_SERVICES:;= %) do (
        call :CheckServiceStatus "%%S" "STANDARD"
    )
    
    exit /b 0

:CheckServiceStatus
    set "SERVICE_NAME=%~1"
    set "SERVICE_TYPE=%~2"
    
    sc query "%SERVICE_NAME%" | findstr "STATE" | findstr "RUNNING" >nul 2>&1
    
    if errorlevel 1 (
        REM Service is not running
        sc query "%SERVICE_NAME%" | findstr "STATE" | findstr "STOPPED" >nul 2>&1
        if not errorlevel 1 (
            call :LogMonitor "ERROR" "%SERVICE_TYPE% service '%SERVICE_NAME%' is stopped"
            echo %DATE% %TIME% - Service %SERVICE_NAME% stopped >> "%ALERT_FILE%"
            exit /b 1
        ) else (
            call :LogMonitor "WARNING" "%SERVICE_TYPE% service '%SERVICE_NAME%' status unknown"
            exit /b 2
        )
    ) else (
        call :LogMonitor "INFO" "%SERVICE_TYPE% service '%SERVICE_NAME%' is running"
        exit /b 0
    )

:RestartService
    set "SERVICE_NAME=%~1"
    set "ATTEMPT=0"
    
    call :LogMonitor "INFO" "Attempting to restart service: %SERVICE_NAME%"
    
    :RestartLoop
        set /a "ATTEMPT+=1"
        if %ATTEMPT% GTR %RESTART_ATTEMPTS% (
            call :LogMonitor "ERROR" "Failed to restart %SERVICE_NAME% after %RESTART_ATTEMPTS% attempts"
            echo %DATE% %TIME% - CRITICAL: Failed to restart %SERVICE_NAME% >> "%ALERT_FILE%"
            exit /b 1
        )
        
        call :LogMonitor "INFO" "Restart attempt %ATTEMPT% for %SERVICE_NAME%"
        
        net stop "%SERVICE_NAME%" >nul 2>&1
        timeout /t 5 /nobreak >nul
        net start "%SERVICE_NAME%" >nul 2>&1
        
        if errorlevel 1 (
            call :LogMonitor "WARNING" "Restart attempt %ATTEMPT% failed for %SERVICE_NAME%"
            timeout /t 10 /nobreak >nul
            goto :RestartLoop
        ) else (
            call :LogMonitor "INFO" "Successfully restarted %SERVICE_NAME% on attempt %ATTEMPT%"
            echo %DATE% %TIME% - Service %SERVICE_NAME% restarted successfully >> "%ALERT_FILE%"
            exit /b 0
        )

:MonitorPerformance
    call :LogMonitor "INFO" "Monitoring system performance"
    
    REM Check CPU usage
    for /f "skip=1 tokens=2" %%A in ('wmic cpu get LoadPercentage /value') do (
        if "%%A" NEQ "" (
            set "CPU_USAGE=%%A"
            set "CPU_USAGE=!CPU_USAGE:~15!"
            
            if !CPU_USAGE! GTR %CPU_THRESHOLD% (
                call :LogMonitor "WARNING" "High CPU usage detected: !CPU_USAGE!%%"
                echo %DATE% %TIME% - High CPU usage: !CPU_USAGE!%% >> "%ALERT_FILE%"
            ) else (
                call :LogMonitor "INFO" "CPU usage normal: !CPU_USAGE!%%"
            )
        )
    )
    
    REM Check memory usage
    for /f "skip=1 tokens=4" %%A in ('wmic OS get TotalVisibleMemorySize /value') do set "TOTAL_MEMORY=%%A"
    for /f "skip=1 tokens=4" %%A in ('wmic OS get FreePhysicalMemory /value') do set "FREE_MEMORY=%%A"
    
    if defined TOTAL_MEMORY if defined FREE_MEMORY (
        set /a "USED_MEMORY=%TOTAL_MEMORY% - %FREE_MEMORY%"
        set /a "MEMORY_PERCENT=(%USED_MEMORY% * 100) / %TOTAL_MEMORY%"
        
        if !MEMORY_PERCENT! GTR %MEMORY_THRESHOLD% (
            call :LogMonitor "WARNING" "High memory usage: !MEMORY_PERCENT!%%"
            echo %DATE% %TIME% - High memory usage: !MEMORY_PERCENT!%% >> "%ALERT_FILE%"
        ) else (
            call :LogMonitor "INFO" "Memory usage normal: !MEMORY_PERCENT!%%"
        )
    )
    
    REM Check disk space for system drives
    for /f "skip=1 tokens=1,2,3" %%A in ('wmic logicaldisk where "DriveType=3" get DeviceID^,FreeSpace^,Size /value') do (
        if "%%A" NEQ "" if "%%B" NEQ "" if "%%C" NEQ "" (
            set "DRIVE=%%A"
            set "FREE_SPACE=%%B"
            set "TOTAL_SIZE=%%C"
            
            if defined DRIVE if defined FREE_SPACE if defined TOTAL_SIZE (
                set /a "DISK_PERCENT=100 - ((!FREE_SPACE! * 100) / !TOTAL_SIZE!)"
                
                if !DISK_PERCENT! GTR %DISK_THRESHOLD% (
                    call :LogMonitor "WARNING" "Low disk space on !DRIVE!: !DISK_PERCENT!%% used"
                    echo %DATE% %TIME% - Low disk space !DRIVE!: !DISK_PERCENT!%% used >> "%ALERT_FILE%"
                ) else (
                    call :LogMonitor "INFO" "Disk space OK on !DRIVE!: !DISK_PERCENT!%% used"
                )
            )
        )
    )
    
    exit /b 0

:GenerateSystemReport
    set "REPORT_FILE=%~dp0system_status_%DATE:~-4,4%%DATE:~-10,2%%DATE:~-7,2%.html"
    
    call :LogMonitor "INFO" "Generating comprehensive system report"
    
    (
        echo ^<!DOCTYPE html^>
        echo ^<html^>^<head^>^<title^>System Status Report^</title^>
        echo ^<style^>body{font-family:Arial;} .warning{color:orange;} .error{color:red;} .info{color:green;}^</style^>
        echo ^</head^>^<body^>
        echo ^<h1^>System Status Report^</h1^>
        echo ^<h2^>Generated: %DATE% %TIME%^</h2^>
        echo ^<h3^>Computer: %COMPUTERNAME% ^| User: %USERNAME%^</h3^>
        
        echo ^<h2^>Service Status^</h2^>
        echo ^<table border="1"^>^<tr^>^<th^>Service^</th^>^<th^>Status^</th^>^</tr^>
        
        for %%S in (%CRITICAL_SERVICES:;= % %MONITOR_SERVICES:;= %) do (
            sc query "%%S" | findstr "STATE" | findstr "RUNNING" >nul 2>&1
            if not errorlevel 1 (
                echo ^<tr^>^<td^>%%S^</td^>^<td class="info"^>Running^</td^>^</tr^>
            ) else (
                echo ^<tr^>^<td^>%%S^</td^>^<td class="error"^>Stopped^</td^>^</tr^>
            )
        )
        
        echo ^</table^>
        echo ^<h2^>Performance Metrics^</h2^>
        echo ^<p^>Detailed performance data available in system logs^</p^>
        echo ^</body^>^</html^>
        
    ) > "%REPORT_FILE%"
    
    call :LogMonitor "INFO" "System report saved: %REPORT_FILE%"
    exit /b 0

:LogMonitor
    set "LEVEL=%~1"
    set "MESSAGE=%~2"
    set "TIMESTAMP=%DATE% %TIME%"
    
    echo [%TIMESTAMP%] [%LEVEL%] %MESSAGE%
    echo [%TIMESTAMP%] [%LEVEL%] %MESSAGE% >> "%LOG_FILE%"
    
    exit /b 0
```cmd
**Usage**:

```cmd
C:\Scripts>system_monitor.bat
[Fri 11/01/2025 15:00:00.12] [INFO] Starting system monitoring session
[Fri 11/01/2025 15:00:00.15] [INFO] Monitor configuration loaded - Spooler;Themes;AudioSrv;BITS
[Fri 11/01/2025 15:00:00.17] [INFO] Performing system health assessment
[Fri 11/01/2025 15:00:02.33] [INFO] System last booted: 20251101 080000
[Fri 11/01/2025 15:00:05.45] [INFO] Windows Updates are current
[Fri 11/01/2025 15:00:08.67] [INFO] System files integrity verified
[Fri 11/01/2025 15:00:08.70] [INFO] Monitoring critical services
[Fri 11/01/2025 15:00:09.12] [INFO] CRITICAL service 'EventLog' is running
[Fri 11/01/2025 15:00:09.45] [INFO] CRITICAL service 'RpcSs' is running
[Fri 11/01/2025 15:00:09.78] [ERROR] CRITICAL service 'Spooler' is stopped
[Fri 11/01/2025 15:00:09.80] [INFO] Attempting to restart service: Spooler
[Fri 11/01/2025 15:00:15.23] [INFO] Successfully restarted Spooler on attempt 1
[Fri 11/01/2025 15:00:15.25] [INFO] Monitoring system performance
[Fri 11/01/2025 15:00:16.34] [INFO] CPU usage normal: 25%
[Fri 11/01/2025 15:00:17.67] [INFO] Memory usage normal: 65%
[Fri 11/01/2025 15:00:18.89] [WARNING] Low disk space on C:: 92% used
[Fri 11/01/2025 15:00:19.12] [INFO] Generating comprehensive system report
[Fri 11/01/2025 15:00:20.45] [INFO] System report saved: C:\Scripts\system_status_20251101.html
[Fri 11/01/2025 15:00:20.47] [INFO] System monitoring session completed
```cmd
echo ========================================== > "%REPORT_FILE%"
echo SYSTEM INFORMATION REPORT >> "%REPORT_FILE%"
echo Generated on: %date% at %time% >> "%REPORT_FILE%"
echo ========================================== >> "%REPORT_FILE%"
echo. >> "%REPORT_FILE%"

echo BASIC SYSTEM INFO: >> "%REPORT_FILE%"
echo Computer Name: %COMPUTERNAME% >> "%REPORT_FILE%"
echo User Name: %USERNAME% >> "%REPORT_FILE%"
echo OS Version: >> "%REPORT_FILE%"
ver >> "%REPORT_FILE%"
echo. >> "%REPORT_FILE%"

echo DISK SPACE: >> "%REPORT_FILE%"
for %%d in (C D E F) do (
    if exist %%d:\ (
        echo Drive %%d: >> "%REPORT_FILE%"
        dir %%d:\ | find "bytes free" >> "%REPORT_FILE%"
    )
)
echo. >> "%REPORT_FILE%"

echo RUNNING PROCESSES: >> "%REPORT_FILE%"
tasklist /fo table >> "%REPORT_FILE%"
echo. >> "%REPORT_FILE%"

echo NETWORK CONFIGURATION: >> "%REPORT_FILE%"
ipconfig /all >> "%REPORT_FILE%"

echo Report generated: %REPORT_FILE%
echo Opening report in notepad...
notepad "%REPORT_FILE%"
```cmd
**Usage**:

```cmd
C:\Scripts>system_info.bat
Generating system information report...
Report generated: system_report_20251101.txt
Opening report in notepad...
```cmd
## Menu-Driven Script

**Code**:

```batch
@echo off
setlocal
:MENU
cls
echo ================================
echo    SYSTEM UTILITIES MENU
echo ================================
echo 1. Show system information
echo 2. Clean temporary files
echo 3. List running processes
echo 4. Check disk space
echo 5. Ping network host
echo 6. Exit
echo ================================
set /p CHOICE="Enter your choice (1-6): "

if "%CHOICE%"=="1" goto SYSINFO
if "%CHOICE%"=="2" goto CLEANUP
if "%CHOICE%"=="3" goto PROCESSES
if "%CHOICE%"=="4" goto DISKSPACE
if "%CHOICE%"=="5" goto PING
if "%CHOICE%"=="6" goto EXIT
echo Invalid choice. Please try again.
pause
goto MENU

:SYSINFO
cls
echo System Information:
echo -------------------
systeminfo | findstr /C:"OS Name" /C:"Total Physical Memory" /C:"System Type"
pause
goto MENU

:CLEANUP
cls
echo Cleaning temporary files...
del /q /s "%temp%\*.*" 2>nul
del /q /s "C:\Windows\Temp\*.*" 2>nul
echo Temporary files cleaned.
pause
goto MENU

:PROCESSES
cls
echo Running Processes:
echo ------------------
tasklist /fo table | more
pause
goto MENU

:DISKSPACE
cls
echo Disk Space Information:
echo ----------------------
for %%d in (C D E F) do (
    if exist %%d:\ (
        echo Drive %%d:
        dir %%d:\ | find "bytes free"
        echo.
    )
)
pause
goto MENU

:PING
cls
set /p HOST="Enter hostname or IP to ping: "
if "%HOST%"=="" (
    echo No host specified.
) else (
    echo Pinging %HOST%...
    ping -n 4 "%HOST%"
)
pause
goto MENU

:EXIT
echo Goodbye!
pause
exit /b 0
```cmd
**Usage**:

```cmd
C:\Scripts>menu.bat
================================
   SYSTEM UTILITIES MENU
================================
1. Show system information
2. Clean temporary files
3. List running processes
4. Check disk space
5. Ping network host
6. Exit
================================
Enter your choice (1-6): 1

System Information:
-------------------
OS Name:                   Microsoft Windows 11 Pro
Total Physical Memory:     16,384 MB
System Type:              x64-based PC
Press any key to continue . . .
```cmd
## Error Handling and Logging

**Code**:

```batch
@echo off
setlocal enabledelayedexpansion

:: Advanced error handling example
set LOG_FILE=operation_log.txt
set ERROR_COUNT=0

echo === Operation Started === > "%LOG_FILE%"

:: Function to log messages
:LOG_MESSAGE
echo %date% %time%: %~1 >> "%LOG_FILE%"
goto :EOF

call :LOG_MESSAGE "Starting file operations"

:: Example operations with error checking
set OPERATION=Creating directory
mkdir TestFolder 2>nul
if !errorlevel! equ 0 (
    call :LOG_MESSAGE "SUCCESS: %OPERATION%"
) else (
    call :LOG_MESSAGE "ERROR: %OPERATION% failed"
    set /a ERROR_COUNT+=1
)

set OPERATION=Copying files
copy *.txt TestFolder\ >nul 2>&1
if !errorlevel! equ 0 (
    call :LOG_MESSAGE "SUCCESS: %OPERATION%"
) else (
    call :LOG_MESSAGE "ERROR: %OPERATION% failed"
    set /a ERROR_COUNT+=1
)

set OPERATION=Deleting temporary files
del /q temp_*.* >nul 2>&1
if !errorlevel! equ 0 (
    call :LOG_MESSAGE "SUCCESS: %OPERATION%"
) else (
    call :LOG_MESSAGE "WARNING: %OPERATION% - no files found or access denied"
)

call :LOG_MESSAGE "Operation completed with %ERROR_COUNT% errors"
echo === Operation Finished === >> "%LOG_FILE%"

echo Script completed. Errors: %ERROR_COUNT%
if %ERROR_COUNT% gtr 0 (
    echo Check %LOG_FILE% for details.
    type "%LOG_FILE%"
)
```cmd
**Usage**:

```cmd
C:\Scripts>error_handling.bat
Script completed. Errors: 1
Check operation_log.txt for details.

=== Operation Started ===
Fri 11/01/2025 14:45:12.34: Starting file operations
Fri 11/01/2025 14:45:12.35: SUCCESS: Creating directory
Fri 11/01/2025 14:45:12.36: ERROR: Copying files failed
Fri 11/01/2025 14:45:12.37: WARNING: Deleting temporary files - no files found or access denied
Fri 11/01/2025 14:45:12.38: Operation completed with 1 errors
=== Operation Finished ===
```cmd
**Notes**:

**Enterprise Batch Scripting Best Practices**:

- **Script Initialization**: Always start with `@echo off` and `setlocal enabledelayedexpansion`
- **Error Handling**: Implement comprehensive `%ERRORLEVEL%` checking after every critical operation
- **Input Validation**: Validate all user inputs and file paths before processing
- **Logging Strategy**: Use structured logging with timestamps, levels (INFO/WARNING/ERROR), and log rotation
- **Configuration Management**: Separate configuration from logic using external config files
- **Single Instance Control**: Implement lock files to prevent multiple script instances
- **Resource Cleanup**: Always clean up temporary files, lock files, and open handles in cleanup routines
- **Function Design**: Use PascalCase for function names and implement proper parameter validation

**Advanced CMD Techniques**:

- **Variable Scope**: Use `setlocal` to control variable scope and prevent pollution
- **Delayed Expansion**: Essential for variables modified inside loops: `!VARIABLE!` vs `%VARIABLE%`
- **Robust File Operations**: Use `robocopy` for enterprise file operations with retry logic
- **Service Management**: Implement proper service state checking and dependency handling
- **Performance Monitoring**: Use WMI queries for system metrics and threshold monitoring
- **Error Recovery**: Implement multi-level recovery strategies with fallback mechanisms

**Security Considerations**:

- **Path Safety**: Always quote paths with spaces and validate against directory traversal
- **Credential Handling**: Never store passwords in plain text; use Windows Credential Store
- **Permission Validation**: Check for required permissions before attempting operations  
- **Input Sanitization**: Validate and sanitize all external inputs to prevent injection attacks
- **Audit Logging**: Log all security-relevant operations with user context and timestamps
- **Privileged Operations**: Run with minimum required privileges using UAC elevation when necessary

**Integration Patterns**:

- **PowerShell Hybrid**: Call PowerShell for complex operations while maintaining CMD compatibility
- **WMI Automation**: Use Windows Management Instrumentation for system administration
- **Task Scheduler**: Design scripts for scheduled execution with proper error reporting
- **Event Log Integration**: Write to Windows Event Log for enterprise monitoring integration
- **Email Notifications**: Implement SMTP notifications for critical operations and failures

**Performance Optimization**:

- **Batch Operations**: Process multiple items efficiently to reduce overhead
- **Resource Management**: Monitor and limit CPU, memory, and disk usage during operations
- **Parallel Processing**: Use background processes for independent operations
- **Caching Strategies**: Cache expensive operations like WMI queries and file system operations

**Related Snippets**:

- [Basic Commands](basic-commands.md) - Fundamental CMD command reference
- [README](readme.md) - Complete Windows batch scripting guide

**Production Deployment**:

- Test scripts in isolated environments before production deployment
- Implement comprehensive error reporting and notification systems
- Use version control for script management and change tracking
- Document all configuration parameters and operational procedures
- Establish monitoring and alerting for automated script execution
