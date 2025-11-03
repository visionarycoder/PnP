# Docker Containerization

Comprehensive Docker patterns and best practices for secure, production-ready containerization following industry standards and modern deployment strategies.

## Overview

This section covers enterprise-grade Docker patterns focusing on:

- **Multi-Stage Builds** - Optimized image sizes with build-time vs runtime separation
- **Security Hardening** - Non-root users, vulnerability scanning, secrets management  
- **Production Orchestration** - Docker Compose, resource limits, health monitoring
- **Performance Optimization** - Layer caching, minimal base images, efficient builds
- **Development Workflow** - Hot-reloading, bind mounts, consistent environments

## Index

- [Dockerfile Examples](dockerfile-examples.md) - Production-ready Dockerfiles with security and optimization patterns

## Security Foundation

### Trusted Base Images
```dockerfile
# Use official images with specific tags for reproducibility
FROM node:18.19-alpine AS base
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime

# Verify image signatures (Docker Content Trust)
export DOCKER_CONTENT_TRUST=1
```

### Non-Root Security
```dockerfile
# Create dedicated application user
RUN addgroup -g 1001 -S appgroup && \
    adduser -S appuser -u 1001 -G appgroup

# Switch to non-root user
USER appuser:appgroup
WORKDIR /app

# Set proper ownership
COPY --chown=appuser:appgroup package*.json ./
```

### Vulnerability Management
```bash
# Scan images for security vulnerabilities
docker scout cves myapp:latest
docker scout recommendations myapp:latest

# Trivy security scanning
trivy image --severity HIGH,CRITICAL myapp:latest

# Regular base image updates
docker pull node:18.19-alpine
```

## Multi-Stage Build Optimization

### Production-Ready Node.js Application

```dockerfile
# Build stage - includes development dependencies
FROM node:18.19-alpine AS builder
WORKDIR /app

# Copy package files first for better caching
COPY package*.json ./
RUN npm ci --include=dev --frozen-lockfile

# Copy source and build
COPY . .
RUN npm run build && npm prune --omit=dev

# Runtime stage - minimal production image
FROM node:18.19-alpine AS runtime
RUN addgroup -g 1001 -S nodejs && adduser -S appuser -u 1001

# Set security context
USER appuser
WORKDIR /app

# Copy only production files
COPY --from=builder --chown=appuser:nodejs /app/node_modules ./node_modules
COPY --from=builder --chown=appuser:nodejs /app/dist ./dist
COPY --from=builder --chown=appuser:nodejs /app/package.json ./

# Health monitoring
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
  CMD node healthcheck.js || exit 1

EXPOSE 3000
CMD ["node", "dist/server.js"]
```

### .NET Application Multi-Stage

```dockerfile
# SDK stage for building
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /source

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore --use-current-runtime

# Copy source and publish
COPY . .
RUN dotnet publish -c Release -o /app --use-current-runtime --self-contained false --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
RUN adduser --disabled-password --home /app --gecos '' appuser && chown -R appuser /app
USER appuser
WORKDIR /app

COPY --from=build --chown=appuser /app ./

HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["./MyApp"]
```

## Docker Compose Orchestration

### Production Service Stack

```yaml
version: '3.8'

services:
  app:
    build:
      context: .
      dockerfile: Dockerfile
      target: runtime
    image: myapp:latest
    container_name: myapp
    restart: unless-stopped
    
    # Security configuration
    user: "1001:1001"
    read_only: true
    cap_drop:
      - ALL
    cap_add:
      - NET_BIND_SERVICE
    
    # Resource constraints
    deploy:
      resources:
        limits:
          memory: 512M
          cpus: '0.50'
        reservations:
          memory: 256M
          cpus: '0.25'
    
    # Environment configuration
    env_file:
      - .env.production
    environment:
      - NODE_ENV=production
      - PORT=3000
    
    # Secrets management
    secrets:
      - db_password
      - api_key
    
    # Volume mounts (minimal for security)
    volumes:
      - app_logs:/app/logs
      - /tmp
    
    # Health monitoring
    healthcheck:
      test: ["CMD", "node", "healthcheck.js"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 60s
    
    # Network configuration
    networks:
      - app_network
    
    # Port exposure
    ports:
      - "127.0.0.1:3000:3000"
    
    depends_on:
      database:
        condition: service_healthy

  database:
    image: postgres:15.4-alpine
    container_name: myapp_db
    restart: unless-stopped
    
    # Security hardening
    user: postgres
    
    # Environment configuration
    environment:
      POSTGRES_DB: ${DB_NAME:-myapp}
      POSTGRES_USER: ${DB_USER:-postgres}
      POSTGRES_PASSWORD_FILE: /run/secrets/db_password
    
    # Secrets
    secrets:
      - db_password
    
    # Persistent storage
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./init-scripts:/docker-entrypoint-initdb.d:ro
    
    # Health check
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${DB_USER:-postgres} -d ${DB_NAME:-myapp}"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 30s
    
    networks:
      - app_network
    
    # Internal port only (no external exposure)
    expose:
      - "5432"

# Secrets management
secrets:
  db_password:
    file: ./secrets/db_password.txt
  api_key:
    file: ./secrets/api_key.txt

# Named volumes for persistence
volumes:
  postgres_data:
    driver: local
  app_logs:
    driver: local

# Network isolation
networks:
  app_network:
    driver: bridge
    ipam:
      config:
        - subnet: 172.20.0.0/16
```

