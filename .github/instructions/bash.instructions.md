---
description: Bash shell scripting standards and best practices
applyTo: '**/*.{sh,bash}'
---

# Bash Shell Scripting Instructions

## Scope
Applies to `.sh`, `.bash` files and shell scripting.

## Script Structure
- Start with proper shebang line for bash scripts.
- Use `set -euo pipefail` for error handling.
- Include script description and usage in header comments.
- Define functions before main script logic.
- Use `main()` function for script entry point.

## Naming Conventions
- Use `snake_case` for variables and function names.
- Use `UPPER_CASE` for environment variables and constants.
- Use descriptive names that indicate purpose.
- Prefix local variables with `local` in functions.
- Use readonly for constants: `readonly SCRIPT_DIR`.

## Variable Handling
- Quote variables to prevent word splitting: `"$variable"`.
- Use `${variable}` for parameter expansion.
- Check if variables are set: `${variable:-default}`.
- Use arrays for multiple values: `array=("item1" "item2")`.
- Avoid global variables when possible.

## Error Handling
- Check exit codes with `$?` or conditional statements.
- Use meaningful error messages with context.
- Implement proper cleanup with trap handlers.
- Exit with appropriate codes (0 for success, 1-255 for errors).
- Log errors to stderr: `echo "Error message" >&2`.

## Function Design
- Keep functions small and focused.
- Use local variables within functions.
- Return status codes, not values.
- Document function parameters and behavior.
- Use `declare -f function_name` to check if function exists.

## File Operations
- Check file existence before operations: `[[ -f "$file" ]]`.
- Use proper file permissions and ownership.
- Handle file paths with spaces correctly.
- Use temporary files securely with `mktemp`.
- Clean up temporary files in exit handlers.

## Command Execution
- Use `command -v` to check if commands exist.
- Quote command arguments properly.
- Use `$()` for command substitution instead of backticks.
- Handle command failures gracefully.
- Use `exec` for replacing current process when appropriate.

## Input/Output
- Use `read -r` to read input safely.
- Validate user input before processing.
- Use here documents for multi-line strings.
- Implement proper logging levels (info, warning, error).
- Use appropriate file descriptors for different output types.

## Security Best Practices
- Validate and sanitize all input.
- Use absolute paths for critical commands.
- Avoid eval and other dangerous constructs.
- Handle signals properly with trap handlers.
- Use secure temporary file creation.

## Portability
- Use POSIX-compliant features when possible.
- Test scripts on different shell environments.
- Handle different operating system variations.
- Use portable command options.
- Document shell-specific requirements.

## Performance
- Avoid unnecessary subprocess creation.
- Use built-in commands instead of external utilities.
- Process files efficiently with appropriate tools.
- Use appropriate data structures for the task.
- Profile scripts for performance bottlenecks.

## Documentation
- Include comprehensive header comments.
- Document function parameters and return values.
- Provide usage examples and common scenarios.
- Document required dependencies and environment.
- Include troubleshooting information.