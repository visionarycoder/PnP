# GraphQL Authorization Patterns

**Description**: Comprehensive authorization patterns for HotChocolate GraphQL applications including field-level security, role-based access, and policy-driven authorization.

**Language/Technology**: C# / HotChocolate

## Code

### Authorization Handlers

```csharp
namespace DocumentProcessor.GraphQL.Authorization;

using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

// Document ownership authorization handler
public class DocumentOwnershipHandler : AuthorizationHandler<DocumentOwnershipRequirement, Document>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<DocumentOwnershipHandler> _logger;

    public DocumentOwnershipHandler(
        IDocumentRepository documentRepository,
        ILogger<DocumentOwnershipHandler> logger)
    {
        _documentRepository = documentRepository;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DocumentOwnershipRequirement requirement,
        Document resource)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("User ID not found in claims");
            return;
        }

        // Check direct ownership
        if (resource.Metadata.AuthorId == userId)
        {
            context.Succeed(requirement);
            return;
        }

        // Check team membership for shared documents
        if (requirement.AllowTeamAccess && !string.IsNullOrEmpty(resource.TeamId))
        {
            var hasTeamAccess = await _documentRepository.HasTeamAccessAsync(
                userId, resource.TeamId, CancellationToken.None);
                
            if (hasTeamAccess)
            {
                context.Succeed(requirement);
                return;
            }
        }

        // Check explicit permissions
        var hasExplicitAccess = await _documentRepository.HasExplicitAccessAsync(
            userId, resource.Id, CancellationToken.None);
            
        if (hasExplicitAccess)
        {
            context.Succeed(requirement);
        }
    }
}

public class DocumentOwnershipRequirement : IAuthorizationRequirement
{
    public bool AllowTeamAccess { get; set; } = true;
    public bool AllowReadOnlyAccess { get; set; } = false;
}

// Role-based authorization handler
public class RoleBasedHandler : AuthorizationHandler<RoleBasedRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RoleBasedRequirement requirement)
    {
        var userRoles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value);
        
        if (requirement.RequiredRoles.Any(role => userRoles.Contains(role)))
        {
            context.Succeed(requirement);
        }
        else if (requirement.FallbackToOwnership && context.Resource is Document document)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (document.Metadata.AuthorId == userId)
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}

public class RoleBasedRequirement : IAuthorizationRequirement
{
    public string[] RequiredRoles { get; set; } = Array.Empty<string>();
    public bool FallbackToOwnership { get; set; } = false;
}

// Processing pipeline authorization handler
public class ProcessingPipelineHandler : AuthorizationHandler<ProcessingPipelineRequirement>
{
    private readonly IProcessingPipelineRepository _pipelineRepository;

    public ProcessingPipelineHandler(IProcessingPipelineRepository pipelineRepository)
    {
        _pipelineRepository = pipelineRepository;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ProcessingPipelineRequirement requirement)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        // Check user's pipeline access level
        var accessLevel = await _pipelineRepository.GetUserAccessLevelAsync(
            userId, requirement.PipelineId, CancellationToken.None);

        if (accessLevel >= requirement.MinimumAccessLevel)
        {
            context.Succeed(requirement);
        }
    }
}

public class ProcessingPipelineRequirement : IAuthorizationRequirement
{
    public string PipelineId { get; set; } = "";
    public AccessLevel MinimumAccessLevel { get; set; }
}

public enum AccessLevel
{
    None = 0,
    Read = 1,
    Execute = 2,
    Configure = 3,
    Manage = 4
}
```

### Authorization Policies

