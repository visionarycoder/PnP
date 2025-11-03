---
description: GraphQL schema design and query best practices
applyTo: '**/*.{graphql,gql,graphqls}'
---

# GraphQL Instructions

## Scope
Applies to `.graphql`, `.gql`, `.graphqls` files and GraphQL development.

## Schema Design Principles
- Design schema around business domain, not database structure.
- Use clear, descriptive names for types, fields, and operations.
- Follow GraphQL naming conventions (camelCase for fields).
- Keep schema flat and avoid unnecessary nesting.
- Design for client needs, not server convenience.

## Type System Best Practices
- Use scalar types appropriately (String, Int, Float, Boolean, ID).
- Create custom scalars for domain-specific types (DateTime, Email, URL).
- Use enums for fixed sets of values.
- Design interfaces for common functionality across types.
- Use unions for fields that can return different types.

## Field Design
- Make fields nullable by default, non-null only when guaranteed.
- Use descriptive field names that indicate their purpose.
- Avoid abbreviations in field names.
- Group related fields into nested objects.
- Design fields for reusability across different contexts.

## Query Design
- Structure queries to minimize over-fetching and under-fetching.
- Use fragments for reusable field selections.
- Implement proper pagination with cursor-based approach.
- Design efficient filtering and sorting mechanisms.
- Consider query complexity and depth limiting.

## Mutation Best Practices
- Design mutations to be atomic and consistent.
- Use input types for complex mutation arguments.
- Return meaningful payloads with success/error information.
- Include client mutation ID for request tracking.
- Design mutations to be idempotent when possible.

## Error Handling
- Use proper error codes and messages.
- Distinguish between user errors and system errors.
- Provide actionable error messages.
- Use field-level errors when appropriate.
- Implement proper error logging and monitoring.

## Performance Optimization
- Implement DataLoader for batching and caching.
- Use query complexity analysis to prevent abuse.
- Implement proper depth limiting.
- Cache resolved fields when appropriate.
- Monitor and optimize N+1 query problems.

## Security Considerations
- Validate and sanitize all input data.
- Implement proper authentication and authorization.
- Use query whitelisting in production.
- Implement rate limiting and query timeouts.
- Sanitize error messages to prevent information leaks.

## Schema Evolution
- Design schema for backwards compatibility.
- Deprecate fields instead of removing them immediately.
- Use schema versioning strategies when needed.
- Document breaking changes clearly.
- Plan migration strategies for clients.

## Documentation
- Provide comprehensive descriptions for all types and fields.
- Include examples in field descriptions.
- Document complex business logic and rules.
- Maintain schema changelog for client developers.
- Use GraphQL comments for internal documentation.

## Subscription Design
- Design subscriptions for real-time data needs.
- Use proper filtering to reduce unnecessary updates.
- Implement subscription cleanup and lifecycle management.
- Consider subscription complexity and resource usage.
- Provide fallback mechanisms for connection issues.

## Testing Strategies
- Write unit tests for resolvers and business logic.
- Test schema validation and type checking.
- Implement integration tests for complete operations.
- Test error handling scenarios thoroughly.
- Validate performance under load.

## Code Organization
- Organize schema files by domain or feature.
- Use schema stitching or federation for large schemas.
- Separate business logic from GraphQL layer.
- Implement proper resolver organization.
- Use consistent file and folder naming conventions.

## Monitoring and Analytics
- Track query performance and usage patterns.
- Monitor error rates and types.
- Analyze field usage for schema optimization.
- Implement proper logging for debugging.
- Use APM tools for performance monitoring.