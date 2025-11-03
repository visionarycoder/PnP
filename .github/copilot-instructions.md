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
- `src/chsharp/Patterns/` - Production C# library with `LazySingleton<T>` and `ConstantsDictionaryBuilder`
- `tests/csharpPatterns.Tests/` - Comprehensive test coverage with xUnit

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

## C# Best Practices & Microsoft Idioms

### Code Style & Conventions
- **Best Practices**: Follow Microsoft's C# coding conventions
- **Consistent naming**: Use consistent naming conventions across the codebase
- **No underscore prefixes**: Never use `_field`, `_parameter`, or `_variable` naming
- **Primary constructors**: Prefer primary constructors for simple parameter assignment
- **Modern C# features**: Use pattern matching, switch expressions, and record types
- **Nullable reference types**: Always enable and handle null scenarios explicitly
- **File-scoped namespaces**: Use single-line namespace declarations
- **Expression-bodied members**: Prefer for simple property getters and single-line methods
- **PascalCase**: For classes, methods, properties, and namespaces
- **camelCase**: For local variables and method parameters
- **snake_case**: For private fields (e.g., `fieldName`)
- **kebab-case**: For file names and URLs (e.g., `my-file-name.md`)

```csharp
// ✅ Preferred - Primary constructor with modern syntax
namespace MyApp.Services;

public class UserService(ILogger<UserService> logger, IUserRepository repository)
{
    public async Task<User?> GetUserAsync(int id) =>
        await repository.GetByIdAsync(id);

    public void LogActivity(string activity) =>
        logger.LogInformation("User activity: {Activity}", activity);
}

// ❌ Avoid - Traditional constructor with underscore prefixes
namespace MyApp.Services
{
    public class UserService
    {
        private readonly ILogger<UserService> _logger;
        private readonly IUserRepository _repository;

        public UserService(ILogger<UserService> logger, IUserRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }
    }
}
```

### Development Flow Best Practices

#### 1. Feature Development Workflow
```powershell
# 1. Create feature branch from main
git checkout -b feature/user-authentication

# 2. Implement with TDD approach
dotnet test --watch  # Keep running during development

# 3. Build and validate
dotnet build --configuration Release
dotnet test --collect:"XPlat Code Coverage"

# 4. Code quality checks
dotnet format --verify-no-changes
dotnet build --verbosity normal --warnaserror

# 5. Integration testing
dotnet test --filter Category=Integration
```

#### 2. Continuous Development Practices
- **Test-Driven Development (TDD)**: Write tests first, implement to pass
- **Red-Green-Refactor**: Fail → Pass → Improve cycle
- **Small commits**: Atomic changes with descriptive messages
- **Feature toggles**: Use configuration for incomplete features
- **Backward compatibility**: Maintain API contracts during evolution

#### 3. Code Review Standards
- **Self-review first**: Use `git diff --staged` before committing
- **Performance considerations**: Identify potential bottlenecks
- **Security review**: Check for vulnerabilities and data exposure
- **Documentation updates**: Ensure XML docs and README accuracy

### Project Organization & Naming

#### Solution Structure
```
Internal.Snippet/
├── src/
│   ├── Core/                          # Domain models and interfaces
│   ├── Infrastructure/                # Data access, external services
│   ├── Application/                   # Business logic and use cases
│   ├── Web/                          # Controllers, middleware, configuration
│   └── Shared/                       # Common utilities and extensions
├── tests/
│   ├── UnitTests/                    # Fast, isolated tests
│   ├── IntegrationTests/             # Database and API tests
│   └── PerformanceTests/             # Load and stress tests
├── docs/                             # Architecture and API documentation
└── tools/                            # Build scripts and utilities
```

#### Naming Conventions
- **Assemblies**: `Company.Product.Component` (e.g., `VisionaryCoder.Snippet.Core`)
- **Namespaces**: Match folder structure, use PascalCase
- **Classes**: Descriptive nouns (e.g., `UserService`, `OrderProcessor`)
- **Interfaces**: Prefix with 'I' (e.g., `IUserRepository`, `IEmailSender`)
- **Methods**: Verbs describing action (e.g., `GetUserAsync`, `ProcessOrder`)
- **Properties**: Nouns describing state (e.g., `UserName`, `IsActive`)

#### File Organization

##### One Artifact Per File Rule
- **One class/record/struct/enum per file**: Each type gets its own file
- **File name matches type name**: `UserService.cs` contains `UserService` class
- **Exception**: Nested types and embedded artifacts (like private helper classes) can stay in parent file
- **Markdown examples**: Multiple classes in documentation examples are acceptable for brevity

##### Generic Type File Naming
When both generic and non-generic versions exist:
- **Non-generic**: `Repository.cs` 
- **Generic**: `RepositoryOfType.cs` (avoids file system conflicts)

```csharp
// ✅ File: UserService.cs - Single responsibility
namespace VisionaryCoder.Snippet.Application.Services;

public class UserService(IUserRepository repository, ILogger<UserService> logger)
{
    public async Task<User?> GetByEmailAsync(string email) =>
        await repository.FindByEmailAsync(email);
}

// ✅ File: Repository.cs - Non-generic version
namespace VisionaryCoder.Snippet.Infrastructure;

public class Repository
{
    public void Save(object entity) { /* implementation */ }
}

// ✅ File: RepositoryOfType.cs - Generic version  
namespace VisionaryCoder.Snippet.Infrastructure;

public class Repository<T> where T : class
{
    public void Save(T entity) { /* implementation */ }
}

// ❌ Avoid - Multiple top-level types in one file (except for documentation)
namespace VisionaryCoder.Snippet.Services;

public class UserService { }
public class OrderService { }  // Should be in OrderService.cs
public record UserDto();       // Should be in UserDto.cs
```

