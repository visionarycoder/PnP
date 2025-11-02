# Authorization Patterns

**Description**: Role-based access control (RBAC), attribute-based access control (ABAC), policy-based authorization, and permission management patterns for enterprise security.

**Language/Technology**: C# / .NET 9.0

**Code**:

## Role-Based Access Control (RBAC) Implementation

```csharp
// Core RBAC Models
public record Role(int Id, string Name, string Description, bool IsActive)
{
    public List<Permission> Permissions { get; init; } = new();
}

public record Permission(int Id, string Resource, string Action, string? Scope = null)
{
    public string GetPermissionString() => $"{Resource}:{Action}" + (Scope != null ? $":{Scope}" : string.Empty);
}

public record UserRole(int UserId, int RoleId, DateTime AssignedAt, DateTime? ExpiresAt = null);

// RBAC Service Implementation
public interface IRoleBasedAccessControl
{
    Task<bool> HasPermissionAsync(int userId, string resource, string action, string? scope = null);
    Task<List<Permission>> GetUserPermissionsAsync(int userId);
    Task AssignRoleAsync(int userId, int roleId, DateTime? expiresAt = null);
    Task RevokeRoleAsync(int userId, int roleId);
    Task<List<Role>> GetUserRolesAsync(int userId);
}

public class RoleBasedAccessControlService : IRoleBasedAccessControl
{
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RoleBasedAccessControlService> _logger;

    public RoleBasedAccessControlService(
        IUserRoleRepository userRoleRepository,
        IRoleRepository roleRepository,
        IMemoryCache cache,
        ILogger<RoleBasedAccessControlService> logger)
    {
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> HasPermissionAsync(int userId, string resource, string action, string? scope = null)
    {
        var cacheKey = $"user_permissions_{userId}";
        var permissions = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(15));
            return await GetUserPermissionsAsync(userId);
        });

        var requiredPermission = $"{resource}:{action}" + (scope != null ? $":{scope}" : string.Empty);
        
        return permissions.Any(p => 
            p.GetPermissionString() == requiredPermission ||
            IsWildcardMatch(p.GetPermissionString(), requiredPermission));
    }

    public async Task<List<Permission>> GetUserPermissionsAsync(int userId)
    {
        var userRoles = await _userRoleRepository.GetActiveUserRolesAsync(userId);
        var roleIds = userRoles.Select(ur => ur.RoleId).ToList();
        
        var roles = await _roleRepository.GetRolesWithPermissionsAsync(roleIds);
        
        return roles
            .Where(r => r.IsActive)
            .SelectMany(r => r.Permissions)
            .Distinct()
            .ToList();
    }

    public async Task AssignRoleAsync(int userId, int roleId, DateTime? expiresAt = null)
    {
        var userRole = new UserRole(userId, roleId, DateTime.UtcNow, expiresAt);
        await _userRoleRepository.AssignRoleAsync(userRole);
        
        // Invalidate cache
        _cache.Remove($"user_permissions_{userId}");
        
        _logger.LogInformation("Role {RoleId} assigned to user {UserId}", roleId, userId);
    }

    public async Task RevokeRoleAsync(int userId, int roleId)
    {
        await _userRoleRepository.RevokeRoleAsync(userId, roleId);
        
        // Invalidate cache
        _cache.Remove($"user_permissions_{userId}");
        
        _logger.LogInformation("Role {RoleId} revoked from user {UserId}", roleId, userId);
    }

    public async Task<List<Role>> GetUserRolesAsync(int userId)
    {
        var userRoles = await _userRoleRepository.GetActiveUserRolesAsync(userId);
        var roleIds = userRoles.Select(ur => ur.RoleId).ToList();
        
        return await _roleRepository.GetRolesAsync(roleIds);
    }

    private static bool IsWildcardMatch(string pattern, string value)
    {
        // Support wildcard permissions like "documents:*" or "*:read"
        return pattern.Contains('*') && 
               Regex.IsMatch(value, "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$");
    }
}
```

## Attribute-Based Access Control (ABAC) Implementation

