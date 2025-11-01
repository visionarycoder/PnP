# Role-Based Authorization with Claims

**Description**: Comprehensive role-based authorization system with claims-based permissions, custom authorization handlers, and policy-based security for ASP.NET Core applications.

**Language/Technology**: C#, ASP.NET Core 8+

**Code**:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

// Custom Authorization Requirements
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}

public class ResourceAccessRequirement : IAuthorizationRequirement
{
    public string Resource { get; }
    public string Action { get; }
    
    public ResourceAccessRequirement(string resource, string action)
    {
        Resource = resource;
        Action = action;
    }
}

// Authorization Handlers
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // Check if user has the required permission claim
        if (context.User.HasClaim("permission", requirement.Permission))
        {
            context.Succeed(requirement);
        }
        
        return Task.CompletedTask;
    }
}

public class ResourceAccessHandler : AuthorizationHandler<ResourceAccessRequirement>
{
    private readonly IResourcePermissionService permissionService;
    
    public ResourceAccessHandler(IResourcePermissionService permissionService)
    {
        this.permissionService = permissionService;
    }
    
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourceAccessRequirement requirement)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            context.Fail();
            return;
        }

        var hasAccess = await permissionService.HasAccessAsync(
            userId, requirement.Resource, requirement.Action);
            
        if (hasAccess)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}

// Custom Authorization Attributes
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
    {
        Policy = $"Permission.{permission}";
    }
}

public class RequireResourceAccessAttribute : AuthorizeAttribute
{
    public RequireResourceAccessAttribute(string resource, string action)
    {
        Policy = $"Resource.{resource}.{action}";
    }
}

// Permission and Role Services
public interface IPermissionService
{
    Task<IEnumerable<string>> GetUserPermissionsAsync(string userId);
    Task<IEnumerable<string>> GetRolePermissionsAsync(string roleName);
    Task AssignPermissionToRoleAsync(string roleName, string permission);
    Task RevokePermissionFromRoleAsync(string roleName, string permission);
    Task AssignPermissionToUserAsync(string userId, string permission);
    Task RevokePermissionFromUserAsync(string userId, string permission);
}

public class PermissionService : IPermissionService
{
    private readonly IUserRepository userRepository;
    private readonly IRoleRepository roleRepository;
    private readonly IPermissionRepository permissionRepository;
    private readonly ILogger<PermissionService> logger;

    public PermissionService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        ILogger<PermissionService> logger)
    {
        this.userRepository = userRepository;
        this.roleRepository = roleRepository;
        this.permissionRepository = permissionRepository;
        this.logger = logger;
    }

    public async Task<IEnumerable<string>> GetUserPermissionsAsync(string userId)
    {
        // Get direct user permissions
        var userPermissions = await permissionRepository.GetUserPermissionsAsync(userId);
        
        // Get permissions from user roles
        var userRoles = await userRepository.GetUserRolesAsync(userId);
        var rolePermissions = new();
        
        foreach (var role in userRoles)
        {
            var permissions = await permissionRepository.GetRolePermissionsAsync(role);
            rolePermissions.AddRange(permissions);
        }
        
        // Combine and deduplicate
        return userPermissions.Union(rolePermissions).Distinct();
    }

    public async Task<IEnumerable<string>> GetRolePermissionsAsync(string roleName)
    {
        return await permissionRepository.GetRolePermissionsAsync(roleName);
    }

    public async Task AssignPermissionToRoleAsync(string roleName, string permission)
    {
        await permissionRepository.AssignPermissionToRoleAsync(roleName, permission);
        logger.LogInformation("Permission {Permission} assigned to role {Role}", permission, roleName);
    }

    public async Task RevokePermissionFromRoleAsync(string roleName, string permission)
    {
        await permissionRepository.RevokePermissionFromRoleAsync(roleName, permission);
        logger.LogInformation("Permission {Permission} revoked from role {Role}", permission, roleName);
    }

    public async Task AssignPermissionToUserAsync(string userId, string permission)
    {
        await permissionRepository.AssignPermissionToUserAsync(userId, permission);
        logger.LogInformation("Permission {Permission} assigned to user {UserId}", permission, userId);
    }

    public async Task RevokePermissionFromUserAsync(string userId, string permission)
    {
        await permissionRepository.RevokePermissionFromUserAsync(userId, permission);
        logger.LogInformation("Permission {Permission} revoked from user {UserId}", permission, userId);
    }
}

