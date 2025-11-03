# Basic CMD Commands

**Description**: Essential Windows Command Prompt commands for daily file and directory operations.
**Language/Technology**: Windows CMD, Batch
**Prerequisites**: Windows operating system with Command Prompt access

## Directory Navigation

**Code**:

```cmd
@echo off
setlocal enabledelayedexpansion

REM Safe directory navigation with error handling
call :SafeChangeDirectory "C:\Users\%USERNAME%\Documents"
if errorlevel 1 (
    echo ERROR: Failed to navigate to directory
    exit /b 1
)

REM Navigate up directories safely
pushd .
cd ..
if errorlevel 1 (
    echo ERROR: Cannot navigate up from root
    popd
    exit /b 1
)
popd

REM Change drive and directory with validation
if exist "D:\Projects" (
    cd /d "D:\Projects"
    echo Successfully changed to D:\Projects
) else (
    echo WARNING: D:\Projects does not exist
)

REM Display current directory with formatting
echo Current directory: %CD%
echo Script location: %~dp0

REM Advanced directory listing with error handling
call :ListDirectory "%CD%" "detailed"
if errorlevel 1 (
    echo ERROR: Failed to list directory contents
    exit /b 1
)

REM Generate comprehensive directory report
call :GenerateDirectoryReport "%CD%" "directory_report.txt"
echo Directory report saved to directory_report.txt

exit /b 0

:SafeChangeDirectory
    set "TARGET_DIR=%~1"
    if not exist "%TARGET_DIR%" (
        echo ERROR: Directory '%TARGET_DIR%' does not exist
        exit /b 1
    )
    
    pushd "%TARGET_DIR%"
    if errorlevel 1 (
        echo ERROR: Cannot access directory '%TARGET_DIR%'
        exit /b 1
    )
    
    echo Changed to directory: %CD%
    exit /b 0

:ListDirectory
    set "DIR_PATH=%~1"
    set "FORMAT_TYPE=%~2"
    
    if not exist "%DIR_PATH%" (
        echo ERROR: Directory '%DIR_PATH%' not found
        exit /b 1
    )
    
    echo Listing contents of: %DIR_PATH%
    echo.
    
    if /i "%FORMAT_TYPE%"=="detailed" (
        dir "%DIR_PATH%" /a /o:d /t:c
    ) else if /i "%FORMAT_TYPE%"=="wide" (
        dir "%DIR_PATH%" /w /a
    ) else if /i "%FORMAT_TYPE%"=="tree" (
        tree "%DIR_PATH%" /f /a
    ) else (
        dir "%DIR_PATH%"
    )
    
    if errorlevel 1 (
        echo ERROR: Failed to list directory
        exit /b 1
    )
    
    exit /b 0

:GenerateDirectoryReport
    set "SOURCE_DIR=%~1"
    set "REPORT_FILE=%~2"
    
    echo Generating directory report for: %SOURCE_DIR%
    
    (
        echo ===== DIRECTORY REPORT =====
        echo Generated: %DATE% %TIME%
        echo Source: %SOURCE_DIR%
        echo.
        echo === DIRECTORY STRUCTURE ===
        tree "%SOURCE_DIR%" /f /a
        echo.
        echo === DETAILED FILE LISTING ===
        dir "%SOURCE_DIR%" /s /a /-c
        echo.
        echo === SUMMARY ===
        for /f %%i in ('dir "%SOURCE_DIR%" /s /b /-c ^| find /c /v ""') do echo Total files: %%i
    ) > "%REPORT_FILE%"
    
    if exist "%REPORT_FILE%" (
        echo Report generated successfully
    ) else (
        echo ERROR: Failed to generate report
        exit /b 1
    )
    
    exit /b 0
```

**Usage**:

```cmd
C:\>cd C:\Users\John\Documents
C:\Users\John\Documents>dir /o:d
 Volume in drive C has no label.
 Directory of C:\Users\John\Documents

10/15/2023  02:30 PM    <DIR>          .
10/15/2023  02:30 PM    <DIR>          ..
10/01/2023  10:15 AM    <DIR>          Projects
10/05/2023  03:22 PM           1,024   readme.txt
               1 File(s)          1,024 bytes
               3 Dir(s)  15,234,567,890 bytes free
```

## File Operations

**Code**:

