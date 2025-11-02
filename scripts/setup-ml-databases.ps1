#!/usr/bin/env pwsh

# ML Database Technologies Setup Script
# This script sets up the complete ML database stack for local development

Write-Host "🚀 Setting up ML Database Technologies Stack" -ForegroundColor Green

# Check prerequisites
Write-Host "📋 Checking prerequisites..." -ForegroundColor Yellow

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host "❌ Docker not found. Please install Docker Desktop first." -ForegroundColor Red
    exit 1
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "❌ .NET SDK not found. Please install .NET 8+ SDK first." -ForegroundColor Red
    exit 1
}

Write-Host "✅ Prerequisites check passed" -ForegroundColor Green

# Create directory structure
Write-Host "📁 Creating directory structure..." -ForegroundColor Yellow

$directories = @(
    "MLDatabaseExamples\src\ML.Examples",
    "MLDatabaseExamples\docker",
    "MLDatabaseExamples\data\postgres", 
    "MLDatabaseExamples\data\chroma",
    "MLDatabaseExamples\data\clickhouse",
    "MLDatabaseExamples\data\duckdb",
    "MLDatabaseExamples\data\redis",
    "MLDatabaseExamples\sql\init",
    "MLDatabaseExamples\notebooks"
)

foreach ($dir in $directories) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        Write-Host "  Created: $dir" -ForegroundColor Gray
    }
}

# Copy database examples to the new structure
Write-Host "📝 Copying example files..." -ForegroundColor Yellow

# Copy the ML database examples documentation
$sourceDoc = "docs\database\ml-database-examples.md"
$targetDoc = "MLDatabaseExamples\README.md"

if (Test-Path $sourceDoc) {
    Copy-Item $sourceDoc $targetDoc
    Write-Host "  Copied documentation to $targetDoc" -ForegroundColor Gray
}

# Copy the notebook
$sourceNotebook = "notebooks\ml-database-examples.ipynb"
$targetNotebook = "MLDatabaseExamples\notebooks\ml-database-examples.ipynb"

if (Test-Path $sourceNotebook) {
    Copy-Item $sourceNotebook $targetNotebook
    Write-Host "  Copied notebook to $targetNotebook" -ForegroundColor Gray
}

# Navigate to the examples directory
Set-Location "MLDatabaseExamples"

# Create Docker Compose file
Write-Host "🐳 Creating Docker Compose configuration..." -ForegroundColor Yellow

$dockerCompose = @"
version: '3.8'

services:
  # PostgreSQL with vector support
  postgres:
    image: pgvector/pgvector:pg16
    container_name: ml_postgres
    ports:
      - "5432:5432"
    environment:
      POSTGRES_DB: ml_examples
      POSTGRES_USER: ml_user
      POSTGRES_PASSWORD: ml_pass123
    volumes:
      - ./data/postgres:/var/lib/postgresql/data
      - ./sql/init:/docker-entrypoint-initdb.d/
    restart: unless-stopped
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ml_user -d ml_examples"]
      interval: 10s
      timeout: 5s
      retries: 5

  # Chroma vector database
  chroma:
    image: chromadb/chroma:latest
    container_name: ml_chroma
    ports:
      - "8000:8000"
    volumes:
      - ./data/chroma:/chroma/chroma
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8000/api/v1/heartbeat"]
      interval: 30s
      timeout: 10s
      retries: 3

  # ClickHouse for analytics
  clickhouse:
    image: clickhouse/clickhouse-server:latest
    container_name: ml_clickhouse
    ports:
      - "8123:8123"  # HTTP interface
      - "9000:9000"  # Native TCP interface
    volumes:
      - ./data/clickhouse:/var/lib/clickhouse
    environment:
      CLICKHOUSE_DB: ml_analytics
      CLICKHOUSE_USER: ml_user
      CLICKHOUSE_PASSWORD: ml_pass123
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "wget", "--no-verbose", "--tries=1", "--spider", "http://localhost:8123/ping"]
      interval: 30s
      timeout: 10s
      retries: 3

  # Redis for caching
  redis:
    image: redis:7-alpine
    container_name: ml_redis
    ports:
      - "6379:6379"
    volumes:
      - ./data/redis:/data
    command: redis-server --appendonly yes
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

  # Jupyter for experimentation
  jupyter:
    image: jupyter/datascience-notebook:latest
    container_name: ml_jupyter
    ports:
      - "8888:8888"
    volumes:
      - ./notebooks:/home/jovyan/work
      - ./data:/home/jovyan/data
    environment:
      JUPYTER_ENABLE_LAB: "yes"
      JUPYTER_TOKEN: ml_examples_token
    restart: unless-stopped