// Resource-based authorization service
public interface IResourcePermissionService
{
    Task<bool> HasAccessAsync(string userId, string resource, string action);
    Task GrantAccessAsync(string userId, string resource, string action);
    Task RevokeAccessAsync(string userId, string resource, string action);
}

public class ResourcePermissionService : IResourcePermissionService
{
    private readonly IResourceRepository resourceRepository;
    private readonly IPermissionService permissionService;

    public ResourcePermissionService(
        IResourceRepository resourceRepository,
        IPermissionService permissionService)
    {
        this.resourceRepository = resourceRepository;
        this.permissionService = permissionService;
    }

    public async Task<bool> HasAccessAsync(string userId, string resource, string action)
    {
        // Check if user has direct permission for this resource/action
        var userPermissions = await permissionService.GetUserPermissionsAsync(userId);
        var requiredPermission = $"{resource}.{action}";
        
        if (userPermissions.Contains(requiredPermission))
            return true;

        // Check if user owns the resource
        var resourceEntity = await resourceRepository.GetByIdAsync(resource);
        if (resourceEntity?.OwnerId == userId && action == "read")
            return true;

        // Check hierarchical permissions (admin can do everything)
        if (userPermissions.Contains("admin") || userPermissions.Contains($"{resource}.admin"))
            return true;

        return false;
    }

    public async Task GrantAccessAsync(string userId, string resource, string action)
    {
        var permission = $"{resource}.{action}";
        await permissionService.AssignPermissionToUserAsync(userId, permission);
    }

    public async Task RevokeAccessAsync(string userId, string resource, string action)
    {
        var permission = $"{resource}.{action}";
        await permissionService.RevokePermissionFromUserAsync(userId, permission);
    }
}

// Controllers demonstrating usage
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService documentService;
    private readonly IResourcePermissionService resourcePermissionService;

    public DocumentsController(
        IDocumentService documentService,
        IResourcePermissionService resourcePermissionService)
    {
        this.documentService = documentService;
        this.resourcePermissionService = resourcePermissionService;
    }

    [HttpGet]
    [RequirePermission("documents.list")]
    public async Task<IActionResult> GetDocuments()
    {
        var documents = await documentService.GetUserDocumentsAsync(GetUserId());
        return Ok(documents);
    }

    [HttpGet("{id}")]
    [RequireResourceAccess("document", "read")]
    public async Task<IActionResult> GetDocument(string id)
    {
        // Additional check within the method
        if (!await resourcePermissionService.HasAccessAsync(GetUserId(), $"document:{id}", "read"))
        {
            return Forbid("Insufficient permissions to access this document");
        }

        var document = await documentService.GetByIdAsync(id);
        if (document == null)
            return NotFound();

        return Ok(document);
    }

    [HttpPost]
    [RequirePermission("documents.create")]
    public async Task<IActionResult> CreateDocument([FromBody] CreateDocumentRequest request)
    {
        var document = await documentService.CreateAsync(request, GetUserId());
        
        // Grant owner full access to the new document
        await resourcePermissionService.GrantAccessAsync(GetUserId(), $"document:{document.Id}", "read");
        await resourcePermissionService.GrantAccessAsync(GetUserId(), $"document:{document.Id}", "write");
        await resourcePermissionService.GrantAccessAsync(GetUserId(), $"document:{document.Id}", "delete");

        return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, document);
    }

    [HttpPut("{id}")]
    [RequireResourceAccess("document", "write")]
    public async Task<IActionResult> UpdateDocument(string id, [FromBody] UpdateDocumentRequest request)
    {
        if (!await resourcePermissionService.HasAccessAsync(GetUserId(), $"document:{id}", "write"))
        {
            return Forbid("Insufficient permissions to modify this document");
        }

        var document = await documentService.UpdateAsync(id, request);
        return Ok(document);
    }

    [HttpDelete("{id}")]
    [RequireResourceAccess("document", "delete")]
    public async Task<IActionResult> DeleteDocument(string id)
    {
        if (!await resourcePermissionService.HasAccessAsync(GetUserId(), $"document:{id}", "delete"))
        {
            return Forbid("Insufficient permissions to delete this document");
        }

        await documentService.DeleteAsync(id);
        return NoContent();
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
}

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IPermissionService permissionService;

    public AdminController(IPermissionService permissionService)
    {
        this.permissionService = permissionService;
    }

    [HttpPost("roles/{roleName}/permissions")]
    public async Task<IActionResult> AssignPermissionToRole(
        string roleName, 
        [FromBody] AssignPermissionRequest request)
    {
        await permissionService.AssignPermissionToRoleAsync(roleName, request.Permission);
        return Ok(new { Message = $"Permission {request.Permission} assigned to role {roleName}" });
    }

    [HttpDelete("roles/{roleName}/permissions/{permission}")]
    public async Task<IActionResult> RevokePermissionFromRole(string roleName, string permission)
    {
        await permissionService.RevokePermissionFromRoleAsync(roleName, permission);
        return Ok(new { Message = $"Permission {permission} revoked from role {roleName}" });
    }

    [HttpPost("users/{userId}/permissions")]
    public async Task<IActionResult> AssignPermissionToUser(
        string userId, 
        [FromBody] AssignPermissionRequest request)
    {
        await permissionService.AssignPermissionToUserAsync(userId, request.Permission);
        return Ok(new { Message = $"Permission {request.Permission} assigned to user {userId}" });
    }
}

