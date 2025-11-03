# Dockerfile Examples

**Description**: Production-ready Dockerfile patterns with security hardening, multi-stage builds, and performance optimization for enterprise containerization.

**Language/Technology**: Docker, Containerization

**Code**:

```dockerfile
# ============================================
# Production Node.js Application with Security
# ============================================
# syntax=docker/dockerfile:1

# Build stage - includes development dependencies and build tools
FROM node:18.19-alpine AS builder

# Install build dependencies for native modules
RUN apk add --no-cache python3 make g++

# Set working directory
WORKDIR /app

# Copy package files first for optimal layer caching
COPY package*.json ./

# Install ALL dependencies (including devDependencies for building)
RUN npm ci --include=dev --frozen-lockfile

# Copy source code
COPY . .

# Build application with optimizations
RUN npm run build && \
    npm prune --omit=dev && \
    npm cache clean --force

# Runtime stage - minimal production image
FROM node:18.19-alpine AS runtime

# Install security updates and required packages only
RUN apk update && \
    apk upgrade && \
    apk add --no-cache dumb-init curl && \
    rm -rf /var/cache/apk/*

# Create application user with specific UID/GID
RUN addgroup -g 1001 -S nodejs && \
    adduser -S -u 1001 -G nodejs nodejs

# Set working directory and ownership
WORKDIR /app
RUN chown nodejs:nodejs /app

# Copy application files from builder with proper ownership
COPY --from=builder --chown=nodejs:nodejs /app/node_modules ./node_modules
COPY --from=builder --chown=nodejs:nodejs /app/dist ./dist
COPY --from=builder --chown=nodejs:nodejs /app/package.json ./

# Create health check script
COPY --chown=nodejs:nodejs healthcheck.js ./

# Switch to non-root user
USER nodejs

# Health monitoring with proper timeout and retries
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD node healthcheck.js || exit 1

# Expose port (use non-privileged port)
EXPOSE 3000

# Use dumb-init to handle signals properly
ENTRYPOINT ["dumb-init", "--"]

# Start application
CMD ["node", "dist/server.js"]

# ============================================
# Python FastAPI Application with Security Optimization
# ============================================
# Build stage for compiled dependencies
FROM python:3.12-slim AS builder

# Security and performance environment variables
ENV PYTHONUNBUFFERED=1 \
    PYTHONDONTWRITEBYTECODE=1 \
    PIP_NO_CACHE_DIR=1 \
    PIP_DISABLE_PIP_VERSION_CHECK=1 \
    PIP_DEFAULT_TIMEOUT=100

# Update system and install build dependencies
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        build-essential \
        gcc \
        libc6-dev \
        libffi-dev \
        libssl-dev && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /build

# Install Python dependencies with wheels
COPY requirements.txt .
RUN pip wheel --no-cache-dir --no-deps --wheel-dir /wheels -r requirements.txt

# Production stage - distroless for maximum security
FROM python:3.12-slim AS runtime

# Install security updates and minimal runtime dependencies
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        curl \
        tini && \
    apt-get upgrade -y && \
    rm -rf /var/lib/apt/lists/* /tmp/* /var/tmp/*

# Create application user
RUN groupadd -r -g 1001 appgroup && \
    useradd -r -u 1001 -g appgroup -m -d /app -s /bin/bash appuser

# Set up working directory
WORKDIR /app
RUN chown -R appuser:appgroup /app

# Copy wheels and install Python packages
COPY --from=builder /wheels /wheels
COPY requirements.txt .
RUN pip install --no-cache-dir --no-index --find-links /wheels -r requirements.txt && \
    rm -rf /wheels requirements.txt

# Copy application with proper ownership
COPY --chown=appuser:appgroup . .

# Switch to non-root user
USER appuser

# Health check with proper error handling
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8000/health || exit 1

# Expose application port
EXPOSE 8000

# Use tini as PID 1 to handle signals properly
ENTRYPOINT ["tini", "--"]

# Start FastAPI with Uvicorn
CMD ["python", "-m", "uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000", "--workers", "1"]

# ============================================
# .NET 8 Web API with Chiseled Ubuntu (Secure & Minimal)
# ============================================
# Build stage with full SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /source

# Copy project file and restore as distinct layers
COPY Directory.Build.props .
COPY src/MyApp/*.csproj src/MyApp/

# Restore dependencies for better caching
RUN dotnet restore src/MyApp/MyApp.csproj --use-current-runtime

# Copy source code
COPY src/ src/

# Build and publish with optimizations
WORKDIR /source/src/MyApp
RUN dotnet publish MyApp.csproj \
    --configuration Release \
    --runtime linux-x64 \
    --self-contained false \
    --no-restore \
    --output /app/publish \
    -p:PublishTrimmed=false \
    -p:PublishSingleFile=false

# Runtime stage with chiseled Ubuntu (ultra-secure)
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled AS runtime

# Create application user (chiseled images require explicit user creation)
RUN groupadd -r -g 1000 appgroup && \
    useradd -r -u 1000 -g appgroup -m -d /app appuser

# Set working directory and ownership
WORKDIR /app
RUN chown appuser:appgroup /app

# Copy published application with proper ownership
COPY --from=build --chown=appuser:appgroup /app/publish .

# Switch to non-root user
USER appuser

# Configure ASP.NET Core
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_GENERATE_ASPNET_CERTIFICATE=false \
    DOTNET_USE_POLLING_FILE_WATCHER=true

# Health check using built-in endpoint
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Expose HTTP port (HTTPS handled by reverse proxy)
EXPOSE 8080

# Entry point for the application
ENTRYPOINT ["dotnet", "MyApp.dll"]

# ============================================
# React SPA with Nginx and Security Headers
# ============================================
# Build stage with Node.js
FROM node:18.19-alpine AS builder

# Install build dependencies
RUN apk add --no-cache python3 make g++

WORKDIR /app

# Copy package files for optimal caching
COPY package*.json ./

# Install dependencies with exact versions
RUN npm ci --frozen-lockfile

# Copy source code and build
COPY . .

# Build optimized production bundle
RUN npm run build && \
    npm run test:coverage && \
    npm audit --audit-level=moderate

# Production stage with security-hardened Nginx
FROM nginx:1.25-alpine AS runtime

# Install security updates
RUN apk update && \
    apk upgrade && \
    apk add --no-cache curl && \
    rm -rf /var/cache/apk/*

# Remove default nginx files
RUN rm -rf /usr/share/nginx/html/* /etc/nginx/conf.d/default.conf

# Create nginx user (if not exists)
RUN adduser -D -s /bin/false nginx || true

# Copy custom nginx configuration with security headers
COPY docker/nginx.conf /etc/nginx/nginx.conf
COPY docker/security-headers.conf /etc/nginx/conf.d/security-headers.conf

# Copy built application files
COPY --from=builder /app/dist /usr/share/nginx/html

# Set proper permissions
RUN chown -R nginx:nginx /usr/share/nginx/html && \
    chmod -R 644 /usr/share/nginx/html

# Health check for nginx and application
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:80/health.html || exit 1

# Use non-root user
USER nginx

# Expose HTTP port (HTTPS terminated at load balancer)
EXPOSE 80

# Start nginx in foreground
CMD ["nginx", "-g", "daemon off;"]

# ============================================
# Go Microservice with Distroless Base
# ============================================
# Build stage with Go SDK
FROM golang:1.21-alpine AS builder

# Install build dependencies and security updates
RUN apk update && \
    apk add --no-cache git ca-certificates tzdata && \
    update-ca-certificates

# Create appuser for security
RUN adduser -D -g '' appuser

WORKDIR /build

# Copy dependency files first for caching
COPY go.mod go.sum ./

# Download dependencies with verification
RUN go mod download && \
    go mod verify

# Copy source code
COPY . .

# Build optimized binary with security flags
RUN CGO_ENABLED=0 \
    GOOS=linux \
    GOARCH=amd64 \
    go build \
        -ldflags='-w -s -extldflags "-static"' \
        -a -installsuffix cgo \
        -o app \
        ./cmd/server

# Production stage - Google Distroless (ultra-minimal and secure)
FROM gcr.io/distroless/static-debian11:nonroot

# Copy timezone data and certificates
COPY --from=builder /usr/share/zoneinfo /usr/share/zoneinfo
COPY --from=builder /etc/ssl/certs/ca-certificates.crt /etc/ssl/certs/

# Copy application binary
COPY --from=builder /build/app /app

# Use non-root user (provided by distroless)
USER nonroot:nonroot

# Expose application port
EXPOSE 8080

# Health check is handled by Kubernetes probes in production
# For local development, add health endpoint to your Go app

# Entry point
ENTRYPOINT ["/app"]

# ============================================
# Go Service with Alpine (Alternative approach)
# ============================================
FROM golang:1.21-alpine AS builder-alpine

# Security and build setup
RUN apk update && \
    apk add --no-cache \
        git \
        ca-certificates \
        tzdata \
        gcc \
        musl-dev && \
    update-ca-certificates

WORKDIR /build

# Dependency management
COPY go.mod go.sum ./
RUN go mod download

COPY . .

# Build with Alpine-specific optimizations
RUN CGO_ENABLED=1 \
    GOOS=linux \
    GOARCH=amd64 \
    go build \
        -ldflags='-w -s' \
        -a -installsuffix cgo \
        -o app \
        ./cmd/server

# Runtime stage with minimal Alpine
FROM alpine:3.19

# Install runtime dependencies only
RUN apk update && \
    apk add --no-cache \
        ca-certificates \
        curl \
        tzdata && \
    rm -rf /var/cache/apk/*

# Create application user
RUN adduser -D -s /bin/false appuser

WORKDIR /app

# Copy binary with proper ownership
COPY --from=builder-alpine --chown=appuser:appuser /build/app .

# Switch to non-root user
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Expose port
EXPOSE 8080

# Start application
ENTRYPOINT ["./app"]
```

