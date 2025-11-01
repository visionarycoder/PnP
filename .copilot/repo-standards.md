# Copilot Instructions: Repository Standards

## Purpose

Ensure that all generated code, documentation, and automation in this repository:

- Remains **clean, consistent, and maintainable**.
- Supports **isolated, reproducible development environments**.
- Aligns with **industry best practices** and our **Solution Architect Radar**.
- Provides a **smooth onboarding experience** for collaborators.

---

## General Repo Hygiene

- Always respect `.copilotignore` and `.editorconfig` rules.
- Follow **conventional commit messages** (`feat:`, `fix:`, `docs:`, `chore:`).
- Keep PRs small, focused, and linked to an ADR or issue.
- Avoid committing secrets, credentials, or machine-specific configs.

---

## Project Structure

- **Source code** lives under `/src/`.
- **Tests** live under `/tests/` with mirrored structure.
- **Docs** live under `/docs/` (onboarding, ADRs, contributing).
- **Best practices** live under `/best-practices/` (capsules + radar).
- **Copilot instructions** live under `/.copilot/`.

---

## Development Environments

- Prefer **isolated, reproducible setups**:
  - WSL2, Docker, Dev Containers, or VMs.
  - No global dependencies—use local manifests (`global.json`, `requirements.txt`, `package.json`).
- Scripts must be **idempotent** and **cross-platform** where possible.
- Document environment setup in `/docs/onboarding.md`.

---

## Coding Standards

- Follow **.editorconfig** for formatting.
- Enforce **linting and static analysis** (e.g., Roslyn analyzers, ESLint).
- Write **unit tests** for new features; aim for meaningful coverage.
- Use **dependency injection** and avoid hard-coded values.
- Prefer **composition over inheritance**.

---

## Documentation Standards

- Every module/service must have a `README.md` with:
  - Purpose
  - Setup instructions
  - Example usage
- Architecture decisions must be captured as **ADRs** in `/docs/architecture-decision-records/`.
- Best practices must be modularized into **capsules** under `/best-practices/`.

---

## CI/CD Standards

- All code must pass:
  - Build
  - Linting
  - Unit tests
- Use **branch protection rules** (no direct commits to `main`).
- Automate deployments with **GitOps or pipelines**.
- Include **security scanning** (SAST/DAST, dependency checks).

---

## Collaboration Standards

- Use **feature branches** (`feature/xyz`), **bugfix branches** (`fix/xyz`).
- Require **code reviews** before merging.
- Encourage **pairing/mobbing** for complex changes.
- Keep discussions and decisions documented (issues, ADRs, or capsules).

---

## Copilot Guidance

When generating code or docs:

- Respect repo structure and standards above.
- Prefer **modern, maintainable solutions** over hacks.
- Provide **contextual explanations** (why, not just how).
- Suggest **tests and documentation** alongside code.
- Align examples with **current .NET/C# versions** and **Solution Architect Radar** maturity levels.

---

## Anti-Patterns to Avoid

- Committing machine-specific configs (e.g., `.vs/`, `.idea/`, `bin/`, `obj/`).
- Hardcoding secrets or environment-specific values.
- Copy-pasting without attribution or context.
- Over-engineering abstractions without clear value.
- Ignoring repo standards in generated outputs.

---

## References

- [Conventional Commits](https://www.conventionalcommits.org/)
- [EditorConfig](https://editorconfig.org/)
- [ADR GitHub Repo](https://github.com/joelparkerhenderson/architecture_decision_record)
