---
# 🔧 Copilot Instruction Metadata
version: 1.0.0
schema: 1
updated: 2025-10-03
owner: Platform/IDL
stability: stable
domain: database
# version semantics: MAJOR / MINOR / PATCH
---
# Database Project Instructions

## Scope
Applies to `.sql`, `.dbproj`, `.dacpac`, `.psql`, and `.mdf` files.

## Conventions
- Use `snake_case` for tables and columns.
- Prefix stored procedures with `usp_`; views with `vw_`.
- Avoid `SELECT *`; always specify columns.
- Use `TRY...CATCH` for error handling.
- Prefer `MERGE` or `UPSERT` for idempotent writes.
- Use schema-qualified names (`dbo.TableName`).
- Avoid hardcoded credentials; use parameterized connections.

## Migrations
- Document schema changes with versioned migration scripts.
- Include rollback scripts when feasible.

## 📝 Changelog
### 1.0.0 (2025-10-03)
- Added metadata header (initial versioning schema).