**Usage**:

## Build Commands with Optimization

```bash
# Enable BuildKit for improved performance and features
export DOCKER_BUILDKIT=1

# Basic optimized build with caching
docker build \
  --progress=plain \
  --build-arg BUILDKIT_INLINE_CACHE=1 \
  --cache-from myapp:cache \
  -t myapp:latest \
  .

# Multi-platform build for ARM and x86
docker buildx create --use --name multiarch
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  --push \
  -t myregistry.com/myapp:latest \
  .

# Build with secrets (avoid copying sensitive files)
docker build \
  --secret id=npmrc,src=$HOME/.npmrc \
  --secret id=ssh_key,src=$HOME/.ssh/id_rsa \
  -t myapp:latest \
  .

# Build with specific target stage
docker build \
  --target runtime \
  -t myapp:prod \
  .

# Build with build arguments and labels
docker build \
  --build-arg VERSION=1.2.3 \
  --build-arg BUILD_DATE=$(date -u +'%Y-%m-%dT%H:%M:%SZ') \
  --label "org.opencontainers.image.version=1.2.3" \
  --label "org.opencontainers.image.created=$(date -u +'%Y-%m-%dT%H:%M:%SZ')" \
  -t myapp:1.2.3 \
  .
```

## Secure Container Execution

```bash
# Production container with security hardening
docker run -d \
  --name myapp-prod \
  --restart unless-stopped \
  --user 1001:1001 \
  --read-only \
  --tmpfs /tmp:rw,size=100m,mode=1777 \
  --tmpfs /var/run:rw,size=50m,mode=755 \
  --cap-drop ALL \
  --cap-add NET_BIND_SERVICE \
  --security-opt no-new-privileges:true \
  --security-opt seccomp:default \
  --memory 512m \
  --cpus 0.5 \
  --health-cmd "curl -f http://localhost:3000/health || exit 1" \
  --health-interval 30s \
  --health-timeout 10s \
  --health-retries 3 \
  --health-start-period 60s \
  -p 127.0.0.1:3000:3000 \
  -e NODE_ENV=production \
  -e LOG_LEVEL=info \
  --log-driver json-file \
  --log-opt max-size=10m \
  --log-opt max-file=3 \
  myapp:latest

# Development container with hot reload
docker run -it --rm \
  --name myapp-dev \
  --user $(id -u):$(id -g) \
  -v "$(pwd)":/app:cached \
  -v /app/node_modules \
  -v myapp-cache:/app/.cache \
  -p 3000:3000 \
  -e NODE_ENV=development \
  -e CHOKIDAR_USEPOLLING=true \
  myapp:latest \
  npm run dev

# Container with external network and secrets
docker run -d \
  --name myapp-secure \
  --network myapp-network \
  --secret api_key \
  --secret db_password \
  -e API_KEY_FILE=/run/secrets/api_key \
  -e DB_PASSWORD_FILE=/run/secrets/db_password \
  myapp:latest
```

