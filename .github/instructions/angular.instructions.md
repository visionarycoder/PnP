---
# 🔧 Copilot Instruction Metadata
version: 1.0.0
schema: 1
updated: 2025-10-03
owner: Platform/IDL
stability: stable
domain: angular
# version semantics: MAJOR / MINOR / PATCH
---

# Angular + TypeScript Instructions

## Scope
Applies to `.ts`, `.html`, `.scss`, and `.json` files in Angular projects.

## Conventions
- Use Angular CLI for generating components, services, and modules.
- Prefer `@Injectable({ providedIn: 'root' })` for services.
- Use `RxJS` operators over manual subscriptions.
- Avoid logic in templates; keep them declarative.
- Use `strict` mode in `tsconfig.json`.
- Prefer `ngOnInit` for lifecycle hooks.
- Use `async/await` with `HttpClient` and observables.
- Follow SCSS BEM naming for styles.

## UI Testing
- Use Playwright for all UI tests.
- Do not use Karma, Jest, or Protractor.
- Structure Angular components to support Playwright selectors (e.g., `data-testid`).

## 📝 Changelog
### 1.0.0 (2025-10-03)
- Added metadata header (initial versioning schema).
