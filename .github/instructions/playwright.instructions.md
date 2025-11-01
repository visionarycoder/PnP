---
# 🔧 Copilot Instruction Metadata
version: 1.0.0
schema: 1
updated: 2025-10-03
owner: Platform/IDL
stability: stable
domain: playwright
# version semantics: MAJOR / MINOR / PATCH
---

# Playwright Test Instructions

## Scope
Applies to `.spec.ts`, `.test.ts`, and `.playwright.config.ts`.

## Conventions
- Use Playwright for all UI testing, including Angular apps.
- Use `test.describe` to group related tests.
- Prefer `test.step` for granular logging.
- Use `expect(locator).toHaveText(...)` over manual assertions.
- Avoid hardcoded waits; use `await page.waitForSelector(...)`.
- Use `data-testid` attributes for stable selectors.
- Structure tests for parallel execution.
- Include setup/teardown logic in `beforeEach` and `afterEach`.

## Data-Driven Testing
- Use `test.each([...])` for parameterized scenarios.
- Include edge cases and boundary values.

## 📝 Changelog
### 1.0.0 (2025-10-03)
- Added metadata header (initial versioning schema).