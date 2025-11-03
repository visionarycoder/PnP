---
description: Integration patterns and API development best practices
applyTo: '**/*integration*,**/*api*,**/webhook*,**/*client*'
---

# Integration Instructions

## Scope
Applies to API development, service integration, webhooks, and client implementations.

## API Design Principles
- Follow RESTful design principles and HTTP standards.
- Use consistent URL naming conventions (kebab-case).
- Implement proper HTTP status codes and error responses.
- Version APIs using URL paths or headers.
- Design for idempotency where applicable.

## Error Handling
- Return structured error responses with error codes.
- Implement consistent error handling across all endpoints.
- Use appropriate HTTP status codes (400, 401, 403, 404, 500, etc.).
- Provide meaningful error messages for debugging.
- Log errors with sufficient context for troubleshooting.

## Authentication and Authorization
- Implement proper authentication mechanisms (JWT, OAuth2, API keys).
- Use HTTPS for all API communications.
- Implement rate limiting to prevent abuse.
- Validate and sanitize all input data.
- Use role-based access control (RBAC) where appropriate.

## Data Formats and Validation
- Use JSON for data exchange unless specific requirements dictate otherwise.
- Implement input validation at API boundaries.
- Use consistent date/time formats (ISO 8601).
- Handle null and empty values appropriately.
- Document data schemas and formats clearly.

## Async Integration Patterns
- Use message queues for decoupled communication.
- Implement proper retry mechanisms with exponential backoff.
- Handle duplicate messages with idempotency patterns.
- Use dead letter queues for failed message processing.
- Implement circuit breaker patterns for resilience.

## Webhook Implementation
- Verify webhook signatures for security.
- Implement proper payload validation.
- Return appropriate HTTP status codes quickly.
- Process webhook payloads asynchronously when possible.
- Implement replay mechanisms for failed deliveries.

## Client Library Design
- Use builder patterns for complex client configurations.
- Implement proper connection pooling and reuse.
- Handle network timeouts and retries gracefully.
- Provide both synchronous and asynchronous methods.
- Use appropriate serialization libraries.

## Performance Optimization
- Implement caching strategies at appropriate layers.
- Use connection pooling for database and HTTP clients.
- Implement pagination for large data sets.
- Use compression for large payloads.
- Monitor and optimize API response times.

## Testing Strategies
- Unit test business logic with mocked dependencies.
- Integration test API endpoints with real dependencies.
- Contract test between services using tools like Pact.
- Load test critical integration points.
- Test error scenarios and edge cases.

## Monitoring and Observability
- Implement structured logging with correlation IDs.
- Use distributed tracing for request flows.
- Monitor API metrics (latency, throughput, error rates).
- Implement health check endpoints.
- Set up alerting for critical integration failures.

## Documentation Standards
- Provide comprehensive API documentation (OpenAPI/Swagger).
- Include code examples for common use cases.
- Document authentication requirements and flows.
- Maintain up-to-date integration guides.
- Document breaking changes and migration paths.

## Versioning and Compatibility
- Use semantic versioning for APIs and client libraries.
- Maintain backward compatibility when possible.
- Provide deprecation notices with migration timelines.
- Use feature flags for gradual rollouts.
- Document breaking changes clearly.