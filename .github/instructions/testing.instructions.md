---
# 🔧 Copilot Instruction Metadata
version: 1.0.0
schema: 1
updated: 2025-10-03
owner: Platform/IDL
stability: stable
domain: testing
# version semantics: MAJOR / MINOR / PATCH
---

# Data-Driven Unit & Integration Test Instructions

## Scope
Applies to `.cs`, `.ts`, `.spec.ts`, and `.test.ts` files.

## Unit Testing
- C#: Use MSTest with `[DataTestMethod]` and `[DataRow(...)]`.
- TypeScript: Use Playwright’s `test.each([...])` for parameterized UI tests.
- Separate test data from logic.
- Include edge cases, nulls, and boundaries.
- Use mocks/stubs for external dependencies.
- Follow Arrange-Act-Assert structure.

## Integration Testing
- C#: Use MSTest with real services or test doubles.
- TypeScript: Use Playwright for end-to-end flows.
- Clean up state between runs.
- Validate side effects (e.g., DB writes, API calls).

## 📝 Changelog
### 1.0.0 (2025-10-03)
- Added metadata header (initial versioning schema).