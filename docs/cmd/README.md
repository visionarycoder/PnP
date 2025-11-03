# Windows Command Prompt (CMD) and Batch Scripting

**Description**: Production-ready Windows CMD commands and batch scripts following modern best practices

**Language/Technology**: Windows Batch (.cmd/.bat), Command Prompt

## Overview

This collection provides enterprise-grade Windows command-line tools and batch scripts with comprehensive error handling, input validation, and security considerations. All examples follow Windows batch scripting standards with proper variable scoping, error checking, and documentation.

## Index

- [Basic Commands](basic-commands.md) - Essential CMD commands with advanced options and error handling
- [Batch Scripts](batch-scripts.md) - Production-ready batch automation with comprehensive examples

## Quick Reference Template

```cmd
@echo off
setlocal enabledelayedexpansion

REM ===================================================================
REM Script Name: template.cmd
REM Purpose: [Script description]
REM Author: [Your name]
REM Version: 1.0
REM Last Updated: [Date]
REM ===================================================================

REM Validate parameters
if "%~1"=="" (
    echo ERROR: Parameter required
    echo Usage: %~nx0 ^<parameter^>
    exit /b 1
)

set "INPUT_PARAM=%~1"
set "SCRIPT_DIR=%~dp0"
set "LOG_FILE=%SCRIPT_DIR%operation.log"

echo [%DATE% %TIME%] Starting operation... >> "%LOG_FILE%"

REM Main logic here
call :MainFunction "%INPUT_PARAM%"
if errorlevel 1 (
    echo ERROR: Operation failed
    exit /b 1
)

echo Operation completed successfully
exit /b 0

:MainFunction
    set "local_param=%~1"
    REM Function implementation
    exit /b 0
```

## Key Features

### Production Standards
- **Error Handling**: Comprehensive `%ERRORLEVEL%` checking and meaningful error messages
- **Input Validation**: Parameter validation with usage instructions
- **Logging**: Structured logging with timestamps for audit trails
- **Security**: Input sanitization and safe file operations

### Modern Practices
- **Delayed Expansion**: Proper use of `setlocal enabledelayedexpansion` for variable scope
- **Function Design**: Modular functions with local variable scoping
- **Path Handling**: Robust handling of paths with spaces using quotes
- **Documentation**: Complete header comments with version and purpose

### Enterprise Features
- **Robocopy Integration**: Reliable file operations with progress reporting
- **PowerShell Hybrid**: Integration with PowerShell for advanced operations
- **Service Management**: Windows service control and monitoring
- **Registry Operations**: Safe registry manipulation with backup procedures

## Categories

### File & Directory Operations
- **Advanced Navigation**: `pushd`/`popd`, relative paths with `%~dp0`
- **Robust File Operations**: `robocopy` with retry logic and logging
- **Permission Management**: `icacls` for access control and security
- **Disk Management**: Space monitoring, cleanup automation

### System Administration
- **Service Control**: Start, stop, configure Windows services
- **Process Management**: `tasklist`, `taskkill` with filtering and safety checks
- **Registry Operations**: Safe key manipulation with backup procedures
- **Event Log Management**: Reading and writing Windows Event Log

### Network & Security
- **Connectivity Testing**: Advanced `ping`, `tracert` with logging
- **Network Configuration**: `netsh` commands for interface management  
- **Security Scanning**: Port checking, service enumeration
- **Certificate Management**: SSL/TLS certificate operations

### Automation & Integration
- **Scheduled Tasks**: `schtasks` for automated job management
- **WMI Queries**: `wmic` for system information gathering
- **PowerShell Integration**: Hybrid scripts leveraging both engines
- **Error Recovery**: Automatic retry logic and rollback procedures

## Best Practices Summary

1. **Always use `@echo off`** and `setlocal enabledelayedexpansion`
2. **Validate all input parameters** before processing
3. **Quote all paths** that may contain spaces: `"%PATH%"`
4. **Check `%ERRORLEVEL%`** after critical operations
5. **Use functions** instead of `goto` for better structure
6. **Include comprehensive logging** for troubleshooting
7. **Handle edge cases** like missing files, access denied, network issues
8. **Document parameters** and provide usage examples

## Security Considerations

- **Input Sanitization**: Validate and clean all user inputs
- **Path Traversal Protection**: Use `%~dp0` for script-relative paths
- **Privilege Escalation**: Check and request appropriate permissions
- **Credential Handling**: Never store passwords in plain text scripts

## Related Documentation

- **PowerShell**: For advanced automation and .NET integration
- **Windows Admin Center**: Modern GUI-based administration
- **Windows Subsystem for Linux (WSL)**: Linux command integration