```csharp
// ABAC Attributes and Context
public record AttributeValue(string Name, object Value, Type ValueType);

public class AuthorizationContext
{
    public Dictionary<string, AttributeValue> SubjectAttributes { get; } = new();
    public Dictionary<string, AttributeValue> ResourceAttributes { get; } = new();
    public Dictionary<string, AttributeValue> EnvironmentAttributes { get; } = new();
    public string Action { get; set; } = string.Empty;

    public void AddSubjectAttribute(string name, object value)
    {
        SubjectAttributes[name] = new AttributeValue(name, value, value.GetType());
    }

    public void AddResourceAttribute(string name, object value)
    {
        ResourceAttributes[name] = new AttributeValue(name, value, value.GetType());
    }

    public void AddEnvironmentAttribute(string name, object value)
    {
        EnvironmentAttributes[name] = new AttributeValue(name, value, value.GetType());
    }

    public T? GetAttribute<T>(string name)
    {
        var allAttributes = SubjectAttributes.Concat(ResourceAttributes).Concat(EnvironmentAttributes);
        var attribute = allAttributes.FirstOrDefault(a => a.Key == name).Value;
        
        return attribute != null && attribute.Value is T value ? value : default;
    }
}

// Policy Definition and Evaluation
public abstract class AuthorizationPolicy
{
    public abstract string Name { get; }
    public abstract Task<PolicyResult> EvaluateAsync(AuthorizationContext context);
}

public enum PolicyDecision { Permit, Deny, NotApplicable }

public record PolicyResult(PolicyDecision Decision, string? Reason = null, Dictionary<string, object>? Obligations = null);

// Time-Based Access Policy
public class TimeBasedAccessPolicy : AuthorizationPolicy
{
    public override string Name => "TimeBasedAccess";

    public override Task<PolicyResult> EvaluateAsync(AuthorizationContext context)
    {
        var currentTime = context.GetAttribute<DateTime>("current_time") ?? DateTime.UtcNow;
        var allowedStartTime = context.GetAttribute<TimeSpan>("allowed_start_time");
        var allowedEndTime = context.GetAttribute<TimeSpan>("allowed_end_time");

        if (allowedStartTime == null || allowedEndTime == null)
            return Task.FromResult(new PolicyResult(PolicyDecision.NotApplicable));

        var currentTimeOfDay = currentTime.TimeOfDay;
        
        var isWithinAllowedTime = allowedStartTime <= allowedEndTime
            ? currentTimeOfDay >= allowedStartTime && currentTimeOfDay <= allowedEndTime
            : currentTimeOfDay >= allowedStartTime || currentTimeOfDay <= allowedEndTime;

        return Task.FromResult(new PolicyResult(
            isWithinAllowedTime ? PolicyDecision.Permit : PolicyDecision.Deny,
            isWithinAllowedTime ? "Access granted within allowed time window" : $"Access denied outside allowed time window ({allowedStartTime}-{allowedEndTime})"
        ));
    }
}

// Department-Based Access Policy
public class DepartmentBasedAccessPolicy : AuthorizationPolicy
{
    public override string Name => "DepartmentBasedAccess";

    public override Task<PolicyResult> EvaluateAsync(AuthorizationContext context)
    {
        var userDepartment = context.GetAttribute<string>("user_department");
        var resourceDepartment = context.GetAttribute<string>("resource_department");
        var isCrossDepartmentAllowed = context.GetAttribute<bool>("cross_department_allowed");

        if (userDepartment == null || resourceDepartment == null)
            return Task.FromResult(new PolicyResult(PolicyDecision.NotApplicable));

        if (userDepartment == resourceDepartment)
            return Task.FromResult(new PolicyResult(PolicyDecision.Permit, "Same department access"));

        if (isCrossDepartmentAllowed == true)
            return Task.FromResult(new PolicyResult(PolicyDecision.Permit, "Cross-department access allowed"));

        return Task.FromResult(new PolicyResult(PolicyDecision.Deny, "Cross-department access denied"));
    }
}

// ABAC Policy Engine
public interface IAttributeBasedAccessControl
{
    Task<PolicyResult> EvaluateAsync(AuthorizationContext context);
    void RegisterPolicy(AuthorizationPolicy policy);
    Task<AuthorizationContext> BuildContextAsync(int userId, string resourceId, string action);
}

public class AttributeBasedAccessControlEngine : IAttributeBasedAccessControl
{
    private readonly List<AuthorizationPolicy> _policies = new();
    private readonly IUserAttributeService _userAttributeService;
    private readonly IResourceAttributeService _resourceAttributeService;
    private readonly ILogger<AttributeBasedAccessControlEngine> _logger;

    public AttributeBasedAccessControlEngine(
        IUserAttributeService userAttributeService,
        IResourceAttributeService resourceAttributeService,
        ILogger<AttributeBasedAccessControlEngine> logger)
    {
        _userAttributeService = userAttributeService;
        _resourceAttributeService = resourceAttributeService;
        _logger = logger;
    }

    public void RegisterPolicy(AuthorizationPolicy policy)
    {
        _policies.Add(policy);
        _logger.LogInformation("Registered policy: {PolicyName}", policy.Name);
    }

    public async Task<PolicyResult> EvaluateAsync(AuthorizationContext context)
    {
        var results = new List<PolicyResult>();

        foreach (var policy in _policies)
        {
            try
            {
                var result = await policy.EvaluateAsync(context);
                results.Add(result);

                if (result.Decision == PolicyDecision.Deny)
                {
                    _logger.LogWarning("Policy {PolicyName} denied access: {Reason}", policy.Name, result.Reason);
                    return result; // Fail fast on explicit deny
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating policy {PolicyName}", policy.Name);
                results.Add(new PolicyResult(PolicyDecision.Deny, $"Policy evaluation error: {ex.Message}"));
            }
        }

        // All policies must either permit or be not applicable
        var hasPermit = results.Any(r => r.Decision == PolicyDecision.Permit);
        var hasApplicable = results.Any(r => r.Decision != PolicyDecision.NotApplicable);

        if (hasApplicable && !hasPermit)
            return new PolicyResult(PolicyDecision.Deny, "No applicable policies granted access");

        return new PolicyResult(PolicyDecision.Permit, "Access granted by policy evaluation");
    }

    public async Task<AuthorizationContext> BuildContextAsync(int userId, string resourceId, string action)
    {
        var context = new AuthorizationContext { Action = action };

        // Add subject attributes
        var userAttributes = await _userAttributeService.GetUserAttributesAsync(userId);
        foreach (var attr in userAttributes)
        {
            context.AddSubjectAttribute(attr.Key, attr.Value);
        }

        // Add resource attributes
        var resourceAttributes = await _resourceAttributeService.GetResourceAttributesAsync(resourceId);
        foreach (var attr in resourceAttributes)
        {
            context.AddResourceAttribute(attr.Key, attr.Value);
        }

        // Add environment attributes
        context.AddEnvironmentAttribute("current_time", DateTime.UtcNow);
        context.AddEnvironmentAttribute("day_of_week", DateTime.UtcNow.DayOfWeek);
        context.AddEnvironmentAttribute("is_weekend", DateTime.UtcNow.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);

        return context;
    }
}
```

