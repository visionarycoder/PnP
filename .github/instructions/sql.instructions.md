---
description: SQL database standards and best practices
applyTo: '**/*.sql'
---

# SQL Instructions

## Scope
Applies to `.sql` files and SQL database development.

## Naming Conventions
- Use `snake_case` for table names, column names, and variables.
- Use descriptive names that clearly indicate purpose.
- Prefix stored procedures with `sp_` or use schema prefixes.
- Use `v_` prefix for views or organize in view schema.
- Use singular nouns for table names (user, not users).

## Query Structure
- Use proper indentation and formatting for readability.
- Align keywords and use consistent capitalization (UPPER for keywords).
- Break complex queries into readable sections.
- Use meaningful table aliases (not single letters).
- Group related conditions logically with parentheses.

## Performance Best Practices
- Avoid `SELECT *` - specify required columns explicitly.
- Use appropriate indexes for query optimization.
- Use `EXISTS` instead of `IN` for subqueries when possible.
- Implement proper pagination for large result sets.
- Use query execution plans to identify bottlenecks.

## Data Types and Constraints
- Use appropriate data types for columns.
- Implement proper constraints (NOT NULL, CHECK, UNIQUE).
- Use foreign keys to maintain referential integrity.
- Define primary keys for all tables.
- Use default values appropriately.

## Security Practices
- Use parameterized queries to prevent SQL injection.
- Implement proper user roles and permissions.
- Avoid dynamic SQL when possible.
- Validate input data before processing.
- Use stored procedures for complex operations.

## Transaction Management
- Use transactions for multi-statement operations.
- Implement proper error handling with rollback.
- Keep transactions as short as possible.
- Use appropriate isolation levels.
- Handle deadlocks and concurrency issues.

## Stored Procedures and Functions
- Keep procedures focused and single-purpose.
- Use proper parameter validation.
- Implement consistent error handling.
- Document procedure parameters and behavior.
- Return appropriate status codes.

## Schema Design
- Follow normalization principles appropriately.
- Design for data integrity and consistency.
- Use appropriate relationships between tables.
- Consider performance implications of design choices.
- Document business rules and constraints.

## Migration and Versioning
- Create reversible migration scripts.
- Test migrations on copy of production data.
- Document schema changes thoroughly.
- Use version control for database schema.
- Implement proper backup strategies.

## Cross-Database Compatibility
- Use standard SQL when possible.
- Document database-specific features used.
- Test queries on target database systems.
- Use appropriate date/time functions.
- Handle NULL values consistently.

## Optimization Techniques
- Use appropriate join types (INNER, LEFT, RIGHT).
- Implement efficient filtering with WHERE clauses.
- Use UNION ALL instead of UNION when duplicates are acceptable.
- Consider using CTEs for complex queries.
- Implement proper indexing strategies.

## Documentation Standards
- Comment complex queries and business logic.
- Document table purposes and relationships.
- Include examples of proper usage.
- Document performance considerations.
- Maintain data dictionary and schema documentation.