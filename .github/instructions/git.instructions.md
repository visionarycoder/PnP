---
description: Git version control best practices and workflow standards
applyTo: '**/.git*'
---

# Git Instructions

## Scope
Applies to Git repositories, `.gitignore`, `.gitattributes`, and Git workflow practices.

## Commit Message Conventions
- Use imperative mood in commit messages ("Add feature" not "Added feature").
- Keep first line under 50 characters as a summary.
- Add blank line before detailed description if needed.
- Reference issue numbers when applicable using hash notation.
- Use conventional commits format: `type(scope): description`.

## Commit Types
- `feat`: New features
- `fix`: Bug fixes
- `docs`: Documentation changes
- `style`: Code formatting (no logic changes)
- `refactor`: Code restructuring without behavior changes
- `test`: Adding or modifying tests
- `chore`: Maintenance tasks, build updates

## Branching Strategy
- Use `main` as the primary branch for production-ready code.
- Create feature branches from `main`: `feature/description`.
- Use `hotfix/description` for urgent production fixes.
- Use `release/version` for release preparation.
- Delete merged branches to keep repository clean.

## Branch Naming Conventions
- Use lowercase with hyphens: `feature/user-authentication`.
- Include issue number when applicable: `fix/123-login-bug`.
- Keep names descriptive but concise.
- Use prefixes: `feature/`, `fix/`, `hotfix/`, `docs/`, `refactor/`.

## File Management
- Always include a comprehensive `.gitignore` file.
- Ignore build artifacts, dependencies, and IDE files.
- Never commit sensitive information (keys, passwords, tokens).
- Use `.gitattributes` for consistent line endings.
- Keep repository size manageable (use Git LFS for large files).

## Workflow Best Practices
- Pull latest changes before starting new work.
- Commit early and often with logical chunks.
- Review changes before committing (`git diff --cached`).
- Use interactive rebase to clean up commit history.
- Write meaningful commit messages that explain "why".

## Code Review Process
- Create pull requests for all changes to main branches.
- Provide clear PR descriptions with context.
- Review code thoroughly before approving.
- Address feedback constructively.
- Use draft PRs for work-in-progress discussions.

## Tag Management
- Use semantic versioning for releases (v1.2.3).
- Tag stable releases with annotated tags.
- Include release notes with tags.
- Use consistent tag naming conventions.
- Sign important tags for verification.

## Merge Strategies
- Prefer merge commits for feature integration.
- Use fast-forward merges for simple updates.
- Squash commits for clean history when appropriate.
- Avoid merge conflicts by rebasing before merging.
- Test merged code before pushing to main.

## Repository Maintenance
- Regular cleanup of merged branches.
- Archive or remove obsolete repositories.
- Keep commit history clean and meaningful.
- Use `git gc` periodically for optimization.
- Monitor repository size and performance.

## Security Practices
- Use signed commits for authenticity.
- Regularly rotate access tokens and SSH keys.
- Review and audit repository permissions.
- Enable branch protection rules.
- Use secure authentication methods.

## Collaboration Guidelines
- Establish team conventions for workflow.
- Document branching strategy in repository.
- Use issue templates for consistent reporting.
- Maintain CHANGELOG.md for release tracking.
- Communicate breaking changes clearly.

## Troubleshooting
- Use `git reflog` to recover lost commits.
- Know how to resolve merge conflicts safely.
- Understand when to use `git reset` vs `git revert`.
- Keep backups of important work.
- Document resolution steps for complex issues.