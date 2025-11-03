---
description: .NET Aspire cloud-native application development best practices
applyTo: '**/aspire/**,**/*.aspire.*,**/AppHost/**,**/ServiceDefaults/**'
---

# Aspire Instructions

## Scope
Applies to .NET Aspire cloud-native application development, service orchestration, and distributed system architecture.

## Application Host Design
- Use the AppHost project as the orchestration entry point.
- Define service dependencies and relationships clearly.
- Implement proper service discovery and communication patterns.
- Use appropriate resource allocation and scaling strategies.
- Configure proper health checks and monitoring.

## Service Architecture
- Design services with clear boundaries and responsibilities.
- Implement proper inter-service communication patterns.
- Use appropriate data persistence strategies per service.
- Handle service failures and implement resilience patterns.
- Design for horizontal scaling and load distribution.

## Configuration Management
- Use structured configuration with strong typing.
- Implement environment-specific configuration strategies.
- Use configuration validation and binding patterns.
- Secure sensitive configuration with proper secret management.
- Document configuration requirements and defaults.

## Observability and Monitoring
- Implement distributed tracing across service boundaries.
- Use structured logging with correlation identifiers.
- Configure appropriate metrics collection and alerting.
- Implement health checks for all service dependencies.
- Use OpenTelemetry for standardized observability.

## Resource Management
- Define infrastructure requirements declaratively.
- Use appropriate containerization strategies.
- Implement proper resource limits and requests.
- Configure networking and service mesh requirements.
- Use infrastructure as code for reproducible deployments.

## Development Workflow
- Use local development containers for consistency.
- Implement hot reload and fast feedback cycles.
- Use appropriate debugging strategies for distributed systems.
- Test services in isolation and integration scenarios.
- Use feature flags for gradual rollouts.

## Security Best Practices
- Implement proper authentication and authorization patterns.
- Use secure communication protocols (TLS/mTLS).
- Implement proper secret management and rotation.
- Use principle of least privilege for service permissions.
- Regular security scanning and vulnerability assessment.

## Data Management
- Use appropriate data consistency patterns (eventual consistency, ACID).
- Implement proper database per service patterns.
- Use event sourcing and CQRS where appropriate.
- Handle distributed transactions carefully.
- Implement proper data backup and recovery strategies.

## Performance Optimization
- Implement caching strategies at appropriate layers.
- Use connection pooling and resource reuse.
- Optimize serialization and communication protocols.
- Monitor and optimize resource utilization.
- Use appropriate async patterns and non-blocking I/O.

## Testing Strategies
- Unit test business logic with proper isolation.
- Integration test service interactions.
- Contract test service interfaces.
- End-to-end test critical user journeys.
- Performance test under realistic load conditions.

## Deployment and Operations
- Use blue-green or rolling deployment strategies.
- Implement proper CI/CD pipelines.
- Use GitOps for deployment automation.
- Implement proper backup and disaster recovery.
- Monitor and alert on key operational metrics.

## Cloud-Native Patterns
- Implement proper retry and circuit breaker patterns.
- Use bulkhead isolation for fault tolerance.
- Implement graceful degradation strategies.
- Use appropriate load balancing and traffic management.
- Design for multi-region deployment when required.