## Container Management

### Build Optimization

```bash
# Optimized build with BuildKit
export DOCKER_BUILDKIT=1
docker build \
  --progress=plain \
  --build-arg BUILDKIT_INLINE_CACHE=1 \
  --cache-from myapp:cache \
  --target runtime \
  -t myapp:latest \
  -f Dockerfile .

# Multi-platform builds
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  --push \
  -t myapp:latest \
  .

# Build secrets (avoid copying sensitive files)
docker build \
  --secret id=npm_token,src=./npm_token \
  --target runtime \
  -t myapp:latest .
```

### Security-Hardened Execution

```bash
# Production container with security constraints
docker run -d \
  --name myapp \
  --restart unless-stopped \
  --user 1001:1001 \
  --read-only \
  --tmpfs /tmp:rw,size=100m \
  --tmpfs /var/run:rw,size=50m \
  --cap-drop ALL \
  --cap-add NET_BIND_SERVICE \
  --security-opt no-new-privileges:true \
  --security-opt seccomp:default \
  --memory 512m \
  --cpus 0.5 \
  --health-cmd "node healthcheck.js" \
  --health-interval 30s \
  --health-timeout 10s \
  --health-retries 3 \
  -p 127.0.0.1:3000:3000 \
  myapp:latest

# Development container with bind mounts
docker run -it --rm \
  --name myapp-dev \
  --user $(id -u):$(id -g) \
  -v "$(pwd)":/app:cached \
  -v /app/node_modules \
  -p 3000:3000 \
  -e NODE_ENV=development \
  myapp:latest npm run dev
```

### Monitoring and Diagnostics

```bash
# Health status monitoring
docker inspect --format='{{.State.Health.Status}}' myapp
docker inspect --format='{{range .State.Health.Log}}{{.Output}}{{end}}' myapp

# Resource utilization
docker stats --no-stream myapp
docker exec myapp sh -c 'cat /proc/meminfo | grep MemAvailable'

# Container logs with rotation
docker logs --since 1h --follow --tail 100 myapp

# Process inspection
docker exec myapp ps aux
docker top myapp

# File system analysis
docker exec myapp df -h
docker diff myapp
```

### Maintenance Operations

```bash
# Update base images
docker pull node:18.19-alpine
docker build --pull -t myapp:latest .

# Security scanning
docker scout cves myapp:latest
trivy image --severity HIGH,CRITICAL myapp:latest

# Resource cleanup
docker container prune -f
docker image prune -f --filter "until=24h"
docker volume prune -f
docker network prune -f

# Complete system cleanup (use with caution)
docker system prune -af --volumes --filter "until=168h"

# Export/Import for backup
docker save myapp:latest | gzip > myapp-backup.tar.gz
gunzip -c myapp-backup.tar.gz | docker load
```

## Enterprise Patterns

### Production Deployment

- **Zero-Downtime Deployments** - Rolling updates with health checks
- **Blue-Green Deployment** - Environment switching strategies  
- **Canary Releases** - Gradual traffic migration patterns
- **Resource Scaling** - Horizontal and vertical scaling approaches

### Security Hardening

- **Runtime Security** - Non-root users, capability dropping, read-only filesystems
- **Secret Management** - External secret stores, rotation strategies
- **Network Isolation** - Custom networks, service segmentation
- **Vulnerability Management** - Automated scanning, patch management

### Performance Optimization

- **Image Optimization** - Multi-stage builds, minimal base images
- **Layer Caching** - Build optimization, CI/CD cache strategies
- **Resource Efficiency** - Memory limits, CPU constraints
- **Storage Performance** - Volume types, mount optimization

### Development Experience

- **Hot Reloading** - Development container patterns
- **Testing Strategies** - Container testing, integration tests
- **Debugging Tools** - Remote debugging, log aggregation
- **Local Development** - Docker Compose workflows

### Monitoring and Observability

- **Health Monitoring** - Application health checks, dependency checks
- **Log Management** - Structured logging, centralized aggregation
- **Metrics Collection** - Performance monitoring, resource tracking
- **Distributed Tracing** - Request tracking across containers

### CI/CD Integration

- **Build Automation** - Multi-stage pipelines, artifact management
- **Security Scanning** - Automated vulnerability detection
- **Image Registry** - Artifact storage, version management
- **Deployment Automation** - Infrastructure as code, GitOps patterns
