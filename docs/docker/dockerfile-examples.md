# Dockerfile Examples

**Description**: Common Dockerfile patterns for containerizing applications.

**Language/Technology**: Docker

**Code**:

```dockerfile
# ============================================
# Example 1: Node.js Application
# ============================================
FROM node:18-alpine AS build

# Set working directory
WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm ci --only=production

# Copy application files
COPY . .

# Build application (if needed)
RUN npm run build

# Production stage
FROM node:18-alpine

WORKDIR /app

# Copy from build stage
COPY --from=build /app/node_modules ./node_modules
COPY --from=build /app/dist ./dist
COPY --from=build /app/package*.json ./

# Create non-root user
RUN addgroup -g 1001 -S nodejs && \
    adduser -S nodejs -u 1001

USER nodejs

EXPOSE 3000

CMD ["node", "dist/index.js"]

# ============================================
# Example 2: Python Application (Multi-stage for compiled dependencies)
# ============================================
FROM python:3.11-slim AS builder

# Set environment variables
ENV PYTHONUNBUFFERED=1 \
    PYTHONDONTWRITEBYTECODE=1 \
    PIP_NO_CACHE_DIR=1

WORKDIR /app

# Install build dependencies (only in builder stage)
RUN apt-get update && \
    apt-get install -y --no-install-recommends gcc && \
    rm -rf /var/lib/apt/lists/*

# Copy requirements first (for caching)
COPY requirements.txt .

# Install Python dependencies (including those that need compilation)
RUN pip install --no-cache-dir -r requirements.txt

# Production stage - minimal image without build tools
FROM python:3.11-slim

ENV PYTHONUNBUFFERED=1 \
    PYTHONDONTWRITEBYTECODE=1

WORKDIR /app

# Copy only the installed packages from builder
COPY --from=builder /usr/local/lib/python3.11/site-packages /usr/local/lib/python3.11/site-packages
COPY --from=builder /usr/local/bin /usr/local/bin

# Copy application
COPY . .

# Create non-root user
RUN useradd -m -u 1001 appuser && \
    chown -R appuser:appuser /app

USER appuser

EXPOSE 8000

CMD ["python", "app.py"]

# ============================================
# Example 3: .NET Core Application
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

WORKDIR /src

# Copy project file
COPY ["MyApp/MyApp.csproj", "MyApp/"]

# Restore dependencies
RUN dotnet restore "MyApp/MyApp.csproj"

# Copy source code
COPY . .

WORKDIR "/src/MyApp"

# Build and publish
RUN dotnet build "MyApp.csproj" -c Release -o /app/build
RUN dotnet publish "MyApp.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:7.0

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "MyApp.dll"]

# ============================================
# Example 4: Static Website (Nginx)
# ============================================
FROM node:18-alpine AS build

WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build

# Production stage with Nginx
FROM nginx:alpine

# Copy custom nginx config
COPY nginx.conf /etc/nginx/nginx.conf

# Copy built files
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]

# ============================================
# Example 5: Multi-Service (Go Application)
# ============================================
FROM golang:1.21-alpine AS builder

WORKDIR /app

# Copy go mod files
COPY go.mod go.sum ./

# Download dependencies
RUN go mod download

# Copy source code
COPY . .

# Build application
RUN CGO_ENABLED=0 GOOS=linux go build -a -installsuffix cgo -o main .

# Final stage - minimal image
FROM alpine:latest

RUN apk --no-cache add ca-certificates

WORKDIR /root/

# Copy binary from builder
COPY --from=builder /app/main .

EXPOSE 8080

CMD ["./main"]
```

**Usage**:

```bash
# Build image
docker build -t my-app:latest .

# Build with specific Dockerfile
docker build -f Dockerfile.prod -t my-app:prod .

# Build with build arguments
docker build --build-arg VERSION=1.0.0 -t my-app:1.0.0 .

# Build multi-platform image
docker buildx build --platform linux/amd64,linux/arm64 -t my-app:latest .

# Run container
docker run -d -p 3000:3000 --name my-app my-app:latest

# Run with environment variables
docker run -d -p 3000:3000 -e NODE_ENV=production my-app:latest

# Run with volume mount
docker run -d -p 3000:3000 -v $(pwd)/data:/app/data my-app:latest
```

**Docker Compose Example**:

```yaml
# docker-compose.yml
version: '3.8'

services:
  app:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "3000:3000"
    environment:
      - NODE_ENV=production
      - DATABASE_URL=postgresql://db:5432/mydb
    depends_on:
      - db
    volumes:
      - ./logs:/app/logs
    restart: unless-stopped

  db:
    image: postgres:15-alpine
    environment:
      - POSTGRES_DB=mydb
      - POSTGRES_USER=user
      - POSTGRES_PASSWORD=password
    volumes:
      - postgres_data:/var/lib/postgresql/data
    restart: unless-stopped

volumes:
  postgres_data:
```

**Best Practices**:

```dockerfile
# Use specific version tags
FROM node:18.17.0-alpine

# Leverage build cache - copy dependency files first
COPY package*.json ./
RUN npm ci

# Then copy application code
COPY . .

# Use multi-stage builds to reduce image size
FROM node:18-alpine AS builder
# ... build steps ...
FROM node:18-alpine AS production
COPY --from=builder /app/dist ./dist

# Don't run as root
RUN adduser -D appuser
USER appuser

# Use .dockerignore to exclude unnecessary files
# .dockerignore file:
# node_modules
# npm-debug.log
# .git
# .env

# Minimize layers by combining RUN commands
RUN apt-get update && \
    apt-get install -y package1 package2 && \
    rm -rf /var/lib/apt/lists/*

# Set working directory
WORKDIR /app

# Use COPY instead of ADD (unless you need ADD's features)
COPY . .

# Set environment variables
ENV NODE_ENV=production

# Expose ports
EXPOSE 3000

# Use ENTRYPOINT for executable containers, CMD for arguments
ENTRYPOINT ["node"]
CMD ["index.js"]
```

**Notes**:

- Multi-stage builds reduce final image size
- Alpine Linux images are smaller but may lack some tools
- Always use specific version tags, avoid `latest`
- Copy dependency files before source code for better caching
- Run as non-root user for security
- Use `.dockerignore` to exclude unnecessary files
- Combine RUN commands to reduce layers
- Clean up package manager caches in the same layer
- Related: [Docker Compose](docker-compose-examples.md), [Docker Commands](docker-commands.md)