"@

$dockerCompose | Out-File -FilePath "docker\docker-compose.yml" -Encoding UTF8

# Create database initialization scripts
Write-Host "🗃️  Creating database initialization scripts..." -ForegroundColor Yellow

$initExtensions = @"
-- Enable required extensions for ML workloads
CREATE EXTENSION IF NOT EXISTS vector;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS btree_gin;
CREATE EXTENSION IF NOT EXISTS btree_gist;
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- Create ML user and schema
CREATE SCHEMA IF NOT EXISTS ml_schema;
GRANT ALL PRIVILEGES ON SCHEMA ml_schema TO ml_user;
"@

$initExtensions | Out-File -FilePath "sql\init\01-setup-extensions.sql" -Encoding UTF8

$createTables = @"
-- ML Experiments tracking
CREATE TABLE IF NOT EXISTS ml_schema.experiments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    description TEXT,
    model_type VARCHAR(100) NOT NULL,
    parameters JSONB,
    metrics JSONB,
    status VARCHAR(50) DEFAULT 'created',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    completed_at TIMESTAMP WITH TIME ZONE
);

-- Document embeddings storage
CREATE TABLE IF NOT EXISTS ml_schema.document_embeddings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id VARCHAR(255) NOT NULL,
    content_hash VARCHAR(64) NOT NULL,
    embedding vector(1536), -- OpenAI embedding dimension
    model_name VARCHAR(100) NOT NULL,
    chunk_index INTEGER DEFAULT 0,
    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Model artifacts tracking
CREATE TABLE IF NOT EXISTS ml_schema.model_artifacts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    experiment_id UUID REFERENCES ml_schema.experiments(id),
    artifact_name VARCHAR(255) NOT NULL,
    artifact_type VARCHAR(100) NOT NULL, -- 'model', 'scaler', 'vectorizer'
    file_path TEXT NOT NULL,
    file_size_bytes BIGINT,
    checksum VARCHAR(64),
    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_experiments_status ON ml_schema.experiments(status);
CREATE INDEX IF NOT EXISTS idx_experiments_model_type ON ml_schema.experiments(model_type);
CREATE INDEX IF NOT EXISTS idx_experiments_created_at ON ml_schema.experiments(created_at);

CREATE INDEX IF NOT EXISTS idx_embeddings_document_id ON ml_schema.document_embeddings(document_id);
CREATE INDEX IF NOT EXISTS idx_embeddings_model_name ON ml_schema.document_embeddings(model_name);
CREATE INDEX IF NOT EXISTS idx_embeddings_vector ON ml_schema.document_embeddings USING ivfflat (embedding vector_cosine_ops);

CREATE INDEX IF NOT EXISTS idx_artifacts_experiment_id ON ml_schema.model_artifacts(experiment_id);
CREATE INDEX IF NOT EXISTS idx_artifacts_type ON ml_schema.model_artifacts(artifact_type);
"@

$createTables | Out-File -FilePath "sql\init\02-create-tables.sql" -Encoding UTF8

# Create .NET project
Write-Host "📦 Creating .NET project..." -ForegroundColor Yellow

Set-Location "src\ML.Examples"

$projectFile = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Npgsql" Version="8.0.3" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.4" />
    <PackageReference Include="Pgvector.EntityFrameworkCore" Version="0.2.1" />
    <PackageReference Include="DuckDB.NET.Data" Version="1.0.0" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Console" Version="8.0.0" />
    <PackageReference Include="System.Text.Json" Version="8.0.4" />
  </ItemGroup>
