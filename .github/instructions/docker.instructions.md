---
description: Docker containerization best practices and standards
applyTo: '**/Dockerfile*,**/*.dockerfile,**/docker-compose*.yml'
---

# Docker Instructions

## Scope
Applies to `Dockerfile`, `docker-compose.yml`, and container development.

## Dockerfile Best Practices
- Use official base images from trusted registries.
- Use specific tags instead of `latest` for reproducibility.
- Minimize layer count by combining RUN commands.
- Use multi-stage builds for production images.
- Order instructions by frequency of change (cache optimization).

## Image Optimization
- Use `.dockerignore` to exclude unnecessary files.
- Remove package managers and build tools in final stage.
- Use minimal base images (Alpine, distroless).
- Clean up caches and temporary files in same RUN command.
- Optimize image layers for better caching.

## Security Practices
- Run containers as non-root user when possible.
- Use `USER` instruction to set appropriate user.
- Scan images for vulnerabilities regularly.
- Use secrets management for sensitive data.
- Keep base images updated with security patches.

## Container Configuration
- Use `COPY` instead of `ADD` unless specific features needed.
- Set appropriate `WORKDIR` for clarity.
- Use `ENTRYPOINT` for primary command, `CMD` for default arguments.
- Implement proper signal handling in applications.
- Use health checks to monitor container status.

## Environment Management
- Use environment variables for configuration.
- Provide sensible defaults for environment variables.
- Use `ARG` for build-time variables.
- Document required environment variables.
- Use `.env` files for local development.

## Docker Compose
- Use version 3+ compose file format.
- Define services with clear, descriptive names.
- Use named volumes for persistent data.
- Implement proper networking between services.
- Use profiles for different deployment scenarios.

## Volume Management
- Use named volumes for persistent data.
- Mount configuration files as read-only when possible.
- Avoid mounting sensitive host directories.
- Use tmpfs for temporary file storage.
- Document volume requirements clearly.

## Networking
- Use custom networks instead of default bridge.
- Implement proper service discovery patterns.
- Use appropriate port mappings.
- Secure inter-container communication.
- Document network dependencies.

## Logging and Monitoring
- Configure appropriate logging drivers.
- Use structured logging formats.
- Implement health check endpoints.
- Monitor container resource usage.
- Use labels for metadata and organization.

## Development Workflow
- Use bind mounts for development hot-reloading.
- Implement proper build contexts.
- Use build args for configuration.
- Test images in isolation before deployment.
- Use consistent naming conventions.

## Production Deployment
- Use orchestration platforms (Kubernetes, Docker Swarm).
- Implement proper resource limits and requests.
- Use rolling updates for zero-downtime deployments.
- Configure proper restart policies.
- Implement service mesh for complex applications.

## Performance Optimization
- Use appropriate resource limits (CPU, memory).
- Optimize application startup time.
- Use efficient base images.
- Implement proper caching strategies.
- Monitor and tune container performance.