```cmd
@echo off
setlocal enabledelayedexpansion

REM Production-ready file operations with comprehensive error handling

REM Safe file creation with validation
call :CreateFileWithContent "hello.txt" "Hello World from automated script"
if errorlevel 1 (
    echo ERROR: Failed to create file
    exit /b 1
)

REM Append content safely
call :AppendToFile "hello.txt" "Additional line added on %DATE%"
if errorlevel 1 (
    echo ERROR: Failed to append to file
    exit /b 1
)

REM Robust file copying with backup
call :SafeFileCopy "source.txt" "destination.txt" "true"
if errorlevel 1 (
    echo ERROR: File copy operation failed
    exit /b 1
)

REM Batch file operations with progress
call :BatchCopyFiles "*.txt" "backup" "true"
if errorlevel 1 (
    echo ERROR: Batch copy operation failed
    exit /b 1
)

REM Safe file movement with validation
call :SafeMoveFile "oldfile.txt" "newfile.txt"
if errorlevel 1 (
    echo ERROR: File move operation failed
    exit /b 1
)

REM Secure file deletion with confirmation
call :SecureDeleteFiles "*.tmp" "false"
if errorlevel 1 (
    echo ERROR: File deletion failed
    exit /b 1
)

exit /b 0

:CreateFileWithContent
    set "FILENAME=%~1"
    set "CONTENT=%~2"
    
    if "%FILENAME%"=="" (
        echo ERROR: Filename required
        exit /b 1
    )
    
    REM Check if file already exists
    if exist "%FILENAME%" (
        echo WARNING: File '%FILENAME%' already exists
        set /p "OVERWRITE=Overwrite? (y/N): "
        if /i not "!OVERWRITE!"=="y" (
            echo INFO: Operation cancelled
            exit /b 0
        )
    )
    
    REM Create file with content
    echo %CONTENT% > "%FILENAME%" 2>nul
    if errorlevel 1 (
        echo ERROR: Cannot create file '%FILENAME%'
        exit /b 1
    )
    
    echo INFO: File '%FILENAME%' created successfully
    exit /b 0

:AppendToFile
    set "FILENAME=%~1"
    set "CONTENT=%~2"
    
    if not exist "%FILENAME%" (
        echo ERROR: File '%FILENAME%' does not exist
        exit /b 1
    )
    
    echo %CONTENT% >> "%FILENAME%" 2>nul
    if errorlevel 1 (
        echo ERROR: Cannot append to file '%FILENAME%'
        exit /b 1
    )
    
    echo INFO: Content appended to '%FILENAME%'
    exit /b 0

:SafeFileCopy
    set "SOURCE=%~1"
    set "DESTINATION=%~2"
    set "CREATE_BACKUP=%~3"
    
    if not exist "%SOURCE%" (
        echo ERROR: Source file '%SOURCE%' not found
        exit /b 1
    )
    
    REM Create backup if requested and destination exists
    if /i "%CREATE_BACKUP%"=="true" (
        if exist "%DESTINATION%" (
            copy "%DESTINATION%" "%DESTINATION%.backup" >nul 2>&1
            echo INFO: Backup created: %DESTINATION%.backup
        )
    )
    
    REM Perform copy with verification
    copy "%SOURCE%" "%DESTINATION%" >nul 2>&1
    if errorlevel 1 (
        echo ERROR: Copy failed from '%SOURCE%' to '%DESTINATION%'
        exit /b 1
    )
    
    REM Verify file integrity
    for %%A in ("%SOURCE%") do set "SOURCE_SIZE=%%~zA"
    for %%B in ("%DESTINATION%") do set "DEST_SIZE=%%~zB"
    
    if not "!SOURCE_SIZE!"=="!DEST_SIZE!" (
        echo ERROR: File size mismatch after copy
        del "%DESTINATION%" >nul 2>&1
        exit /b 1
    )
    
    echo INFO: File copied successfully: %SOURCE% -> %DESTINATION%
    exit /b 0

:BatchCopyFiles
    set "PATTERN=%~1"
    set "DEST_DIR=%~2"
    set "SHOW_PROGRESS=%~3"
    
    if not exist "%DEST_DIR%" (
        mkdir "%DEST_DIR%" 2>nul
        if errorlevel 1 (
            echo ERROR: Cannot create destination directory '%DEST_DIR%'
            exit /b 1
        )
        echo INFO: Created directory: %DEST_DIR%
    )
    
    set /a "FILE_COUNT=0"
    set /a "SUCCESS_COUNT=0"
    
    for %%F in (%PATTERN%) do (
        set /a "FILE_COUNT+=1"
        
        if /i "%SHOW_PROGRESS%"=="true" (
            echo Processing: %%F
        )
        
        robocopy . "%DEST_DIR%" "%%F" /njh /njs /ndl /nc /ns >nul 2>&1
        if not errorlevel 8 (
            set /a "SUCCESS_COUNT+=1"
        ) else (
            echo WARNING: Failed to copy %%F
        )
    )
    
    echo INFO: Batch copy completed: !SUCCESS_COUNT!/!FILE_COUNT! files copied
    if not !SUCCESS_COUNT!==!FILE_COUNT! exit /b 1
    exit /b 0

:SafeMoveFile
    set "SOURCE=%~1"
    set "DESTINATION=%~2"
    
    if not exist "%SOURCE%" (
        echo ERROR: Source file '%SOURCE%' not found
        exit /b 1
    )
    
    if exist "%DESTINATION%" (
        echo WARNING: Destination '%DESTINATION%' already exists
        set /p "OVERWRITE=Overwrite? (y/N): "
        if /i not "!OVERWRITE!"=="y" (
            echo INFO: Move operation cancelled
            exit /b 0
        )
    )
    
    move "%SOURCE%" "%DESTINATION%" >nul 2>&1
    if errorlevel 1 (
        echo ERROR: Move failed from '%SOURCE%' to '%DESTINATION%'
        exit /b 1
    )
    
    echo INFO: File moved successfully: %SOURCE% -> %DESTINATION%
    exit /b 0

:SecureDeleteFiles
    set "PATTERN=%~1"
    set "FORCE_DELETE=%~2"
    
    set /a "FILE_COUNT=0"
    
    REM Count files to be deleted
    for %%F in (%PATTERN%) do set /a "FILE_COUNT+=1"
    
    if !FILE_COUNT!==0 (
        echo INFO: No files match pattern '%PATTERN%'
        exit /b 0
    )
    
    echo Found !FILE_COUNT! files matching '%PATTERN%'
    
    if /i not "%FORCE_DELETE%"=="true" (
        set /p "CONFIRM=Delete these files? (y/N): "
        if /i not "!CONFIRM!"=="y" (
            echo INFO: Deletion cancelled
            exit /b 0
        )
    )
    
    set /a "DELETE_COUNT=0"
    
    for %%F in (%PATTERN%) do (
        del "%%F" >nul 2>&1
        if not errorlevel 1 (
            set /a "DELETE_COUNT+=1"
            echo INFO: Deleted: %%F
        ) else (
            echo WARNING: Could not delete: %%F
        )
    )
    
    echo INFO: Deletion completed: !DELETE_COUNT!/!FILE_COUNT! files deleted
    exit /b 0
```

