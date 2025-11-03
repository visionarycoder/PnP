---
description: PowerShell scripting standards and best practices
applyTo: '**/*.{ps1,psm1,psd1}'
---

# PowerShell Instructions

## Scope
Applies to `.ps1`, `.psm1`, `.psd1` files and PowerShell development.

## Language Conventions
- Use `PascalCase` for functions, cmdlets, and parameters.
- Use `camelCase` for variables and properties.
- Use approved PowerShell verbs (Get-, Set-, New-, Remove-, etc.).
- Follow PowerShell naming conventions for consistency.
- Use meaningful and descriptive names.

## Script Structure
- Include script header with description and parameters.
- Use `[CmdletBinding()]` for advanced function features.
- Define parameters with proper types and validation.
- Include help documentation with `.SYNOPSIS`, `.DESCRIPTION`.
- Use `param()` block for parameter definitions.

## Error Handling
- Use `try`/`catch`/`finally` for structured error handling.
- Set appropriate `ErrorAction` preferences.
- Create custom error records when needed.
- Use `Write-Error` for non-terminating errors.
- Implement proper logging for troubleshooting.

## Parameter Design
- Use strongly typed parameters with validation.
- Implement parameter sets for different usage scenarios.
- Use `ValidateSet`, `ValidateRange`, `ValidateScript` attributes.
- Provide meaningful parameter descriptions.
- Use appropriate parameter positions and aliases.

## Output and Formatting
- Return objects, not formatted text.
- Use `Write-Output` for data pipeline.
- Use `Write-Host` only for user interaction.
- Use `Write-Verbose`, `Write-Debug` for diagnostic output.
- Implement proper formatting in consuming scripts.

## Pipeline Support
- Design functions to work with PowerShell pipeline.
- Use `Process` block for pipeline processing.
- Accept pipeline input with `ValueFromPipeline`.
- Support both parameter and pipeline input.
- Handle multiple objects efficiently.

## Security Best Practices
- Use execution policies appropriately.
- Validate all input parameters.
- Use secure string types for sensitive data.
- Implement proper authentication methods.
- Follow principle of least privilege.

## Module Development
- Organize related functions into modules.
- Create proper module manifests (`.psd1`).
- Export only necessary functions and variables.
- Use module-scoped variables appropriately.
- Implement module initialization logic.

## Performance Optimization
- Use efficient cmdlets and operators.
- Avoid unnecessary object creation.
- Use filtering early in pipelines.
- Implement proper caching when appropriate.
- Profile scripts for performance bottlenecks.

## Cross-Platform Considerations
- Test on PowerShell Core and Windows PowerShell.
- Use cross-platform compatible cmdlets.
- Handle path separators correctly.
- Test on different operating systems.
- Document platform-specific requirements.

## Testing Practices
- Write Pester tests for all functions.
- Test both success and failure scenarios.
- Use mock objects for external dependencies.
- Implement integration tests where appropriate.
- Maintain good test coverage.

## Documentation Standards
- Use comment-based help for functions.
- Include examples in help documentation.
- Document complex algorithms and business logic.
- Maintain consistent documentation style.
- Include links to related resources.

## Code Organization
- Group related functionality logically.
- Use consistent indentation and formatting.
- Keep functions focused and single-purpose.
- Use meaningful comments for complex sections.
- Organize files and folders systematically.