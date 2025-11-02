# .NET Aspire Production Deployment Strategies

**Description**: Comprehensive deployment patterns for .NET Aspire applications in production environments, including cloud deployment, container orchestration, infrastructure as code, and production-ready configurations.

**Language/Technology**: C#, .NET Aspire, .NET 9.0, Azure Container Apps, Kubernetes, Docker, Bicep

**Code**:

## Table of Contents

1. [Deployment Overview](#deployment-overview)
2. [Azure Container Apps Deployment](#azure-container-apps-deployment)
3. [Kubernetes Deployment](#kubernetes-deployment)
4. [Configuration Management](#configuration-management)
5. [Security Considerations](#security-considerations)
6. [Monitoring and Observability](#monitoring-and-observability)
7. [CI/CD Pipeline Integration](#cicd-pipeline-integration)

## Deployment Overview

### Deployment Architecture

```csharp
namespace DocumentProcessor.Aspire.Deployment;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

public class ProductionHostBuilder
{
    public static IDistributedApplicationBuilder CreateProductionBuilder(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);
        
        // Production-specific configuration
        builder.Configuration.AddAzureKeyVault(
            new Uri(builder.Configuration["KeyVault:VaultUri"]!),
            new DefaultAzureCredential());
            
        // Enable production telemetry
        builder.Services.AddApplicationInsightsTelemetry();
        
        // Configure health checks
        builder.Services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database")
            .AddCheck<OrleansHealthCheck>("orleans")
            .AddCheck<RedisHealthCheck>("redis");
            
        return builder;
    }
}

public record DeploymentConfiguration
{
    public string Environment { get; init; } = "Production";
    public CloudProvider Provider { get; init; } = CloudProvider.Azure;
    public string ResourceGroup { get; init; } = string.Empty;
    public string ContainerRegistry { get; init; } = string.Empty;
    public Dictionary<string, string> Tags { get; init; } = new();
    public NetworkConfiguration Network { get; init; } = new();
    public SecurityConfiguration Security { get; init; } = new();
}

public enum CloudProvider { Azure, AWS, GCP, OnPremises }

public record NetworkConfiguration
{
    public string VirtualNetwork { get; init; } = string.Empty;
    public string[] Subnets { get; init; } = Array.Empty<string>();
    public bool EnablePrivateEndpoints { get; init; } = true;
    public string[] AllowedCIDRs { get; init; } = Array.Empty<string>();
}

public record SecurityConfiguration
{
    public string KeyVaultName { get; init; } = string.Empty;
    public string ManagedIdentity { get; init; } = string.Empty;
    public bool EnableMutualTLS { get; init; } = true;
    public string CertificateThumbprint { get; init; } = string.Empty;
}
```

## Azure Container Apps Deployment

### Container Apps Configuration

```csharp
namespace DocumentProcessor.Aspire.Azure;

public static class AzureContainerAppsExtensions
{
    public static IDistributedApplicationBuilder ConfigureForContainerApps(
        this IDistributedApplicationBuilder builder)
    {
        var environment = builder.Configuration["ASPNETCORE_ENVIRONMENT"];
        
        if (environment == "Production")
        {
            // Azure Container Registry
            var registry = builder.AddAzureContainerRegistry("acr");
            
            // Managed PostgreSQL
            var postgres = builder.AddAzurePostgresFlexibleServer("document-db")
                .WithDatabase("documentprocessor")
                .ConfigureInfrastructure(infrastructure =>
                {
                    infrastructure.AssignProperty(p => p.AdministratorLogin, "dbadmin");
                    infrastructure.AssignProperty(p => p.AdministratorLoginPassword, 
                        new BicepSecretOutputReference("dbPassword", infrastructure));
                    infrastructure.AssignProperty(p => p.HighAvailability, 
                        new { mode = "ZoneRedundant" });
                });
            
            // Azure Cache for Redis
            var redis = builder.AddAzureRedis("cache")
                .ConfigureInfrastructure(infrastructure =>
                {
                    infrastructure.AssignProperty(p => p.Sku, new { 
                        name = "Premium", 
                        family = "P", 
                        capacity = 1 
                    });
                    infrastructure.AssignProperty(p => p.EnableNonSslPort, false);
                });
            
            // Azure Storage Account
            var storage = builder.AddAzureStorage("storage")
                .ConfigureInfrastructure(infrastructure =>
                {
                    infrastructure.AssignProperty(p => p.Kind, "StorageV2");
                    infrastructure.AssignProperty(p => p.Sku, new { name = "Standard_LRS" });
                    infrastructure.AssignProperty(p => p.MinimumTlsVersion, "TLS1_2");
                });
            
            // Container Apps Environment
            var containerEnv = builder.AddAzureContainerAppsEnvironment("container-env")
                .ConfigureInfrastructure(infrastructure =>
                {
                    infrastructure.AssignProperty(p => p.AppLogsConfiguration, new {
                        destination = "log-analytics",
                        logAnalyticsConfiguration = new {
                            customerId = new BicepSecretOutputReference("logAnalyticsWorkspaceId", infrastructure),
                            sharedKey = new BicepSecretOutputReference("logAnalyticsKey", infrastructure)
                        }
                    });
                });
            
            // Orleans Cluster as Container App
            var orleansCluster = builder.AddProject<Projects.OrleansHost>("orleans-host")
                .WithReference(postgres)
                .WithReference(redis)
                .PublishAsAzureContainerApp()
                .WithEnvironment("ORLEANS_CLUSTERING_PROVIDER", "AdoNet")
                .WithEnvironment("ORLEANS_PERSISTENCE_PROVIDER", "AdoNet")
                .ConfigureInfrastructure(infrastructure =>
                {
                    infrastructure.AssignProperty(p => p.Template!.Scale, new {
                        minReplicas = 2,
                        maxReplicas = 10,
                        rules = new[] {
                            new {
                                name = "cpu-scaling-rule",
                                custom = new {
                                    type = "cpu",
                                    metadata = new { type = "Utilization", value = "70" }
                                }
                            }
                        }
                    });
                });
            
            // Document Processing API
            var documentApi = builder.AddProject<Projects.DocumentProcessorApi>("document-api")
                .WithReference(orleansCluster)
                .WithReference(postgres)
                .WithReference(redis)
                .WithReference(storage)
                .PublishAsAzureContainerApp()
                .WithExternalIngress()
                .ConfigureInfrastructure(infrastructure =>
                {
                    infrastructure.AssignProperty(p => p.Template!.Containers.First().Resources, new {
                        cpu = 1.0,
                        memory = "2Gi"
                    });
                });
            
            return builder;
        }
        
        return builder;
    }
}
```

### Bicep Infrastructure Template

```bicep
@description('The location for all resources')
param location string = resourceGroup().location

@description('The name prefix for all resources')
param namePrefix string

@description('The container registry name')
param containerRegistryName string

@description('The managed identity for container apps')
param managedIdentityName string

// Managed Identity
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: managedIdentityName
  location: location
}

// Container Registry
resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: containerRegistryName
  location: location
  sku: {
    name: 'Premium'
  }
  properties: {
    adminUserEnabled: false
    networkRuleSet: {
      defaultAction: 'Deny'
      ipRules: []
    }
    publicNetworkAccess: 'Enabled'
    zoneRedundancy: 'Enabled'
  }
}

// Log Analytics Workspace
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${namePrefix}-logs'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 90
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

// Application Insights
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${namePrefix}-appinsights'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
    IngestionMode: 'LogAnalytics'
  }
}

// Container Apps Environment
resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${namePrefix}-containerenv'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsWorkspace.properties.customerId
        sharedKey: logAnalyticsWorkspace.listKeys().primarySharedKey
      }
    }
    zoneRedundant: true
  }
}

// PostgreSQL Flexible Server
resource postgresqlServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-06-01-preview' = {
  name: '${namePrefix}-postgres'
  location: location
  sku: {
    name: 'Standard_D2ds_v4'
    tier: 'GeneralPurpose'
  }
  properties: {
    administratorLogin: 'dbadmin'
    administratorLoginPassword: 'P@ssw0rd123!' // Should be from Key Vault
    version: '15'
    storage: {
      storageSizeGB: 128
      autoGrow: 'Enabled'
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Enabled'
    }
    highAvailability: {
      mode: 'ZoneRedundant'
    }
    network: {
      publicNetworkAccess: 'Enabled'
    }
  }
}

// Redis Cache
resource redisCache 'Microsoft.Cache/redis@2023-08-01' = {
  name: '${namePrefix}-redis'
  location: location
  properties: {
    sku: {
      name: 'Premium'
      family: 'P'
      capacity: 1
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
    redisConfiguration: {
      'maxmemory-reserved': '50'
      'maxfragmentationmemory-reserved': '50'
      'maxmemory-delta': '50'
    }
  }
}

// Storage Account
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: '${namePrefix}storage'
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    networkAcls: {
      defaultAction: 'Allow'
    }
  }
}

output containerRegistryLoginServer string = containerRegistry.properties.loginServer
output managedIdentityClientId string = managedIdentity.properties.clientId
output logAnalyticsWorkspaceId string = logAnalyticsWorkspace.properties.customerId
output applicationInsightsConnectionString string = applicationInsights.properties.ConnectionString
```

## Kubernetes Deployment

### Kubernetes Manifests

```yaml
# namespace.yaml
apiVersion: v1
kind: Namespace
metadata:
  name: document-processor
  labels:
    name: document-processor

---
# configmap.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: app-config
  namespace: document-processor
data:
  ASPNETCORE_ENVIRONMENT: "Production"
  ORLEANS_CLUSTERING_PROVIDER: "Kubernetes"
  ORLEANS_SERVICE_ID: "DocumentProcessor"
  ORLEANS_CLUSTER_ID: "DocumentProcessorCluster"

---
# orleans-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: orleans-host
  namespace: document-processor
spec:
  replicas: 3
  selector:
    matchLabels:
      app: orleans-host
  template:
    metadata:
      labels:
        app: orleans-host
        orleans/serviceId: DocumentProcessor
        orleans/clusterId: DocumentProcessorCluster
    spec:
      containers:
      - name: orleans-host
        image: myregistry.azurecr.io/orleans-host:latest
        ports:
        - containerPort: 8080
          name: http
        - containerPort: 11111
          name: silo
        - containerPort: 30000
          name: gateway
        env:
        - name: ORLEANS_SERVICE_ID
          valueFrom:
            configMapKeyRef:
              name: app-config
              key: ORLEANS_SERVICE_ID
        - name: ORLEANS_CLUSTER_ID
          valueFrom:
            configMapKeyRef:
              name: app-config
              key: ORLEANS_CLUSTER_ID
        resources:
          requests:
            memory: "512Mi"
            cpu: "500m"
          limits:
            memory: "1Gi"
            cpu: "1000m"
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /ready
            port: 8080
          initialDelaySeconds: 15
          periodSeconds: 5

---
# api-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: document-api
  namespace: document-processor
spec:
  replicas: 2
  selector:
    matchLabels:
      app: document-api
  template:
    metadata:
      labels:
        app: document-api
    spec:
      containers:
      - name: document-api
        image: myregistry.azurecr.io/document-api:latest
        ports:
        - containerPort: 8080
          name: http
        envFrom:
        - configMapRef:
            name: app-config
        resources:
          requests:
            memory: "1Gi"
            cpu: "500m"
          limits:
            memory: "2Gi"
            cpu: "1000m"

---
# services.yaml
apiVersion: v1
kind: Service
metadata:
  name: orleans-host-service
  namespace: document-processor
spec:
  selector:
    app: orleans-host
  ports:
  - name: http
    port: 80
    targetPort: 8080
  - name: silo
    port: 11111
    targetPort: 11111
  - name: gateway
    port: 30000
    targetPort: 30000

---
apiVersion: v1
kind: Service
metadata:
  name: document-api-service
  namespace: document-processor
spec:
  selector:
    app: document-api
  ports:
  - name: http
    port: 80
    targetPort: 8080
  type: LoadBalancer
```

### Helm Chart Structure

```yaml
# Chart.yaml
apiVersion: v2
name: document-processor
description: Document Processing with Orleans and ML Services
version: 1.0.0
appVersion: "1.0.0"

# values.yaml
global:
  imageRegistry: myregistry.azurecr.io
  imagePullPolicy: Always
  
replicaCount:
  orleans: 3
  api: 2
  ml: 1

image:
  orleans:
    repository: orleans-host
    tag: latest
  api:
    repository: document-api
    tag: latest

service:
  type: LoadBalancer
  port: 80

ingress:
  enabled: true
  className: "nginx"
  annotations:
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
  hosts:
    - host: api.documentprocessor.com
      paths:
        - path: /
          pathType: Prefix
  tls:
    - secretName: api-tls
      hosts:
        - api.documentprocessor.com

resources:
  orleans:
    limits:
      cpu: 1000m
      memory: 1Gi
    requests:
      cpu: 500m
      memory: 512Mi
  api:
    limits:
      cpu: 1000m
      memory: 2Gi
    requests:
      cpu: 500m
      memory: 1Gi

autoscaling:
  enabled: true
  minReplicas: 2
  maxReplicas: 10
  targetCPUUtilizationPercentage: 70
  targetMemoryUtilizationPercentage: 80
```

## Configuration Management

### Production Configuration Provider

```csharp
namespace DocumentProcessor.Aspire.Configuration;

public static class ProductionConfigurationExtensions
{
    public static IDistributedApplicationBuilder AddProductionConfiguration(
        this IDistributedApplicationBuilder builder)
    {
        builder.Services.Configure<ProductionOptions>(options =>
        {
            options.Environment = builder.Environment.EnvironmentName;
            options.KeyVaultUri = builder.Configuration["KeyVault:VaultUri"];
            options.ApplicationInsightsConnectionString = 
                builder.Configuration["ApplicationInsights:ConnectionString"];
        });
        
        // Add Azure Key Vault
        if (!string.IsNullOrEmpty(builder.Configuration["KeyVault:VaultUri"]))
        {
            builder.Configuration.AddAzureKeyVault(
                new Uri(builder.Configuration["KeyVault:VaultUri"]!),
                new DefaultAzureCredential());
        }
        
        // Add Azure App Configuration
        if (!string.IsNullOrEmpty(builder.Configuration["AppConfig:ConnectionString"]))
        {
            builder.Configuration.AddAzureAppConfiguration(options =>
            {
                options.Connect(builder.Configuration["AppConfig:ConnectionString"])
                       .ConfigureRefresh(refresh =>
                       {
                           refresh.Register("DocumentProcessor:*", refreshAll: true)
                                  .SetCacheExpiration(TimeSpan.FromMinutes(5));
                       })
                       .UseFeatureFlags(featureFlags =>
                       {
                           featureFlags.CacheExpirationInterval = TimeSpan.FromMinutes(1);
                       });
            });
        }
        
        return builder;
    }
}

public class ProductionOptions
{
    public string Environment { get; set; } = string.Empty;
    public string? KeyVaultUri { get; set; }
    public string? ApplicationInsightsConnectionString { get; set; }
    public DatabaseOptions Database { get; set; } = new();
    public RedisOptions Redis { get; set; } = new();
    public StorageOptions Storage { get; set; } = new();
}

public class DatabaseOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public int MaxRetryCount { get; set; } = 3;
    public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool EnableSensitiveDataLogging { get; set; } = false;
}

public class RedisOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public int Database { get; set; } = 0;
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan SyncTimeout { get; set; } = TimeSpan.FromSeconds(5);
}

public class StorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "documents";
    public bool UseManagedIdentity { get; set; } = true;
}
```

## Security Considerations

### Security Configuration

```csharp
namespace DocumentProcessor.Aspire.Security;

public static class SecurityExtensions
{
    public static IDistributedApplicationBuilder ConfigureProductionSecurity(
        this IDistributedApplicationBuilder builder)
    {
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = builder.Configuration["Authentication:Authority"];
                options.Audience = builder.Configuration["Authentication:Audience"];
                options.RequireHttpsMetadata = true;
                options.SaveToken = false;
                
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
            });
        
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireDocumentProcessorScope", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("scope", "document.process");
            });
            
            options.AddPolicy("RequireAdminRole", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Administrator");
            });
        });
        
        // Configure HTTPS redirection
        builder.Services.AddHttpsRedirection(options =>
        {
            options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
            options.HttpsPort = 443;
        });
        
        // Configure HSTS
        builder.Services.AddHsts(options =>
        {
            options.Preload = true;
            options.IncludeSubDomains = true;
            options.MaxAge = TimeSpan.FromDays(365);
        });
        
        // Configure security headers
        builder.Services.Configure<SecurityHeadersOptions>(options =>
        {
            options.ContentSecurityPolicy = 
                "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';";
            options.ReferrerPolicy = "strict-origin-when-cross-origin";
            options.PermissionsPolicy = "geolocation=(), microphone=(), camera=()";
        });
        
        return builder;
    }
}

public class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        
        await next(context);
    }
}

public class SecurityHeadersOptions
{
    public string ContentSecurityPolicy { get; set; } = string.Empty;
    public string ReferrerPolicy { get; set; } = string.Empty;
    public string PermissionsPolicy { get; set; } = string.Empty;
}
```

## Monitoring and Observability

### Production Monitoring Setup

```csharp
namespace DocumentProcessor.Aspire.Monitoring;

public static class MonitoringExtensions
{
    public static IDistributedApplicationBuilder AddProductionMonitoring(
        this IDistributedApplicationBuilder builder)
    {
        // Application Insights
        builder.Services.AddApplicationInsightsTelemetry(options =>
        {
            options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
            options.EnableAdaptiveSampling = true;
            options.EnableDebugLogger = false;
        });
        
        // OpenTelemetry
        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.Filter = (httpContext) => 
                        !httpContext.Request.Path.Value?.Contains("/health") ?? true;
                });
                
                tracing.AddHttpClientInstrumentation();
                tracing.AddEntityFrameworkCoreInstrumentation();
                tracing.AddRedisInstrumentation();
                
                tracing.AddAzureMonitorTraceExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation();
                metrics.AddHttpClientInstrumentation();
                metrics.AddRuntimeInstrumentation();
                
                metrics.AddAzureMonitorMetricExporter();
            });
        
        // Health checks
        builder.Services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready", "live" })
            .AddCheck<RedisHealthCheck>("redis", tags: new[] { "ready" })
            .AddCheck<OrleansHealthCheck>("orleans", tags: new[] { "ready" })
            .AddCheck<StorageHealthCheck>("storage", tags: new[] { "ready" });
        
        // Custom metrics
        builder.Services.AddSingleton<ICustomMetrics, CustomMetrics>();
        
        return builder;
    }
}

public interface ICustomMetrics
{
    void IncrementDocumentProcessed(string documentType);
    void RecordProcessingTime(string operation, TimeSpan duration);
    void IncrementErrorCount(string errorType);
}

public class CustomMetrics : ICustomMetrics
{
    private readonly Counter<long> documentsProcessedCounter;
    private readonly Histogram<double> processingTimeHistogram;
    private readonly Counter<long> errorCounter;
    
    public CustomMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("DocumentProcessor.Metrics");
        
        documentsProcessedCounter = meter.CreateCounter<long>(
            "documents_processed_total",
            description: "Total number of documents processed");
            
        processingTimeHistogram = meter.CreateHistogram<double>(
            "processing_time_seconds",
            unit: "s",
            description: "Time taken to process operations");
            
        errorCounter = meter.CreateCounter<long>(
            "errors_total",
            description: "Total number of errors");
    }
    
    public void IncrementDocumentProcessed(string documentType)
    {
        documentsProcessedCounter.Add(1, new("document_type", documentType));
    }
    
    public void RecordProcessingTime(string operation, TimeSpan duration)
    {
        processingTimeHistogram.Record(duration.TotalSeconds, new("operation", operation));
    }
    
    public void IncrementErrorCount(string errorType)
    {
        errorCounter.Add(1, new("error_type", errorType));
    }
}
```

## CI/CD Pipeline Integration

### GitHub Actions Workflow

```yaml
name: Deploy to Production

on:
  push:
    branches: [main]
  workflow_dispatch:

env:
  REGISTRY: myregistry.azurecr.io
  RESOURCE_GROUP: rg-documentprocessor-prod
  CONTAINER_APP_ENV: cae-documentprocessor-prod

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build and test
      run: |
        dotnet build --configuration Release --no-restore
        dotnet test --configuration Release --no-build --verbosity normal
        
    - name: Login to Azure
      uses: azure/login@v1
      with:
        creds: ${{ secrets.AZURE_CREDENTIALS }}
        
    - name: Login to Container Registry
      run: az acr login --name ${{ env.REGISTRY }}
      
    - name: Build and push Docker images
      run: |
        # Build Orleans Host
        docker build -t ${{ env.REGISTRY }}/orleans-host:${{ github.sha }} \
          -f src/Orleans.Host/Dockerfile .
        docker push ${{ env.REGISTRY }}/orleans-host:${{ github.sha }}
        
        # Build Document API
        docker build -t ${{ env.REGISTRY }}/document-api:${{ github.sha }} \
          -f src/DocumentProcessor.Api/Dockerfile .
        docker push ${{ env.REGISTRY }}/document-api:${{ github.sha }}
        
    - name: Deploy infrastructure
      run: |
        az deployment group create \
          --resource-group ${{ env.RESOURCE_GROUP }} \
          --template-file infrastructure/main.bicep \
          --parameters containerRegistry=${{ env.REGISTRY }} \
                      imageTag=${{ github.sha }}
                      
    - name: Deploy to Container Apps
      run: |
        # Update Orleans Host
        az containerapp update \
          --name orleans-host \
          --resource-group ${{ env.RESOURCE_GROUP }} \
          --image ${{ env.REGISTRY }}/orleans-host:${{ github.sha }}
          
        # Update Document API
        az containerapp update \
          --name document-api \
          --resource-group ${{ env.RESOURCE_GROUP }} \
          --image ${{ env.REGISTRY }}/document-api:${{ github.sha }}
          
    - name: Run smoke tests
      run: |
        # Wait for deployment to be ready
        sleep 60
        
        # Run basic health checks
        API_URL=$(az containerapp show \
          --name document-api \
          --resource-group ${{ env.RESOURCE_GROUP }} \
          --query properties.configuration.ingress.fqdn -o tsv)
          
        curl -f https://$API_URL/health || exit 1
        curl -f https://$API_URL/ready || exit 1
```

### Azure DevOps Pipeline

```yaml
# azure-pipelines.yml
trigger:
- main

variables:
  buildConfiguration: 'Release'
  containerRegistry: 'myregistry.azurecr.io'
  resourceGroup: 'rg-documentprocessor-prod'

stages:
- stage: Build
  jobs:
  - job: BuildAndTest
    pool:
      vmImage: 'ubuntu-latest'
    steps:
    - task: UseDotNet@2
      displayName: 'Use .NET 9.0'
      inputs:
        packageType: 'sdk'
        version: '9.0.x'
        
    - task: DotNetCoreCLI@2
      displayName: 'Restore packages'
      inputs:
        command: 'restore'
        
    - task: DotNetCoreCLI@2
      displayName: 'Build solution'
      inputs:
        command: 'build'
        arguments: '--configuration $(buildConfiguration) --no-restore'
        
    - task: DotNetCoreCLI@2
      displayName: 'Run tests'
      inputs:
        command: 'test'
        arguments: '--configuration $(buildConfiguration) --no-build --collect:"XPlat Code Coverage"'
        
    - task: Docker@2
      displayName: 'Build Orleans Host image'
      inputs:
        containerRegistry: 'ACR Connection'
        repository: 'orleans-host'
        command: 'buildAndPush'
        Dockerfile: 'src/Orleans.Host/Dockerfile'
        tags: |
          $(Build.BuildId)
          latest
          
    - task: Docker@2
      displayName: 'Build Document API image'
      inputs:
        containerRegistry: 'ACR Connection'
        repository: 'document-api'
        command: 'buildAndPush'
        Dockerfile: 'src/DocumentProcessor.Api/Dockerfile'
        tags: |
          $(Build.BuildId)
          latest

- stage: Deploy
  dependsOn: Build
  condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/main'))
  jobs:
  - deployment: DeployToProduction
    environment: 'Production'
    pool:
      vmImage: 'ubuntu-latest'
    strategy:
      runOnce:
        deploy:
          steps:
          - task: AzureCLI@2
            displayName: 'Deploy infrastructure'
            inputs:
              azureSubscription: 'Azure Subscription'
              scriptType: 'bash'
              scriptLocation: 'inlineScript'
              inlineScript: |
                az deployment group create \
                  --resource-group $(resourceGroup) \
                  --template-file infrastructure/main.bicep \
                  --parameters containerRegistry=$(containerRegistry) \
                              imageTag=$(Build.BuildId)
                              
          - task: AzureContainerApps@1
            displayName: 'Deploy Orleans Host'
            inputs:
              azureSubscription: 'Azure Subscription'
              containerAppName: 'orleans-host'
              resourceGroup: $(resourceGroup)
              imageToDeploy: '$(containerRegistry)/orleans-host:$(Build.BuildId)'
              
          - task: AzureContainerApps@1
            displayName: 'Deploy Document API'
            inputs:
              azureSubscription: 'Azure Subscription'
              containerAppName: 'document-api'
              resourceGroup: $(resourceGroup)
              imageToDeploy: '$(containerRegistry)/document-api:$(Build.BuildId)'
```

**Usage**:

1. **Azure Container Apps**: Use for serverless container deployment with built-in scaling
2. **Kubernetes**: Use for more control over container orchestration and advanced scenarios
3. **Infrastructure as Code**: Use Bicep or Terraform for reproducible deployments
4. **Configuration Management**: Leverage Azure Key Vault and App Configuration for secrets and settings
5. **Security**: Implement authentication, authorization, and security headers
6. **Monitoring**: Set up comprehensive observability with Application Insights and custom metrics
7. **CI/CD**: Automate deployments with proper testing and rollback capabilities

**Notes**:

- **Environment Separation**: Maintain separate environments for development, staging, and production
- **Blue-Green Deployment**: Consider blue-green or canary deployment strategies for zero-downtime updates
- **Disaster Recovery**: Implement backup and recovery strategies for databases and storage
- **Performance Testing**: Include load testing in CI/CD pipeline before production deployment
- **Cost Optimization**: Monitor resource usage and implement auto-scaling policies
- **Security Scanning**: Include container security scanning and dependency vulnerability checks
- **Compliance**: Ensure deployments meet organizational security and compliance requirements

**Related Snippets**:

- [Service Orchestration](service-orchestration.md) - Coordinating services in production
- [Scaling Strategies](scaling-strategies.md) - Auto-scaling production workloads
- [Health Monitoring](health-monitoring.md) - Production health checks and diagnostics
- [Configuration Management](configuration-management.md) - Managing production configuration