## Policy-Based Authorization with Custom Policies

```csharp
// Custom Authorization Requirements
public class MinimumAgeRequirement : IAuthorizationRequirement
{
    public int MinimumAge { get; }
    
    public MinimumAgeRequirement(int minimumAge)
    {
        MinimumAge = minimumAge;
    }
}

public class DepartmentRequirement : IAuthorizationRequirement
{
    public string[] AllowedDepartments { get; }
    
    public DepartmentRequirement(params string[] allowedDepartments)
    {
        AllowedDepartments = allowedDepartments;
    }
}

public class BusinessHoursRequirement : IAuthorizationRequirement
{
    public TimeSpan StartTime { get; }
    public TimeSpan EndTime { get; }
    
    public BusinessHoursRequirement(TimeSpan startTime, TimeSpan endTime)
    {
        StartTime = startTime;
        EndTime = endTime;
    }
}

// Authorization Handlers
public class MinimumAgeHandler : AuthorizationHandler<MinimumAgeRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MinimumAgeRequirement requirement)
    {
        var birthDateClaim = context.User.FindFirst("birth_date");
        if (birthDateClaim == null || !DateTime.TryParse(birthDateClaim.Value, out var birthDate))
        {
            return Task.CompletedTask;
        }

        var age = DateTime.Today.Year - birthDate.Year;
        if (birthDate.Date > DateTime.Today.AddYears(-age))
        {
            age--;
        }

        if (age >= requirement.MinimumAge)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public class DepartmentHandler : AuthorizationHandler<DepartmentRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, DepartmentRequirement requirement)
    {
        var departmentClaim = context.User.FindFirst("department");
        if (departmentClaim != null && requirement.AllowedDepartments.Contains(departmentClaim.Value, StringComparer.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public class BusinessHoursHandler : AuthorizationHandler<BusinessHoursRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, BusinessHoursRequirement requirement)
    {
        var currentTime = DateTime.Now.TimeOfDay;
        
        if (currentTime >= requirement.StartTime && currentTime <= requirement.EndTime)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

// Dynamic Policy Builder
public class DynamicPolicyBuilder
{
    private readonly List<IAuthorizationRequirement> _requirements = new();
    private readonly List<string> _roles = new();
    private readonly List<string> _authenticationSchemes = new();

    public DynamicPolicyBuilder RequireMinimumAge(int age)
    {
        _requirements.Add(new MinimumAgeRequirement(age));
        return this;
    }

    public DynamicPolicyBuilder RequireDepartment(params string[] departments)
    {
        _requirements.Add(new DepartmentRequirement(departments));
        return this;
    }

    public DynamicPolicyBuilder RequireBusinessHours(TimeSpan start, TimeSpan end)
    {
        _requirements.Add(new BusinessHoursRequirement(start, end));
        return this;
    }

    public DynamicPolicyBuilder RequireRole(string role)
    {
        _roles.Add(role);
        return this;
    }

    public DynamicPolicyBuilder RequireAuthenticationScheme(string scheme)
    {
        _authenticationSchemes.Add(scheme);
        return this;
    }

    public AuthorizationPolicy Build()
    {
        var policyBuilder = new AuthorizationPolicyBuilder();

        if (_authenticationSchemes.Any())
        {
            policyBuilder.AddAuthenticationSchemes(_authenticationSchemes.ToArray());
        }
        else
        {
            policyBuilder.RequireAuthenticatedUser();
        }

        foreach (var role in _roles)
        {
            policyBuilder.RequireRole(role);
        }

        foreach (var requirement in _requirements)
        {
            policyBuilder.AddRequirements(requirement);
        }

        return policyBuilder.Build();
    }
}

// Policy Registry and Management
public interface IPolicyRegistry
{
    void RegisterPolicy(string name, AuthorizationPolicy policy);
    AuthorizationPolicy? GetPolicy(string name);
    void RegisterDynamicPolicy(string name, Func<DynamicPolicyBuilder, AuthorizationPolicy> builder);
    List<string> GetPolicyNames();
}

public class PolicyRegistry : IPolicyRegistry
{
    private readonly Dictionary<string, AuthorizationPolicy> _policies = new();
    private readonly ILogger<PolicyRegistry> _logger;

    public PolicyRegistry(ILogger<PolicyRegistry> logger)
    {
        _logger = logger;
    }

    public void RegisterPolicy(string name, AuthorizationPolicy policy)
    {
        _policies[name] = policy;
        _logger.LogInformation("Registered authorization policy: {PolicyName}", name);
    }

    public AuthorizationPolicy? GetPolicy(string name)
    {
        return _policies.GetValueOrDefault(name);
    }

    public void RegisterDynamicPolicy(string name, Func<DynamicPolicyBuilder, AuthorizationPolicy> builder)
    {
        var policy = builder(new DynamicPolicyBuilder());
        RegisterPolicy(name, policy);
    }

    public List<string> GetPolicyNames()
    {
        return _policies.Keys.ToList();
    }
}
```