```csharp
// Authorization policy configuration
public static class AuthorizationPolicies
{
    public const string ReadDocument = "ReadDocument";
    public const string ModifyDocument = "ModifyDocument";
    public const string DeleteDocument = "DeleteDocument";
    public const string ProcessDocument = "ProcessDocument";
    public const string ManageUsers = "ManageUsers";
    public const string ViewAnalytics = "ViewAnalytics";
    public const string SystemAdmin = "SystemAdmin";

    public static void ConfigurePolicies(AuthorizationOptions options)
    {
        // Document access policies
        options.AddPolicy(ReadDocument, policy =>
            policy.RequireAuthenticatedUser()
                  .AddRequirements(new DocumentOwnershipRequirement 
                  { 
                      AllowTeamAccess = true, 
                      AllowReadOnlyAccess = true 
                  }));

        options.AddPolicy(ModifyDocument, policy =>
            policy.RequireAuthenticatedUser()
                  .AddRequirements(new DocumentOwnershipRequirement 
                  { 
                      AllowTeamAccess = true, 
                      AllowReadOnlyAccess = false 
                  }));

        options.AddPolicy(DeleteDocument, policy =>
            policy.RequireAuthenticatedUser()
                  .AddRequirements(new RoleBasedRequirement 
                  { 
                      RequiredRoles = new[] { "DocumentAdmin", "Owner" },
                      FallbackToOwnership = true
                  }));

        // Processing policies
        options.AddPolicy(ProcessDocument, policy =>
            policy.RequireAuthenticatedUser()
                  .RequireClaim("can_process", "true")
                  .AddRequirements(new ProcessingPipelineRequirement 
                  { 
                      MinimumAccessLevel = AccessLevel.Execute 
                  }));

        // Administrative policies
        options.AddPolicy(ManageUsers, policy =>
            policy.RequireRole("Admin", "UserManager"));

        options.AddPolicy(ViewAnalytics, policy =>
            policy.RequireAuthenticatedUser()
                  .RequireAssertion(context =>
                      context.User.IsInRole("Admin") ||
                      context.User.IsInRole("Analyst") ||
                      context.User.HasClaim("department", "Analytics")));

        options.AddPolicy(SystemAdmin, policy =>
            policy.RequireRole("SystemAdmin")
                  .RequireClaim("elevated_access", "true"));
    }
}
```

### Field-Level Authorization Attributes

```csharp
// Custom authorization attributes for GraphQL
public class DocumentOwnershipAttribute : AuthorizeAttribute
{
    public DocumentOwnershipAttribute() : base(AuthorizationPolicies.ReadDocument) { }
}

public class RequireRoleAttribute : AuthorizeAttribute
{
    public RequireRoleAttribute(params string[] roles)
    {
        Roles = string.Join(",", roles);
    }
}

public class RequireClaimAttribute : AuthorizeAttribute
{
    public RequireClaimAttribute(string claimType, string claimValue = "")
    {
        Policy = $"RequireClaim_{claimType}_{claimValue}";
    }
}

// Processing-specific authorization
public class ProcessingAccessAttribute : AuthorizeAttribute
{
    public ProcessingAccessAttribute(AccessLevel minLevel = AccessLevel.Read)
    {
        Policy = $"ProcessingAccess_{minLevel}";
    }
}

// Team-based authorization
public class TeamMemberAttribute : AuthorizeAttribute
{
    public TeamMemberAttribute() : base("TeamMember") { }
}
```

### GraphQL Type Authorization

```csharp
// Document type with field-level authorization
[ObjectType<Document>]
public static partial class DocumentType
{
    // Public fields (no authorization required)
    public static string GetId([Parent] Document document) => document.Id;
    
    public static string GetTitle([Parent] Document document) => document.Title;

    // Authorized field access
    [DocumentOwnership]
    public static string GetContent([Parent] Document document) => document.Content;

    [RequireRole("Admin", "Analyst")]
    public static DocumentMetadata GetMetadata([Parent] Document document) => document.Metadata;

    [RequireClaim("can_view_processing", "true")]
    public static async Task<IEnumerable<ProcessingResult>> GetProcessingResultsAsync(
        [Parent] Document document,
        [Service] IProcessingResultRepository repository,
        CancellationToken cancellationToken)
    {
        return await repository.GetByDocumentIdAsync(document.Id, cancellationToken);
    }

    // Owner or team member access
    [DocumentOwnership]
    public static async Task<IEnumerable<DocumentVersion>> GetVersionsAsync(
        [Parent] Document document,
        [Service] IDocumentVersionRepository repository,
        CancellationToken cancellationToken)
    {
        return await repository.GetVersionsAsync(document.Id, cancellationToken);
    }

    // Admin-only sensitive data
    [RequireRole("Admin")]
    public static async Task<DocumentAuditLog> GetAuditLogAsync(
        [Parent] Document document,
        [Service] IAuditLogRepository repository,
        CancellationToken cancellationToken)
    {
        return await repository.GetDocumentAuditLogAsync(document.Id, cancellationToken);
    }
}

// User type with privacy controls
[ObjectType<User>]
public static partial class UserType
{
    public static string GetId([Parent] User user) => user.Id;
    
    public static string GetDisplayName([Parent] User user) => user.DisplayName;

    // Self or admin access to personal information
    [Authorize]
    public static string GetEmail(
        [Parent] User user,
        ClaimsPrincipal currentUser)
    {
        var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId == user.Id || currentUser.IsInRole("Admin"))
        {
            return user.Email;
        }
        throw new UnauthorizedAccessException("Cannot access another user's email");
    }

    [RequireRole("Admin", "HR")]
    public static UserProfile? GetProfile([Parent] User user) => user.Profile;

    [RequireRole("Admin")]
    public static DateTime GetLastLogin([Parent] User user) => user.LastLoginAt;
}
```