## Production Docker Compose Stack

```yaml
# docker-compose.prod.yml - Production-ready multi-service stack
version: '3.8'

services:
  # Reverse Proxy with SSL termination
  traefik:
    image: traefik:v3.0
    container_name: traefik
    restart: unless-stopped
    command:
      - --api.dashboard=false
      - --providers.docker=true
      - --providers.docker.exposedbydefault=false
      - --entrypoints.web.address=:80
      - --entrypoints.websecure.address=:443
      - --certificatesresolvers.letsencrypt.acme.tlschallenge=true
      - --certificatesresolvers.letsencrypt.acme.email=${ACME_EMAIL}
      - --certificatesresolvers.letsencrypt.acme.storage=/letsencrypt/acme.json
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock:ro
      - letsencrypt_data:/letsencrypt
    networks:
      - web_network

  # Application service
  app:
    build:
      context: .
      dockerfile: Dockerfile
      target: runtime
      cache_from:
        - ${REGISTRY}/myapp:cache
    image: ${REGISTRY}/myapp:${VERSION:-latest}
    container_name: myapp
    restart: unless-stopped
    
    # Security configuration
    user: "1001:1001"
    read_only: true
    cap_drop:
      - ALL
    cap_add:
      - NET_BIND_SERVICE
    security_opt:
      - no-new-privileges:true
    
    # Resource management
    deploy:
      resources:
        limits:
          memory: 1G
          cpus: '1.0'
        reservations:
          memory: 512M
          cpus: '0.5'
    
    # Environment configuration
    env_file:
      - .env.production
    environment:
      NODE_ENV: production
      DATABASE_URL: postgresql://${DB_USER}:${DB_PASS}@database:5432/${DB_NAME}
      REDIS_URL: redis://redis:6379
      LOG_LEVEL: ${LOG_LEVEL:-info}
    
    # Secrets management
    secrets:
      - jwt_secret
      - api_keys
    
    # Volume mounts (read-only where possible)
    volumes:
      - app_logs:/app/logs
      - app_uploads:/app/uploads
      - /tmp
    
    # Health monitoring
    healthcheck:
      test: ["CMD", "node", "healthcheck.js"]
      interval: 30s
      timeout: 15s
      retries: 3
      start_period: 120s
    
    # Network and dependencies
    networks:
      - app_network
      - web_network
    depends_on:
      database:
        condition: service_healthy
      redis:
        condition: service_healthy
    
    # Traefik labels for automatic discovery
    labels:
      - traefik.enable=true
      - traefik.http.routers.app.rule=Host(`${DOMAIN}`)
      - traefik.http.routers.app.entrypoints=websecure
      - traefik.http.routers.app.tls.certresolver=letsencrypt
      - traefik.http.services.app.loadbalancer.server.port=3000

  # Database with backup automation
  database:
    image: postgres:15.4-alpine
    container_name: postgres_db
    restart: unless-stopped
    
    # Security
    user: postgres
    
    # Environment configuration
    environment:
      POSTGRES_DB: ${DB_NAME}
      POSTGRES_USER: ${DB_USER}
      POSTGRES_PASSWORD_FILE: /run/secrets/db_password
      POSTGRES_INITDB_ARGS: "--auth-host=scram-sha-256"
    
    # Secrets
    secrets:
      - db_password
    
    # Persistent storage and configuration
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - postgres_backups:/backups
      - ./init-scripts:/docker-entrypoint-initdb.d:ro
      - ./postgresql.conf:/etc/postgresql/postgresql.conf:ro
    
    # Performance tuning
    command: >
      postgres
      -c config_file=/etc/postgresql/postgresql.conf
      -c log_statement=all
      -c log_destination=stderr
      -c logging_collector=on
      -c max_connections=100
      -c shared_buffers=256MB
    
    # Resource limits
    deploy:
      resources:
        limits:
          memory: 1G
          cpus: '0.8'
    
    # Health monitoring
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${DB_USER} -d ${DB_NAME}"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 60s
    
    networks:
      - app_network

  # Redis cache with persistence
  redis:
    image: redis:7.2-alpine
    container_name: redis_cache
    restart: unless-stopped
    
    # Security
    user: redis
    
    # Configuration
    command: >
      redis-server
      --appendonly yes
      --appendfsync everysec
      --maxmemory 256mb
      --maxmemory-policy allkeys-lru
      --requirepass ${REDIS_PASSWORD}
    
    # Persistent storage
    volumes:
      - redis_data:/data
    
    # Resource limits
    deploy:
      resources:
        limits:
          memory: 512M
          cpus: '0.3'
    
    # Health check
    healthcheck:
      test: ["CMD", "redis-cli", "--raw", "incr", "ping"]
      interval: 10s
      timeout: 3s
      retries: 5
    
    networks:
      - app_network

# External secrets (use external secret management in production)
secrets:
  db_password:
    external: true
    name: ${STACK_NAME}_db_password
  jwt_secret:
    external: true
    name: ${STACK_NAME}_jwt_secret
  api_keys:
    external: true
    name: ${STACK_NAME}_api_keys

# Named volumes for data persistence
volumes:
  postgres_data:
    driver: local
  postgres_backups:
    driver: local
  redis_data:
    driver: local
  app_logs:
    driver: local
  app_uploads:
    driver: local
  letsencrypt_data:
    driver: local

# Network segmentation
networks:
  app_network:
    driver: bridge
    internal: true
    ipam:
      config:
        - subnet: 172.20.0.0/16
  web_network:
    driver: bridge
    ipam:
      config:
        - subnet: 172.21.0.0/16
```

