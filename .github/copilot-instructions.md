# Copilot Instructions for Internal.Snippet

## Project Overview

This is a **personal knowledge base repository** containing curated code snippets, design patterns, and solutions organized by technology and domain. The dual structure serves both as a documentation library (`snippets/`) and a working C# library (`src/Patterns/`).

## Architecture & Structure

### Two-Part Design
- **`snippets/`** - Markdown documentation organized by language/technology for reference and knowledge sharing
- **`src/Patterns/`** - Executable C# library (.NET 9.0) with reusable design patterns and utilities
- **`tests/`** - xUnit test suite for the C# library using modern test patterns

### Key Directories
- `snippets/csharp/` - C# code examples and patterns (documentation only)
- `src/Patterns/` - Production C# library with `LazySingleton<T>` and `ConstantsDictionaryBuilder`
- `tests/Patterns.Tests/` - Comprehensive test coverage with xUnit

## Development Patterns

### Snippet Documentation Standard
Follow the **strict template format** from `SNIPPET_TEMPLATE.md`:
```markdown
# [Title]
**Description**: [Purpose and use case]
**Language/Technology**: [Primary tech]
**Code**: [Implementation with comments]
**Usage**: [Practical examples with expected output]
**Notes**: [Performance, security, limitations, alternatives]
```

### C# Library Conventions
- **Generic utilities**: Use `LazySingleton<T>` pattern for thread-safe singletons
- **Reflection-based builders**: Follow `ConstantsDictionaryBuilder` approach for extracting constants/properties
- **Null safety**: Enable nullable reference types, handle null cases explicitly
- **XML documentation**: Complete `<summary>` and `<param>` tags for all public APIs

### File Naming & Organization
- **Snippet files**: `kebab-case.md` (e.g., `string-truncate.md`, `retry-pattern.md`)
- **Categories**: Align with technology domains (csharp, python, javascript, sql, docker, etc.)
- **Update category README**: Add new snippets to the index with brief descriptions

## Essential Workflows

### Adding New Code Snippets
1. Copy `SNIPPET_TEMPLATE.md` to appropriate category folder
2. Follow the exact template structure - all sections are required
3. Update the category's `README.md` index with your snippet
4. Use **language-specific best practices** from `CONTRIBUTING.md`

### Working with C# Library
```powershell
# Build and test cycle
dotnet build
dotnet test
dotnet test --collect:"XPlat Code Coverage"
```

### Key Development Commands
- Testing: `dotnet test` (xUnit with coverlet.collector for coverage)
- Building: `dotnet build` (targets .NET 9.0)
- Solution: Use `Internal.Snippet.sln` for multi-project operations

## Critical Implementation Details

### LazySingleton Pattern Usage
```csharp
// Inherit from LazySingleton<T> where T is your class
public class MyService : LazySingleton<MyService>
{
    // Protected constructor prevents direct instantiation
    protected MyService() { }
}

// Thread-safe access via Instance property
var service = MyService.Instance;
```

### Constants Dictionary Builder
Supports **three extraction modes**:
- `BuildFromConstants<T>()` - const and static readonly fields only
- `BuildFromProperties<T>()` - static properties only  
- `BuildFromAll<T>()` - combines both (properties override fields for same names)

### Documentation Quality Standards
- **Include prerequisites** (framework versions, dependencies)
- **Show realistic usage examples** with expected output
- **Document edge cases** and error handling approaches
- **Cross-reference related snippets** in the "Related Snippets" section
- **Security notes** for any authentication/sensitive operations

## Testing & Quality Patterns

### xUnit Test Conventions
- Test class per implementation class (e.g., `LazySingletonTests`, `ConstantsDictionaryBuilderTests`)
- Use `[Fact]` for single-case tests, `[Theory]` with `[InlineData]` for parameterized tests
- Test both success paths and edge cases (null, empty, invalid inputs)

### Snippet Quality Checklist
Before adding snippets, verify:
- [ ] Code is **tested and working**
- [ ] **All template sections** are completed  
- [ ] **Language best practices** followed (see `CONTRIBUTING.md`)
- [ ] **Category README updated** with index entry
- [ ] **No sensitive information** (credentials, keys, personal data)

## Integration Points & Dependencies

- **Target Framework**: .NET 9.0 with nullable reference types enabled
- **Test Framework**: xUnit with Visual Studio test runner integration
- **Coverage**: Coverlet collector for code coverage analysis
- **Solution Structure**: Multi-project with shared dependencies

Focus on **practical, reusable solutions** rather than academic examples. Each snippet should solve a real development problem with production-ready code quality.