### Query and Mutation Authorization

```csharp
// Document queries with authorization
[QueryType]
public class DocumentQueries
{
    // Public queries (filtered by authorization in resolver)
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public async Task<IQueryable<Document>> GetDocumentsAsync(
        [Service] IDocumentRepository repository,
        [Service] IAuthorizationService authorizationService,
        ClaimsPrincipal currentUser,
        CancellationToken cancellationToken)
    {
        // Return only documents the user is authorized to read
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return await repository.GetAuthorizedDocumentsAsync(userId, cancellationToken);
    }

    [DocumentOwnership]
    public async Task<Document?> GetDocumentAsync(
        string id,
        [Service] IDocumentRepository repository,
        CancellationToken cancellationToken)
    {
        return await repository.GetByIdAsync(id, cancellationToken);
    }

    // Admin-only queries
    [RequireRole("Admin")]
    public async Task<IEnumerable<Document>> GetAllDocumentsAsync(
        [Service] IDocumentRepository repository,
        CancellationToken cancellationToken)
    {
        return await repository.GetAllAsync(cancellationToken);
    }

    [RequireRole("Admin", "Analyst")]
    public async Task<DocumentAnalytics> GetDocumentAnalyticsAsync(
        [Service] IAnalyticsService analyticsService,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        return await analyticsService.GetDocumentAnalyticsAsync(from, to, cancellationToken);
    }
}

// Document mutations with authorization
[MutationType]
public class DocumentMutations
{
    [Authorize]
    public async Task<Document> CreateDocumentAsync(
        CreateDocumentInput input,
        [Service] IDocumentService documentService,
        ClaimsPrincipal currentUser,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? throw new UnauthorizedAccessException("User ID not found");

        return await documentService.CreateAsync(input, userId, cancellationToken);
    }

    [DocumentOwnership]
    public async Task<Document> UpdateDocumentAsync(
        string id,
        UpdateDocumentInput input,
        [Service] IDocumentService documentService,
        [Service] IAuthorizationService authorizationService,
        ClaimsPrincipal currentUser,
        CancellationToken cancellationToken)
    {
        var document = await documentService.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Document {id} not found");

        var authResult = await authorizationService.AuthorizeAsync(
            currentUser, document, AuthorizationPolicies.ModifyDocument);

        if (!authResult.Succeeded)
        {
            throw new UnauthorizedAccessException("Insufficient permissions to modify document");
        }

        return await documentService.UpdateAsync(id, input, cancellationToken);
    }

    [RequireRole("Admin", "Owner")]
    public async Task<bool> DeleteDocumentAsync(
        string id,
        [Service] IDocumentService documentService,
        [Service] IAuthorizationService authorizationService,
        ClaimsPrincipal currentUser,
        CancellationToken cancellationToken)
    {
        var document = await documentService.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Document {id} not found");

        var authResult = await authorizationService.AuthorizeAsync(
            currentUser, document, AuthorizationPolicies.DeleteDocument);

        if (!authResult.Succeeded)
        {
            throw new UnauthorizedAccessException("Insufficient permissions to delete document");
        }

        return await documentService.DeleteAsync(id, cancellationToken);
    }

    [ProcessingAccess(AccessLevel.Execute)]
    public async Task<ProcessingJob> StartProcessingAsync(
        StartProcessingInput input,
        [Service] IProcessingService processingService,
        [Service] IAuthorizationService authorizationService,
        ClaimsPrincipal currentUser,
        CancellationToken cancellationToken)
    {
        // Verify document access
        var document = await processingService.GetDocumentAsync(input.DocumentId, cancellationToken)
            ?? throw new NotFoundException($"Document {input.DocumentId} not found");

        var docAuthResult = await authorizationService.AuthorizeAsync(
            currentUser, document, AuthorizationPolicies.ReadDocument);

        if (!docAuthResult.Succeeded)
        {
            throw new UnauthorizedAccessException("Insufficient permissions to process document");
        }

        return await processingService.StartProcessingAsync(input, cancellationToken);
    }
}
```