## Enterprise Best Practices

### Security-First Development

```dockerfile
# Always use specific, current versions with security patches
FROM node:18.19-alpine  # Not 'latest' or generic versions

# Security hardening from the start
RUN apk update && apk upgrade && \
    apk add --no-cache dumb-init curl && \
    rm -rf /var/cache/apk/*

# Create dedicated application user with specific UID/GID
RUN addgroup -g 1001 -S appgroup && \
    adduser -S -u 1001 -G appgroup appuser

# Use non-root user throughout the build
USER appuser
WORKDIR /app

# Implement proper signal handling
ENTRYPOINT ["dumb-init", "--"]
CMD ["node", "server.js"]
```

### Build Optimization Patterns

```dockerfile
# Optimal layer caching strategy
COPY package*.json ./                    # Dependencies first
RUN npm ci --frozen-lockfile            # Install with exact versions
COPY src/ src/                          # Source code second  
COPY public/ public/                    # Static assets last
RUN npm run build                       # Build after all inputs

# Multi-stage builds for minimal production images
FROM node:18-alpine AS dependencies
# Install all dependencies including dev

FROM node:18-alpine AS builder  
COPY --from=dependencies /app/node_modules ./node_modules
# Build application

FROM node:18-alpine AS runtime
COPY --from=builder /app/dist ./dist    # Only production artifacts
```