**Usage**:

```cmd
C:\Temp>echo Sample content > test.txt
C:\Temp>copy test.txt backup.txt
        1 file(s) copied.
C:\Temp>dir *.txt
 Volume in drive C has no label.
 Directory of C:\Temp

10/15/2023  03:45 PM              15 test.txt
10/15/2023  03:45 PM              15 backup.txt
               2 File(s)             30 bytes
```

## Directory Management

**Code**:

```cmd
:: Create directories
mkdir NewFolder                     # Create single directory
md "Folder With Spaces"             # Create directory with spaces
mkdir folder1\subfolder1\subsubf    # Create nested directories

:: Remove directories
rmdir EmptyFolder                   # Remove empty directory
rd /s /q FolderWithContent         # Remove directory and all contents
deltree OldFolder                  # Alternative (older systems)

:: Directory attributes
attrib +h SecretFolder             # Hide directory
attrib -h SecretFolder             # Unhide directory
attrib +r ReadOnlyFolder           # Make read-only
```

**Usage**:

```cmd
C:\Projects>mkdir MyApp\src\components
C:\Projects>cd MyApp
C:\Projects\MyApp>tree /a
Folder PATH listing
|   
\---src
    \---components

C:\Projects\MyApp>rmdir /s src
src, Are you sure (Y/N)? y
```

## Environment Variables

**Code**:

```cmd
:: Display environment variables
set                                 # Show all variables
echo %PATH%                        # Show specific variable
echo %USERNAME%                    # Current user
echo %COMPUTERNAME%               # Computer name
echo %DATE%                       # Current date
echo %TIME%                       # Current time

:: Set environment variables
set MYVAR=Hello World             # Set for current session
setx MYVAR "Hello World"          # Set permanently for user
setx MYVAR "Hello World" /m       # Set permanently system-wide

:: Use variables in commands
echo Hello %USERNAME%!
cd %USERPROFILE%\Documents
```

**Usage**:

