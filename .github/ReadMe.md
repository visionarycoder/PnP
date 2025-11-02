# 🧠 Copilot-Guided Development

This repository uses GitHub Copilot with custom instructions to ensure consistent, secure, and idiomatic code across multiple technologies. Copilot is configured to follow project-specific standards for C#, Angular, database projects, and Playwright testing.

## 📁 Instruction Files

Language-specific guidance is stored in `.github/instructions/`:

| File                                      | Purpose                                      |
|-------------------------------------------|----------------------------------------------|
| `csharp.instructions.md`                  | C#/.NET conventions using MSTest             |
| `database.instructions.md`                | SQL and schema design best practices         |
| `angular.instructions.md`                 | Angular + TypeScript UI standards            |
| `playwright.instructions.md`              | Playwright testing for all UI layers         |
| `testing.instructions.md`                 | Data-driven unit and integration test setup  |

Global behavior is defined in:

- `.github/copilot-instructions.md`

## ✅ Testing Standards

- **C#**: xUNit or MSTest are used for all unit and integration tests.
- **UI (Angular & others)**: Playwright is used exclusively for UI testing.
- **Data-driven testing** is encouraged across all domains.

## 🔐 Security Practices

- Secrets and credentials must never be committed.
- Use environment variables prefixed with `APP_`.
- Avoid hardcoded connection strings or API keys.

## 🧪 Copilot Behavior

Copilot is instructed to:

- Follow Microsoft best practices and idioms for C# and .Net
- Write idiomatic code for the target language/framework.
- Prioritize readability and maintainability.
- Avoid deprecated or insecure patterns.
- Respect naming conventions and file scopes.
- Suggest minimal, scoped edits unless requested otherwise.

## 🚀 Getting Started

To contribute effectively:

1. Review the relevant instruction files in `.github/instructions/`.
2. Follow the testing and formatting standards.
3. Use conventional commit messages (`feat:`, `fix:`, `test:`, `docs:`, `style:`, `refactor:`, `perf:`, `build:`, `ci:`, `chore:`, `revert:`).
4. Run all tests before submitting a pull request.

## 🤝 Collaboration

These instructions help ensure that Copilot suggestions align with our team’s standards. If you notice inconsistencies or want to propose changes, open a PR to update the relevant `.instructions.md` file.