## Permission Management and Hierarchies

```csharp
// Permission Hierarchy Implementation
public class PermissionHierarchy
{
    private readonly Dictionary<string, List<string>> _hierarchy = new();
    
    public void AddParentChild(string parent, string child)
    {
        if (!_hierarchy.ContainsKey(parent))
            _hierarchy[parent] = new List<string>();
            
        if (!_hierarchy[parent].Contains(child))
            _hierarchy[parent].Add(child);
    }
    
    public List<string> GetAllImpliedPermissions(string permission)
    {
        var implied = new HashSet<string> { permission };
        var toProcess = new Queue<string>();
        toProcess.Enqueue(permission);
        
        while (toProcess.Count > 0)
        {
            var current = toProcess.Dequeue();
            
            if (_hierarchy.ContainsKey(current))
            {
                foreach (var child in _hierarchy[current])
                {
                    if (implied.Add(child))
                    {
                        toProcess.Enqueue(child);
                    }
                }
            }
        }
        
        return implied.ToList();
    }
    
    public bool HasPermission(List<string> userPermissions, string requiredPermission)
    {
        var allImplied = userPermissions.SelectMany(GetAllImpliedPermissions).ToHashSet();
        return allImplied.Contains(requiredPermission);
    }
}

// Advanced Permission Service
public interface IPermissionService
{
    Task<bool> HasPermissionAsync(int userId, string permission);
    Task<List<string>> GetEffectivePermissionsAsync(int userId);
    Task GrantPermissionAsync(int userId, string permission, DateTime? expiresAt = null);
    Task RevokePermissionAsync(int userId, string permission);
    Task<bool> CanDelegatePermissionAsync(int fromUserId, int toUserId, string permission);
}

public class HierarchicalPermissionService : IPermissionService
{
    private readonly IRoleBasedAccessControl _rbacService;
    private readonly IPermissionRepository _permissionRepository;
    private readonly PermissionHierarchy _hierarchy;
    private readonly IMemoryCache _cache;
    private readonly ILogger<HierarchicalPermissionService> _logger;

    public HierarchicalPermissionService(
        IRoleBasedAccessControl rbacService,
        IPermissionRepository permissionRepository,
        PermissionHierarchy hierarchy,
        IMemoryCache cache,
        ILogger<HierarchicalPermissionService> logger)
    {
        _rbacService = rbacService;
        _permissionRepository = permissionRepository;
        _hierarchy = hierarchy;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> HasPermissionAsync(int userId, string permission)
    {
        var effectivePermissions = await GetEffectivePermissionsAsync(userId);
        return _hierarchy.HasPermission(effectivePermissions, permission);
    }

    public async Task<List<string>> GetEffectivePermissionsAsync(int userId)
    {
        var cacheKey = $"effective_permissions_{userId}";
        
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
            
            // Get permissions from roles
            var rolePermissions = await _rbacService.GetUserPermissionsAsync(userId);
            var rolePermissionNames = rolePermissions.Select(p => p.GetPermissionString()).ToList();
            
            // Get direct permissions
            var directPermissions = await _permissionRepository.GetUserDirectPermissionsAsync(userId);
            
            // Combine and get all implied permissions
            var allPermissions = rolePermissionNames.Concat(directPermissions).Distinct().ToList();
            return allPermissions.SelectMany(_hierarchy.GetAllImpliedPermissions).Distinct().ToList();
        });
    }

    public async Task GrantPermissionAsync(int userId, string permission, DateTime? expiresAt = null)
    {
        await _permissionRepository.GrantDirectPermissionAsync(userId, permission, expiresAt);
        _cache.Remove($"effective_permissions_{userId}");
        _logger.LogInformation("Granted permission {Permission} to user {UserId}", permission, userId);
    }

    public async Task RevokePermissionAsync(int userId, string permission)
    {
        await _permissionRepository.RevokeDirectPermissionAsync(userId, permission);
        _cache.Remove($"effective_permissions_{userId}");
        _logger.LogInformation("Revoked permission {Permission} from user {UserId}", permission, userId);
    }

    public async Task<bool> CanDelegatePermissionAsync(int fromUserId, int toUserId, string permission)
    {
        // Check if the delegating user has the permission
        var hasPermission = await HasPermissionAsync(fromUserId, permission);
        if (!hasPermission) return false;
        
        // Check if the delegating user has delegation rights
        var canDelegate = await HasPermissionAsync(fromUserId, $"delegate:{permission}");
        return canDelegate;
    }
}
```