##### Embedded Artifacts (Acceptable in same file)
```csharp
// ✅ File: OrderProcessor.cs - Private helpers can stay
namespace VisionaryCoder.Snippet.Services;

public class OrderProcessor
{
    private readonly ValidationHelper validator = new();
    
    // Private helper class - acceptable in same file
    private class ValidationHelper
    {
        public bool IsValid(Order order) => order.Total > 0;
    }
}
```

### Volatility-Based Decomposition

#### Component Separation by Change Frequency
```csharp
// High volatility - Business rules (frequent changes)
namespace VisionaryCoder.Snippet.Domain.BusinessRules;

public static class DiscountCalculator
{
    public static decimal Calculate(decimal amount, CustomerType type) => type switch
    {
        CustomerType.Premium => amount * 0.15m,
        CustomerType.Standard => amount * 0.10m,
        CustomerType.Basic => amount * 0.05m,
        _ => 0m
    };
}

// Medium volatility - Application services (moderate changes)
namespace VisionaryCoder.Snippet.Application.Services;

public class OrderService(IOrderRepository repository, INotificationService notifications)
{
    public async Task ProcessOrderAsync(Order order)
    {
        await repository.SaveAsync(order);
        await notifications.SendConfirmationAsync(order);
    }
}

// Low volatility - Infrastructure (rare changes)
namespace VisionaryCoder.Snippet.Infrastructure.Data;

public class EntityFrameworkRepository<T> : IRepository<T> where T : class
{
    // Stable data access patterns
}
```

#### Dependency Management Strategy
- **Stable dependencies**: Framework libraries, mature NuGet packages
- **Abstract volatile code**: Use interfaces for frequently changing components
- **Plugin architecture**: Separate volatile business logic from stable infrastructure

### CI/CD Pipeline Best Practices

#### Pipeline Configuration (.github/workflows/ci.yml)
```yaml
name: CI/CD Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --no-restore --configuration Release --warnaserror
      
    - name: Test
      run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"
      
    - name: Security scan
      run: dotnet list package --vulnerable --include-transitive
      
    - name: Package analysis
      run: dotnet pack --no-build --configuration Release
```

#### Quality Gates
- **Zero build warnings**: Treat warnings as errors in CI
- **90%+ test coverage**: Measured and enforced in pipeline
- **Security scanning**: Automated vulnerability detection
- **Performance benchmarks**: Prevent performance regressions

### Central Package Management

#### Directory.Packages.props
```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- Framework packages -->
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Logging" Version="9.0.0" />
    
    <!-- Testing packages -->
    <PackageVersion Include="xunit" Version="2.9.2" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageVersion Include="coverlet.collector" Version="6.0.2" />
    
    <!-- Analysis packages -->
    <PackageVersion Include="SonarAnalyzer.CSharp" Version="9.32.0" />
    <PackageVersion Include="Microsoft.CodeAnalysis.Analyzers" Version="3.3.4" />
  </ItemGroup>
</Project>
```

#### Directory.Build.props
```xml
<Project>
  <!-- Global properties for all projects -->
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <AnalysisMode>AllEnabledByDefault</AnalysisMode>
  </PropertyGroup>
  
  <!-- Package metadata -->
  <PropertyGroup>
    <Company>VisionaryCoder</Company>
    <Product>Internal.Snippet</Product>
    <Copyright>Copyright © VisionaryCoder 2025</Copyright>
    <RepositoryUrl>https://github.com/visionarycoder/Internal.Snippet</RepositoryUrl>
  </PropertyGroup>
  
  <!-- Code analysis -->
  <ItemGroup>
    <PackageReference Include="SonarAnalyzer.CSharp" PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

#### Directory.Build.targets
```xml
<Project>
  <!-- Custom build targets -->
  <Target Name="ValidateCodeQuality" BeforeTargets="Build">
    <Message Text="Running code quality validation..." Importance="high" />
  </Target>
  
  <!-- Automatic package vulnerability scanning -->
  <Target Name="CheckVulnerabilities" AfterTargets="Restore">
    <Exec Command="dotnet list package --vulnerable --include-transitive" 
          ContinueOnError="false" 
          Condition="'$(Configuration)' == 'Release'" />
  </Target>
</Project>
```

### Package Management Best Practices

#### Central Management Benefits
- **Version consistency**: Single source of truth for package versions
- **Security updates**: Centralized vulnerability management
- **Dependency conflicts**: Automatic resolution of version conflicts
- **Audit trail**: Clear tracking of package changes

#### Package Selection Criteria
- **Microsoft packages**: Prefer official Microsoft libraries
- **Mature ecosystems**: Choose packages with active maintenance
- **Minimal dependencies**: Avoid packages with excessive transitive dependencies
- **Performance impact**: Evaluate startup time and memory usage

#### Version Management Strategy
- **Semantic versioning**: Understand breaking vs. non-breaking changes
- **LTS frameworks**: Prefer Long Term Support versions for stability
- **Regular updates**: Schedule monthly dependency update reviews
- **Security patches**: Apply security updates immediately

These practices ensure maintainable, scalable, and professional C# codebases that align with Microsoft's recommended patterns and industry best practices.