// DTOs
public record AssignPermissionRequest(string Permission);
public record CreateDocumentRequest(string Title, string Content);
public record UpdateDocumentRequest(string Title, string Content);

// Permission constants
public static class Permissions
{
    public static class Documents
    {
        public const string List = "documents.list";
        public const string Create = "documents.create";
        public const string Read = "documents.read";
        public const string Update = "documents.update";
        public const string Delete = "documents.delete";
        public const string Admin = "documents.admin";
    }

    public static class Users
    {
        public const string List = "users.list";
        public const string Create = "users.create";
        public const string Update = "users.update";
        public const string Delete = "users.delete";
        public const string Admin = "users.admin";
    }

    public static class System
    {
        public const string Admin = "system.admin";
        public const string ViewLogs = "system.viewlogs";
        public const string ManageSettings = "system.settings";
    }
}
```

**Usage**:

```csharp
// Program.cs - Authorization Configuration
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization(options =>
{
    // Permission-based policies
    options.AddPolicy("Permission.documents.list", policy =>
        policy.Requirements.Add(new PermissionRequirement("documents.list")));
    
    options.AddPolicy("Permission.documents.create", policy =>
        policy.Requirements.Add(new PermissionRequirement("documents.create")));

    // Resource-based policies
    options.AddPolicy("Resource.document.read", policy =>
        policy.Requirements.Add(new ResourceAccessRequirement("document", "read")));
    
    options.AddPolicy("Resource.document.write", policy =>
        policy.Requirements.Add(new ResourceAccessRequirement("document", "write")));

    // Role-based policies
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
    
    options.AddPolicy("ManagerOrAdmin", policy =>
        policy.RequireRole("Manager", "Admin"));

    // Complex policies
    options.AddPolicy("SeniorEmployee", policy =>
        policy.RequireAssertion(context =>
            context.User.IsInRole("Senior") && 
            context.User.HasClaim("department", "Engineering")));
});

// Register authorization handlers
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ResourceAccessHandler>();

// Register services
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IResourcePermissionService, ResourcePermissionService>();

var app = builder.Build();

