---
# 🔧 Copilot Instruction Metadata
version: 1.0.0
schema: 1
updated: 2025-10-03
owner: Platform/IDL
stability: stable
domain: csharp
# version semantics: MAJOR / MINOR / PATCH
---

# C# / .NET Instructions

## Scope
Applies to `.cs`, `.razor`, `.csproj`, and `.sln` files.

## Language Conventions
- Target .NET 8.0+ SDK (supports .NET 8.0, 9.0, and 10.0).
- Use primary constructors and collection expressions.
- Prefer `ref readonly` for public APIs.
- Avoid `dynamic`; use strong typing.
- Use PascalCase for types and camelCase for locals.
- Prefer expression-bodied members for simple logic.
- Use `var` only when type is obvious.
- Never use `_` prefix for private fields.
- Avoid `Thread.Sleep`; use `Task.Delay` or proper async patterns.
- Use `using` alias directives for verbose types.

## Build & Format
- Build with `dotnet build`.
- Format with `dotnet format`.

## Testing
- Use MSTest for all unit and integration tests.
- Prefer `[DataTestMethod]` with `[DataRow(...)]` for data-driven tests.
- Use `TestInitialize` and `TestCleanup` for setup/teardown.
- Follow Arrange-Act-Assert structure.

## 📝 Changelog
### 1.0.0 (2025-10-03)
- Added metadata header (initial versioning schema).