```cmd
C:\>echo Current user: %USERNAME%
Current user: John

C:\>set TEMP_DIR=C:\MyTemp
C:\>mkdir %TEMP_DIR%
C:\>echo Directory created: %TEMP_DIR%
Directory created: C:\MyTemp
```

## System Information

**Description**: Comprehensive system information gathering and health monitoring with enterprise-grade error handling

**Code**:

```cmd
@echo off
setlocal enabledelayedexpansion

REM Comprehensive system information gathering with error handling

REM Generate complete system report
call :GenerateSystemReport "system_report_%DATE:~-4,4%%DATE:~-10,2%%DATE:~-7,2%.txt"
if errorlevel 1 (
    echo ERROR: Failed to generate system report
    exit /b 1
)

REM Check system health and performance
call :SystemHealthCheck
if errorlevel 1 (
    echo WARNING: System health issues detected
)

REM Network diagnostics
call :NetworkDiagnostics
if errorlevel 1 (
    echo WARNING: Network connectivity issues detected
)

exit /b 0

:GenerateSystemReport
    set "REPORT_FILE=%~1"
    
    echo Generating comprehensive system report...
    
    (
        echo ===== SYSTEM INFORMATION REPORT =====
        echo Generated: %DATE% %TIME%
        echo Computer: %COMPUTERNAME%
        echo User: %USERNAME%
        echo.
        
        echo === SYSTEM DETAILS ===
        systeminfo | findstr /i "Host OS Total"
        echo.
        
        echo === HARDWARE INFORMATION ===
        wmic cpu get Name,NumberOfCores,MaxClockSpeed /format:table
        echo.
        wmic memorychip get Capacity,Speed /format:table
        echo.
        
        echo === DISK INFORMATION ===
        wmic logicaldisk get Caption,Size,FreeSpace /format:table
        echo.
        
        echo === NETWORK CONFIGURATION ===
        ipconfig /all | findstr /i "adapter address subnet gateway dns"
        echo.
        
        echo === RUNNING PROCESSES (TOP 10 BY MEMORY) ===
        tasklist /fo table | sort /r /+5 | more +1
        echo.
        
        echo === RUNNING SERVICES ===
        sc query state= running | findstr "SERVICE_NAME DISPLAY_NAME"
        echo.
        
        echo === ENVIRONMENT VARIABLES ===
        set | findstr /i "PATH TEMP PROCESSOR"
        
    ) > "%REPORT_FILE%" 2>&1
    
    if exist "%REPORT_FILE%" (
        echo INFO: System report saved to '%REPORT_FILE%'
        echo Report size: 
        for %%F in ("%REPORT_FILE%") do echo   %%~zF bytes
    ) else (
        echo ERROR: Failed to create system report
        exit /b 1
    )
    
    exit /b 0

:SystemHealthCheck
    echo Performing system health check...
    
    set "ISSUES_FOUND=false"
    
    REM Check available disk space on C: drive
    for /f "skip=1 tokens=3" %%A in ('wmic logicaldisk where "DeviceID='C:'" get FreeSpace /value') do (
        set "FREE_SPACE=%%A"
        if defined FREE_SPACE (
            set /a "FREE_GB=!FREE_SPACE!/1073741824"
            
            if !FREE_GB! LSS 5 (
                echo WARNING: Low disk space on C: drive - !FREE_GB!GB free
                set "ISSUES_FOUND=true"
            ) else (
                echo INFO: C: drive space OK - !FREE_GB!GB free
            )
        )
    )
    
    if "%ISSUES_FOUND%"=="true" (
        echo System health check completed with warnings
        exit /b 1
    ) else (
        echo System health check passed
        exit /b 0
    )

:NetworkDiagnostics
    echo Running network diagnostics...
    
    REM Test local connectivity
    ping -n 2 127.0.0.1 >nul 2>&1
    if errorlevel 1 (
        echo ERROR: Local loopback test failed
        exit /b 1
    ) else (
        echo INFO: Local loopback OK
    )
    
    REM Test DNS resolution
    nslookup google.com >nul 2>&1
    if errorlevel 1 (
        echo WARNING: DNS resolution issues detected
        exit /b 1
    ) else (
        echo INFO: DNS resolution OK
    )
    
    echo Network diagnostics completed successfully
    exit /b 0
```

## Directory Management

**Description**: Advanced directory operations with comprehensive safety checks and automation features

**Code**:

```cmd
@echo off
setlocal enabledelayedexpansion

REM Advanced directory management with comprehensive safety checks

REM Create directory structure safely
call :CreateDirectoryStructure "Projects\WebApp\src\components"
if errorlevel 1 (
    echo ERROR: Failed to create directory structure
    exit /b 1
)

REM Analyze directory usage
call :AnalyzeDirectorySize "C:\Users\%USERNAME%\Documents" "10"
if errorlevel 1 (
    echo WARNING: Directory analysis had issues
)

REM Clean up empty directories
call :CleanupEmptyDirectories "temp" "false"
if errorlevel 1 (
    echo WARNING: Directory cleanup had issues
)

exit /b 0

:CreateDirectoryStructure
    set "DIR_PATH=%~1"
    
    if exist "%DIR_PATH%" (
        echo INFO: Directory '%DIR_PATH%' already exists
        exit /b 0
    )
    
    echo Creating directory structure: %DIR_PATH%
    
    mkdir "%DIR_PATH%" 2>nul
    if errorlevel 1 (
        echo ERROR: Cannot create directory '%DIR_PATH%'
        echo Check permissions and path validity
        exit /b 1
    )
    
    echo INFO: Directory structure created successfully
    exit /b 0

:AnalyzeDirectorySize
    set "TARGET_DIR=%~1"
    set "MAX_DEPTH=%~2"
    
    if not exist "%TARGET_DIR%" (
        echo ERROR: Target directory '%TARGET_DIR%' does not exist
        exit /b 1
    )
    
    echo Analyzing directory size for: %TARGET_DIR%
    
    REM Get total size and file count
    set /a "TOTAL_SIZE=0"
    set /a "FILE_COUNT=0"
    
    for /f "tokens=3" %%S in ('dir "%TARGET_DIR%" /s /-c ^| findstr /E "File(s)"') do (
        set "TOTAL_SIZE=%%S"
    )
    
    for /f "tokens=1" %%C in ('dir "%TARGET_DIR%" /s /-c ^| findstr /E "File(s)"') do (
        set "FILE_COUNT=%%C"
    )
    
    REM Convert to readable format
    if !TOTAL_SIZE! GTR 1073741824 (
        set /a "SIZE_GB=!TOTAL_SIZE!/1073741824"
        echo Total Size: !SIZE_GB! GB
    ) else if !TOTAL_SIZE! GTR 1048576 (
        set /a "SIZE_MB=!TOTAL_SIZE!/1048576"
        echo Total Size: !SIZE_MB! MB
    ) else (
        echo Total Size: !TOTAL_SIZE! bytes
    )
    
    echo File Count: !FILE_COUNT!
    
    exit /b 0

:CleanupEmptyDirectories
    set "ROOT_PATH=%~1"
    set "FORCE_DELETE=%~2"
    
    if not exist "%ROOT_PATH%" (
        echo ERROR: Root path '%ROOT_PATH%' does not exist
        exit /b 1
    )
    
    echo Scanning for empty directories in: %ROOT_PATH%
    
    set /a "EMPTY_COUNT=0"
    
    REM Find and optionally delete empty directories
    for /f "delims=" %%D in ('dir "%ROOT_PATH%" /ad /b /s 2^>nul') do (
        dir "%%D" 2>nul | findstr /c:"0 File(s)" >nul
        if not errorlevel 1 (
            set /a "EMPTY_COUNT+=1"
            echo Found empty directory: %%D
            
            if /i "%FORCE_DELETE%"=="true" (
                rmdir "%%D" 2>nul
                if not errorlevel 1 (
                    echo INFO: Deleted empty directory: %%D
                ) else (
                    echo WARNING: Could not delete: %%D
                )
            )
        )
    )
    
    if !EMPTY_COUNT!==0 (
        echo INFO: No empty directories found
    ) else (
        echo Found !EMPTY_COUNT! empty directories
        if /i not "%FORCE_DELETE%"=="true" (
            echo Use 'true' as second parameter to delete them
        )
    )
    
    exit /b 0
```

**Notes**:

**Command Tips**:

- Use quotes around paths with spaces: `cd "C:\Program Files"`
- Use `/q` flag for quiet operation (no prompts)
- Use `/s` flag for recursive operations
- Use `/?` after any command for help: `dir /?`

**Shortcuts**:

- `Tab` key for auto-completion
- `↑` and `↓` arrows for command history  
- `F7` to show command history window
- `Ctrl+C` to interrupt running command

**Safety Tips**:

- Always verify paths before using destructive commands like `del` or `rmdir`
- Use `/p` flag for prompting before each operation: `del *.* /p`
- Test commands on sample files first
- Consider using `echo` before destructive commands to preview

**Related Snippets**:

- [Batch Scripts](batch-scripts.md)