// Using with minimal APIs
app.MapGet("/api/secure-endpoint", 
    [Authorize(Policy = "Permission.documents.list")] 
    async () => Results.Ok(new { Message = "Authorized access" }));

app.MapGet("/api/admin-endpoint", 
    [Authorize(Roles = "Admin")] 
    async () => Results.Ok(new { Message = "Admin access" }));

// Permission seeding example
using (var scope = app.Services.CreateScope())
{
    var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
    
    // Seed default permissions
    await permissionService.AssignPermissionToRoleAsync("User", Permissions.Documents.List);
    await permissionService.AssignPermissionToRoleAsync("User", Permissions.Documents.Read);
    await permissionService.AssignPermissionToRoleAsync("Manager", Permissions.Documents.Create);
    await permissionService.AssignPermissionToRoleAsync("Manager", Permissions.Documents.Update);
    await permissionService.AssignPermissionToRoleAsync("Admin", Permissions.System.Admin);
}

// Custom middleware for permission logging
public class PermissionLoggingMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<PermissionLoggingMiddleware> logger;

    public PermissionLoggingMiddleware(RequestDelegate next, ILogger<PermissionLoggingMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var endpoint = context.Request.Path;
            var method = context.Request.Method;
            
            logger.LogInformation(
                "User {UserId} accessing {Method} {Endpoint}",
                userId, method, endpoint);
        }

        await next(context);

        // Log authorization failures
        if (context.Response.StatusCode == 403)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
            logger.LogWarning(
                "Authorization failed for user {UserId} accessing {Method} {Endpoint}",
                userId, context.Request.Method, context.Request.Path);
        }
    }
}

// Register the middleware
app.UseMiddleware<PermissionLoggingMiddleware>();

// Client-side usage example
public class PermissionChecker
{
    private readonly HttpClient httpClient;
    
    public async Task<bool> HasPermissionAsync(string permission)
    {
        var response = await httpClient.GetAsync($"api/permissions/check/{permission}");
        return response.IsSuccessStatusCode;
    }
    
    public async Task<bool> CanAccessResourceAsync(string resource, string action)
    {
        var response = await httpClient.GetAsync($"api/permissions/resource/{resource}/{action}");
        return response.IsSuccessStatusCode;
    }
}

// UI helper for conditional rendering based on permissions
public class PermissionHelper
{
    public static bool UserHasPermission(ClaimsPrincipal user, string permission)
    {
        return user.HasClaim("permission", permission);
    }
    
    public static bool UserInRole(ClaimsPrincipal user, params string[] roles)
    {
        return roles.Any(role => user.IsInRole(role));
    }
}
```

**Prerequisites**:

- .NET 8 or later
- Microsoft.AspNetCore.Authorization package
- Entity Framework Core (for permission storage)
- Proper database schema for users, roles, and permissions

**Notes**:

- **Principle of Least Privilege**: Grant minimum permissions necessary for users to perform their tasks
- **Separation of Concerns**: Keep authorization logic separate from business logic
- **Caching**: Consider caching user permissions to improve performance
- **Hierarchical Permissions**: Implement permission inheritance (admin > manager > user)
- **Audit Trail**: Log all permission changes and access attempts
- **Dynamic Permissions**: Permissions can be added/removed at runtime without code changes
- **Resource Ownership**: Consider implementing resource ownership patterns
- **Performance**: Use efficient queries when checking permissions, especially for large datasets

**Related Snippets**:

- [JWT Authentication](jwt-authentication.md)
- [Password Security](password-security.md)
- [Audit Logging](audit-logging.md)

**References**:

- [ASP.NET Core Authorization](https://docs.microsoft.com/en-us/aspnet/core/security/authorization/)
- [Claims-based Authorization](https://docs.microsoft.com/en-us/aspnet/core/security/authorization/claims)
- [Policy-based Authorization](https://docs.microsoft.com/en-us/aspnet/core/security/authorization/policies)

---

*Created: 2025-11-01*  
*Last Updated: 2025-11-01*  
*Tags: #authorization #rbac #claims #permissions #security #aspnetcore*
