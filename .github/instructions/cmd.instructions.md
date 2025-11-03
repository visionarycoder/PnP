---
description: CMD/Windows Batch scripting standards and best practices
applyTo: '**/*.{cmd,bat}'
---

# CMD / Windows Batch Instructions

## Scope
Applies to `.cmd`, `.bat` files and Windows command-line scripting.

## Language Conventions
- Use `UPPER_CASE` for environment variables and constants.
- Use `PascalCase` for custom functions and labels.
- Prefix local variables with `local_` in functions.
- Use `REM` for comments, `::` for temporary comment blocks.
- Always use `@echo off` at the start of scripts.
- Use `setlocal enabledelayedexpansion` when working with variables in loops.

## Error Handling
- Check `%ERRORLEVEL%` after critical operations.
- Use `if errorlevel 1` for error conditions.
- Provide meaningful error messages with `echo`.
- Use `exit /b 1` to return error codes from functions.

## Best Practices
- Quote paths that may contain spaces: `"%PATH%"`.
- Use `pushd`/`popd` for directory navigation in scripts.
- Validate input parameters before use.
- Use `timeout` instead of `pause` for automated scripts.
- Avoid `goto` when possible; prefer function calls.
- Use `call` for invoking other batch files or functions.

## File Operations
- Use `exist` to check file/directory existence.
- Use `for /f` for parsing file content or command output.
- Use `robocopy` for reliable file copying operations.
- Always handle file paths with spaces properly.

## Documentation
- Include script purpose and usage in header comments.
- Document required parameters and environment variables.
- Provide examples of common usage scenarios.
- Include version information and last updated date.

## Security Considerations
- Validate and sanitize user input.
- Avoid storing sensitive information in plain text.
- Use `%~dp0` for script-relative paths.
- Be cautious with file permissions and execution context.

## 📝 Changelog
### 1.0.0 (2025-11-02)
- Initial version with Windows batch scripting standards.
- Added error handling and security guidelines.
- Included file operations and documentation requirements.