## ASP.NET Core Integration and Middleware

```csharp
// Authorization Attribute
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
    {
        Policy = $"Permission:{permission}";
    }
}

// Authorization Middleware
public class AuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<AuthorizationMiddleware> _logger;

    public AuthorizationMiddleware(
        RequestDelegate next,
        IPermissionService permissionService,
        ILogger<AuthorizationMiddleware> logger)
    {
        _next = next;
        _permissionService = permissionService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip authorization for public endpoints
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata?.GetMetadata<AllowAnonymousAttribute>() != null)
        {
            await _next(context);
            return;
        }

        // Extract user ID from claims
        var userIdClaim = context.User.FindFirst("user_id");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            context.Response.StatusCode = 401;
            return;
        }

        // Check for required permission
        var permissionAttribute = endpoint?.Metadata?.GetMetadata<RequirePermissionAttribute>();
        if (permissionAttribute != null)
        {
            var requiredPermission = permissionAttribute.Policy.Replace("Permission:", "");
            var hasPermission = await _permissionService.HasPermissionAsync(userId, requiredPermission);
            
            if (!hasPermission)
            {
                _logger.LogWarning("User {UserId} denied access to {Path} - missing permission {Permission}",
                    userId, context.Request.Path, requiredPermission);
                context.Response.StatusCode = 403;
                return;
            }
        }

        await _next(context);
    }
}

// Policy-Based Authorization Handler
public class PermissionAuthorizationHandler : IAuthorizationHandler
{
    private readonly IPermissionService _permissionService;

    public PermissionAuthorizationHandler(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
        var pendingRequirements = context.PendingRequirements.ToList();
        
        foreach (var requirement in pendingRequirements)
        {
            if (requirement is IAuthorizationRequirement req && 
                context.User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = context.User.FindFirst("user_id");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                {
                    // Handle permission requirements
                    if (req is PermissionRequirement permReq)
                    {
                        var hasPermission = await _permissionService.HasPermissionAsync(userId, permReq.Permission);
                        if (hasPermission)
                        {
                            context.Succeed(requirement);
                        }
                    }
                }
            }
        }
    }
}

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    
    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}

// Service Registration
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthorizationPatterns(this IServiceCollection services)
    {
        services.AddScoped<IRoleBasedAccessControl, RoleBasedAccessControlService>();
        services.AddScoped<IAttributeBasedAccessControl, AttributeBasedAccessControlEngine>();
        services.AddScoped<IPermissionService, HierarchicalPermissionService>();
        services.AddSingleton<IPolicyRegistry, PolicyRegistry>();
        services.AddSingleton<PermissionHierarchy>();
        
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, MinimumAgeHandler>();
        services.AddScoped<IAuthorizationHandler, DepartmentHandler>();
        services.AddScoped<IAuthorizationHandler, BusinessHoursHandler>();
        
        return services;
    }
    
    public static IServiceCollection ConfigureAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Role-based policies
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Administrator"));
            options.AddPolicy("ManagerOrAdmin", policy => policy.RequireRole("Manager", "Administrator"));
            
            // Custom requirement policies
            options.AddPolicy("AdultOnly", policy => 
                policy.Requirements.Add(new MinimumAgeRequirement(18)));
                
            options.AddPolicy("ITDepartment", policy => 
                policy.Requirements.Add(new DepartmentRequirement("IT", "Engineering")));
                
            options.AddPolicy("BusinessHours", policy => 
                policy.Requirements.Add(new BusinessHoursRequirement(
                    new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0))));
            
            // Complex combined policies
            options.AddPolicy("SeniorITBusinessHours", policy =>
            {
                policy.Requirements.Add(new MinimumAgeRequirement(25));
                policy.Requirements.Add(new DepartmentRequirement("IT"));
                policy.Requirements.Add(new BusinessHoursRequirement(
                    new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0)));
            });
        });
        
        return services;
    }
}
```