### Dynamic Authorization Middleware

```csharp
// Custom authorization middleware for dynamic rules
public class DynamicAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<DynamicAuthorizationMiddleware> _logger;

    public DynamicAuthorizationMiddleware(
        RequestDelegate next,
        IAuthorizationService authorizationService,
        ILogger<DynamicAuthorizationMiddleware> logger)
    {
        _next = next;
        _authorizationService = authorizationService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/graphql"))
        {
            // Extract authorization context from GraphQL request
            var authContext = await ExtractAuthorizationContextAsync(context);
            
            if (authContext != null && !await IsAuthorizedAsync(context.User, authContext))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Access Denied");
                return;
            }
        }

        await _next(context);
    }

    private async Task<AuthorizationContext?> ExtractAuthorizationContextAsync(HttpContext context)
    {
        // Extract GraphQL operation and variables
        // Implement based on your GraphQL request format
        return null; // Simplified for example
    }

    private async Task<bool> IsAuthorizedAsync(ClaimsPrincipal user, AuthorizationContext authContext)
    {
        // Implement dynamic authorization logic
        return true; // Simplified for example
    }
}

public class AuthorizationContext
{
    public string Operation { get; set; } = "";
    public Dictionary<string, object> Variables { get; set; } = new();
    public string[] RequiredPermissions { get; set; } = Array.Empty<string>();
}
```

### Resource-Based Authorization

```csharp
// Resource-based authorization service
public interface IResourceAuthorizationService
{
    Task<bool> CanAccessDocumentAsync(ClaimsPrincipal user, string documentId, CancellationToken cancellationToken = default);
    Task<bool> CanModifyDocumentAsync(ClaimsPrincipal user, string documentId, CancellationToken cancellationToken = default);
    Task<bool> CanDeleteDocumentAsync(ClaimsPrincipal user, string documentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetAccessibleDocumentIdsAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);
}

public class ResourceAuthorizationService : IResourceAuthorizationService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IAuthorizationService _authorizationService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ResourceAuthorizationService> _logger;

    public ResourceAuthorizationService(
        IDocumentRepository documentRepository,
        IAuthorizationService authorizationService,
        IMemoryCache cache,
        ILogger<ResourceAuthorizationService> logger)
    {
        _documentRepository = documentRepository;
        _authorizationService = authorizationService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> CanAccessDocumentAsync(
        ClaimsPrincipal user, 
        string documentId, 
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"access:{user.Identity?.Name}:{documentId}";
        
        if (_cache.TryGetValue(cacheKey, out bool cachedResult))
        {
            return cachedResult;
        }

        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
        if (document == null)
        {
            return false;
        }

        var authResult = await _authorizationService.AuthorizeAsync(
            user, document, AuthorizationPolicies.ReadDocument);

        var result = authResult.Succeeded;
        
        // Cache for 5 minutes
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
        
        return result;
    }

    public async Task<bool> CanModifyDocumentAsync(
        ClaimsPrincipal user, 
        string documentId, 
        CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
        if (document == null)
        {
            return false;
        }

        var authResult = await _authorizationService.AuthorizeAsync(
            user, document, AuthorizationPolicies.ModifyDocument);

        return authResult.Succeeded;
    }

    public async Task<bool> CanDeleteDocumentAsync(
        ClaimsPrincipal user, 
        string documentId, 
        CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
        if (document == null)
        {
            return false;
        }

        var authResult = await _authorizationService.AuthorizeAsync(
            user, document, AuthorizationPolicies.DeleteDocument);

        return authResult.Succeeded;
    }

    public async Task<IEnumerable<string>> GetAccessibleDocumentIdsAsync(
        ClaimsPrincipal user, 
        CancellationToken cancellationToken = default)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Array.Empty<string>();
        }

        // Get documents based on user's roles and permissions
        var documentIds = new List<string>();

        // Owner documents
        var ownedDocuments = await _documentRepository.GetDocumentIdsByOwnerAsync(userId, cancellationToken);
        documentIds.AddRange(ownedDocuments);

        // Team documents
        var teamDocuments = await _documentRepository.GetDocumentIdsByTeamMemberAsync(userId, cancellationToken);
        documentIds.AddRange(teamDocuments);

        // Explicitly shared documents
        var sharedDocuments = await _documentRepository.GetSharedDocumentIdsAsync(userId, cancellationToken);
        documentIds.AddRange(sharedDocuments);

        return documentIds.Distinct();
    }
}
```

