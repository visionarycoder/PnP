# Batch Scripts

**Description**: Windows batch file examples for automation and scripting tasks.
**Language/Technology**: Windows Batch (.bat, .cmd)
**Prerequisites**: Windows Command Prompt, text editor

## Basic Batch File Structure

**Code**:

```batch
@echo off
:: This is a comment
setlocal enabledelayedexpansion

echo Starting batch script...
echo Current directory: %cd%
echo Current time: %time%

:: Your commands here
pause
```

**Usage**:

```cmd
C:\Scripts>script.bat
Starting batch script...
Current directory: C:\Scripts
Current time: 14:30:25.67
Press any key to continue . . .
```

## File Processing Batch

**Code**:

```batch
@echo off
setlocal enabledelayedexpansion

:: Backup and process files
set SOURCE_DIR=C:\Data
set BACKUP_DIR=C:\Backup
set LOG_FILE=process_log.txt

echo === File Processing Started === > "%LOG_FILE%"
echo Start Time: %date% %time% >> "%LOG_FILE%"

:: Create backup directory if it doesn't exist
if not exist "%BACKUP_DIR%" (
    mkdir "%BACKUP_DIR%"
    echo Created backup directory: %BACKUP_DIR% >> "%LOG_FILE%"
)

:: Count and copy files
set /a FILE_COUNT=0
for %%f in ("%SOURCE_DIR%\*.txt") do (
    set /a FILE_COUNT+=1
    copy "%%f" "%BACKUP_DIR%\" >nul
    echo Copied: %%~nxf >> "%LOG_FILE%"
)

echo Total files processed: !FILE_COUNT! >> "%LOG_FILE%"
echo End Time: %date% %time% >> "%LOG_FILE%"
echo === Process Complete === >> "%LOG_FILE%"

echo Process completed. Check %LOG_FILE% for details.
pause
```

**Usage**:

```cmd
C:\Scripts>file_processor.bat
Process completed. Check process_log.txt for details.

C:\Scripts>type process_log.txt
=== File Processing Started ===
Start Time: Fri 11/01/2025 14:32:15.23
Created backup directory: C:\Backup
Copied: document1.txt
Copied: document2.txt
Total files processed: 2
End Time: Fri 11/01/2025 14:32:15.87
=== Process Complete ===
```

## System Information Script

**Code**:

```batch
@echo off
setlocal

:: Generate system information report
set REPORT_FILE=system_report_%date:~-4,4%%date:~-10,2%%date:~-7,2%.txt

echo Generating system information report...
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
```

**Usage**:

```cmd
C:\Scripts>system_info.bat
Generating system information report...
Report generated: system_report_20251101.txt
Opening report in notepad...
```

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
```

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
```

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
```

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
```

**Notes**:

**Best Practices**:

- Always use `@echo off` and `setlocal` at the beginning
- Use `enabledelayedexpansion` when working with variables in loops
- Include error checking with `%errorlevel%` or `if exist`
- Add logging for debugging and audit trails

**Common Variables**:

- `%0` - Script name
- `%1`, `%2`, etc. - Command line arguments
- `%*` - All arguments
- `%date%`, `%time%` - Current date and time
- `%random%` - Random number

**Error Levels**:

- `0` - Success
- `1` or higher - Various error conditions
- Use `if errorlevel 1` or `if %errorlevel% neq 0` for error checking

**Security Considerations**:

- Validate user input to prevent injection attacks  
- Use quotes around file paths to handle spaces
- Avoid storing sensitive information in plain text
- Consider using PowerShell for more complex operations

**Related Snippets**:

- [Basic Commands](basic-commands.md)