### Production Configuration

```dockerfile
# Comprehensive health checks
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD node healthcheck.js || exit 1

# Resource-aware configuration
ENV NODE_OPTIONS="--max-old-space-size=512" \
    UV_THREADPOOL_SIZE=4

# Structured logging for observability
ENV LOG_LEVEL=info \
    LOG_FORMAT=json

# Security environment variables
ENV NODE_ENV=production \
    DISABLE_X_POWERED_BY=true
```

## .dockerignore Best Practices

```dockerignore
# Version control
.git
.gitignore
.gitattributes

# Dependencies (will be installed in container)
node_modules
npm-debug.log*
yarn-debug.log*
yarn-error.log*

# Development and testing
.env
.env.local
.env.*.local
coverage/
.nyc_output
.jest
test/
tests/
**/*.test.js
**/*.spec.js

# Documentation and configuration
README.md
LICENSE
Dockerfile*
docker-compose*.yml
.dockerignore

# OS generated files
.DS_Store
Thumbs.db
*.log

# Build artifacts and caches
dist/
build/
.cache/
.vscode/
.idea/

# Runtime data
logs/
pids/
*.pid
*.seed
*.pid.lock
```

## Container Security Checklist

- ✅ **Base Image Security**: Use official images with specific versions
- ✅ **Non-Root User**: Always run containers as non-privileged user  
- ✅ **Minimal Attack Surface**: Use distroless or Alpine for production
- ✅ **Read-Only Filesystem**: Mount root filesystem as read-only
- ✅ **Capability Dropping**: Drop all capabilities, add only required ones
- ✅ **Resource Limits**: Set memory and CPU limits to prevent DoS
- ✅ **Health Checks**: Implement comprehensive application health monitoring
- ✅ **Secret Management**: Never embed secrets, use external secret stores
- ✅ **Network Segmentation**: Use custom networks with minimal exposure
- ✅ **Log Security**: Avoid logging sensitive information

**Notes**:

- **Multi-stage builds** drastically reduce production image size (often 10x smaller)
- **Alpine Linux** provides minimal attack surface but may require additional packages for some applications
- **Distroless images** offer maximum security for runtime-only deployments
- **BuildKit** enables advanced features like build secrets, cache mounts, and multi-platform builds
- **Health checks** are essential for orchestration platforms like Kubernetes
- **Layer optimization** significantly improves build times and reduces storage requirements
- **Security scanning** should be integrated into CI/CD pipelines for continuous vulnerability assessment
- **Container registries** should enforce image signing and vulnerability scanning

**Related Patterns**: [Docker Compose Orchestration](readme.md#docker-compose-orchestration), [Container Security](readme.md#security-foundation), [Multi-Platform Builds](readme.md#build-optimization)