</Project>
"@

$projectFile | Out-File -FilePath "ML.Examples.csproj" -Encoding UTF8

# Create simple Program.cs
$programFile = @"
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ML.Examples;

class Program
{
    static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddHttpClient();
                services.AddLogging();
            })
            .Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("ML Database Examples - Setup Complete!");
        logger.LogInformation("Next steps:");
        logger.LogInformation("1. Start services: docker-compose -f docker/docker-compose.yml up -d");
        logger.LogInformation("2. Check health: docker-compose -f docker/docker-compose.yml ps");
        logger.LogInformation("3. Run examples from the documentation");
        logger.LogInformation("4. Access Jupyter: http://localhost:8888/lab?token=ml_examples_token");
    }
}
"@

$programFile | Out-File -FilePath "Program.cs" -Encoding UTF8

Set-Location "..\..\"

# Create startup script
Write-Host "🎯 Creating startup script..." -ForegroundColor Yellow

$startupScript = @"
#!/usr/bin/env pwsh

Write-Host "🚀 Starting ML Database Technologies Stack" -ForegroundColor Green

# Start Docker services
Write-Host "🐳 Starting Docker services..." -ForegroundColor Yellow
docker-compose -f docker/docker-compose.yml up -d

# Wait for services to be healthy
Write-Host "⏳ Waiting for services to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Check service status
Write-Host "📊 Checking service status..." -ForegroundColor Yellow
docker-compose -f docker/docker-compose.yml ps

Write-Host "✅ Stack is ready!" -ForegroundColor Green
Write-Host ""
Write-Host "🌐 Access Points:" -ForegroundColor Cyan
Write-Host "  PostgreSQL:  localhost:5432 (user: ml_user, password: ml_pass123)" -ForegroundColor Gray
Write-Host "  Chroma:      http://localhost:8000" -ForegroundColor Gray
Write-Host "  ClickHouse:  http://localhost:8123" -ForegroundColor Gray
Write-Host "  Redis:       localhost:6379" -ForegroundColor Gray
Write-Host "  Jupyter:     http://localhost:8888/lab?token=ml_examples_token" -ForegroundColor Gray
Write-Host ""
Write-Host "📚 Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Open notebooks/ml-database-examples.ipynb in Jupyter" -ForegroundColor Gray
Write-Host "  2. Follow the README.md for detailed examples" -ForegroundColor Gray
Write-Host "  3. Run: dotnet run --project src/ML.Examples" -ForegroundColor Gray
"@

$startupScript | Out-File -FilePath "start.ps1" -Encoding UTF8

# Create stop script
$stopScript = @"
#!/usr/bin/env pwsh

Write-Host "🛑 Stopping ML Database Technologies Stack" -ForegroundColor Yellow

docker-compose -f docker/docker-compose.yml down

Write-Host "✅ Stack stopped!" -ForegroundColor Green
"@

$stopScript | Out-File -FilePath "stop.ps1" -Encoding UTF8

Set-Location ".."

Write-Host ""
Write-Host "🎉 Setup completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "📁 Created project structure in: MLDatabaseExamples/" -ForegroundColor Cyan
Write-Host ""
Write-Host "🚀 Quick Start:" -ForegroundColor Cyan
Write-Host "  cd MLDatabaseExamples" -ForegroundColor Gray
Write-Host "  .\start.ps1" -ForegroundColor Gray
Write-Host ""
Write-Host "📖 Documentation available in:" -ForegroundColor Cyan
Write-Host "  - README.md (complete guide)" -ForegroundColor Gray  
Write-Host "  - notebooks/ml-database-examples.ipynb (interactive examples)" -ForegroundColor Gray
Write-Host ""
Write-Host "🌐 After starting, access:" -ForegroundColor Cyan
Write-Host "  - Jupyter Lab: http://localhost:8888/lab?token=ml_examples_token" -ForegroundColor Gray
Write-Host "  - Chroma API: http://localhost:8000" -ForegroundColor Gray