**Usage**:

```csharp
// 1. RBAC Usage
var rbacService = serviceProvider.GetRequiredService<IRoleBasedAccessControl>();

// Check permissions
var canReadDocuments = await rbacService.HasPermissionAsync(userId, "documents", "read");
var canDeleteAll = await rbacService.HasPermissionAsync(userId, "*", "delete");

// Manage roles
await rbacService.AssignRoleAsync(userId, adminRoleId, DateTime.UtcNow.AddYears(1));
var userRoles = await rbacService.GetUserRolesAsync(userId);

// 2. ABAC Usage
var abacEngine = serviceProvider.GetRequiredService<IAttributeBasedAccessControl>();

// Register policies
abacEngine.RegisterPolicy(new TimeBasedAccessPolicy());
abacEngine.RegisterPolicy(new DepartmentBasedAccessPolicy());

// Evaluate access
var context = await abacEngine.BuildContextAsync(userId, resourceId, "read");
context.AddResourceAttribute("sensitivity_level", "confidential");
context.AddEnvironmentAttribute("allowed_start_time", new TimeSpan(8, 0, 0));
context.AddEnvironmentAttribute("allowed_end_time", new TimeSpan(18, 0, 0));

var result = await abacEngine.EvaluateAsync(context);
Console.WriteLine($"Access Decision: {result.Decision} - {result.Reason}");

// 3. Policy-Based Authorization in Controllers
[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    [HttpGet]
    [RequirePermission("documents:read")]
    public async Task<IActionResult> GetDocuments()
    {
        // Implementation
        return Ok();
    }
    
    [HttpPost]
    [Authorize(Policy = "SeniorITBusinessHours")]
    public async Task<IActionResult> CreateDocument([FromBody] DocumentRequest request)
    {
        // Implementation
        return Ok();
    }
    
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteDocument(int id)
    {
        // Implementation
        return Ok();
    }
}

// 4. Dynamic Policy Creation
var policyRegistry = serviceProvider.GetRequiredService<IPolicyRegistry>();

policyRegistry.RegisterDynamicPolicy("FinanceManagerBusinessHours", builder =>
    builder
        .RequireRole("Manager")
        .RequireDepartment("Finance")
        .RequireBusinessHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0))
        .Build());

// 5. Permission Hierarchy Setup
var hierarchy = serviceProvider.GetRequiredService<PermissionHierarchy>();

// Set up hierarchy: admin permissions include manager permissions, etc.
hierarchy.AddParentChild("documents:admin", "documents:write");
hierarchy.AddParentChild("documents:write", "documents:read");
hierarchy.AddParentChild("users:admin", "users:write");
hierarchy.AddParentChild("users:write", "users:read");

// Check hierarchical permissions
var permissionService = serviceProvider.GetRequiredService<IPermissionService>();
var canRead = await permissionService.HasPermissionAsync(userId, "documents:read"); // true if user has documents:admin
```

**Notes**:
- **RBAC**: Implements traditional role-based access control with caching and wildcard support
- **ABAC**: Provides fine-grained attribute-based control with extensible policy framework
- **Policy-Based**: Integrates with ASP.NET Core authorization with custom requirements and handlers
- **Performance**: Uses memory caching for permission lookups and policy evaluations
- **Extensibility**: Supports custom policies, requirements, and handlers for domain-specific rules
- **Audit**: Includes comprehensive logging for security events and access decisions
- **Hierarchical Permissions**: Supports permission inheritance and delegation patterns
- **Security**: Implements fail-fast on explicit deny, secure defaults, and comprehensive validation
- **Integration**: Provides middleware, attributes, and service registration for seamless ASP.NET Core integration

**Security Considerations**:
- Always fail closed (deny by default) when policies cannot be evaluated
- Cache permissions with short expiration times to balance performance and security
- Log all authorization decisions for audit and compliance
- Use secure token validation and claims-based authentication
- Implement permission delegation carefully with explicit delegation rights
- Validate all attribute sources and sanitize input data
- Consider using external policy engines (like Open Policy Agent) for complex scenarios