### Authorization Service Registration

```csharp
// Service registration for authorization
public static class AuthorizationServiceExtensions
{
    public static IServiceCollection AddDocumentAuthorization(this IServiceCollection services)
    {
        // Register authorization handlers
        services.AddScoped<IAuthorizationHandler, DocumentOwnershipHandler>();
        services.AddScoped<IAuthorizationHandler, RoleBasedHandler>();
        services.AddScoped<IAuthorizationHandler, ProcessingPipelineHandler>();

        // Register resource authorization service
        services.AddScoped<IResourceAuthorizationService, ResourceAuthorizationService>();

        // Configure authorization policies
        services.AddAuthorization(AuthorizationPolicies.ConfigurePolicies);

        return services;
    }

    public static IApplicationBuilder UseDocumentAuthorization(this IApplicationBuilder app)
    {
        // Add dynamic authorization middleware
        app.UseMiddleware<DynamicAuthorizationMiddleware>();

        return app;
    }
}

// GraphQL configuration with authorization
services
    .AddGraphQLServer()
    .AddQueryType<DocumentQueries>()
    .AddMutationType<DocumentMutations>()
    .AddAuthorization()
    .AddDocumentAuthorization()
    .ModifyRequestOptions(opt =>
    {
        opt.IncludeExceptionDetails = true;
    });

app.UseAuthentication();
app.UseAuthorization();
app.UseDocumentAuthorization();
```

## Usage

### Query Examples with Authorization

```graphql
# Public query - returns only authorized documents
query GetMyDocuments {
  documents {
    nodes {
      id
      title
      # Content field requires DocumentOwnership
      content
      
      # Metadata requires Admin/Analyst role
      metadata {
        authorId
        createdAt
        tags
      }
    }
  }
}

# Authorized mutation - requires ownership
mutation UpdateDocument($id: ID!, $input: UpdateDocumentInput!) {
  updateDocument(id: $id, input: $input) {
    id
    title
    content
    updatedAt
  }
}

# Admin-only query
query GetSystemAnalytics {
  documentAnalytics {
    totalDocuments
    processingStats {
      completed
      failed
      inProgress
    }
    userActivity {
      activeUsers
      newRegistrations
    }
  }
}
```

## Notes

- **Field-Level Security**: Apply authorization at the field level for fine-grained control
- **Resource-Based**: Use resource-based authorization for entity-specific permissions
- **Policy-Driven**: Define reusable authorization policies for common scenarios
- **Caching**: Cache authorization decisions for performance optimization
- **Audit Trail**: Log authorization decisions for compliance and debugging
- **Dynamic Rules**: Support dynamic authorization rules based on business logic
- **Performance**: Consider the performance impact of authorization checks
- **Testing**: Write comprehensive tests for authorization scenarios

## Related Patterns

- [Error Handling](error-handling.md) - Handling authorization errors
- [Schema Design](schema-design.md) - Securing schema types and fields
- [Performance Optimization](performance-optimization.md) - Optimizing authorization checks

---

**Key Benefits**: Fine-grained security, role-based access, policy-driven authorization, resource protection

**When to Use**: Multi-tenant applications, sensitive data access, complex permission models

**Performance**: Cached decisions, batch authorization, efficient policy evaluation